using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using System.Transactions;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobManager(
    IConfiguration config,
    ILogger<JobManager> logger,
    IJobsRepository jobs,
    IItemsRepository items,
    IEquipmentsRepository equipments)
{
    // DB接続文字列
    private readonly string _defaultConnection =
        config.GetConnectionString("DefaultConnection") ?? string.Empty;

    // JOB遷移チートシート
    private readonly IReadOnlyDictionary<
        JobType,
        IReadOnlyDictionary<JobStatus, JobStatus>>
        _statusTransitionMap =
            new Dictionary<JobType,IReadOnlyDictionary<JobStatus, JobStatus>>
            {
                [JobType.Picking] = new Dictionary<JobStatus, JobStatus> // 出庫JOBの場合
                {
                    [JobStatus.Unassigned] = JobStatus.Assigned, // 未割当→割当済み
                    [JobStatus.Assigned] = JobStatus.Transferring, // 割当済み→搬送中
                    [JobStatus.Transferring] = JobStatus.WaitOut, // 搬送中→取出待ち
                    [JobStatus.WaitOut] = JobStatus.Completed, // 取出待ち→完了
                },
                [JobType.Putaway] = new Dictionary<JobStatus, JobStatus> // 入庫JOBの場合
                {
                    [JobStatus.Assigned] = JobStatus.Transferring, // 割当済み→搬送中
                    [JobStatus.Transferring] = JobStatus.Completed, // 搬送中→完了
                },
            };

    // =========================
    //   公開メソッド
    // =========================

    /// <summary>
    /// 自動倉庫の保持JOBを異常終了させ、自動倉庫を指定した状態にする。
    /// </summary>
    /// <param name="equipmentId">対象自動倉庫</param>
    /// <param name="nextStatus">次の自動倉庫状態</param>
    public async Task ChangeEquipmentStatusAsync(
        string equipmentId,
        EquipmentStatus nextStatus,
        string abortReasonMessage)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // トランザクション開始
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // 対象自動倉庫は存在するか
            EquipmentModel? equipmentModel =
                await equipments.GetEquipmentsByIdAsync(connection,transaction,equipmentId) ??
                throw new KeyNotFoundException($"自動倉庫ID：{equipmentId}が存在しない");

            // 装置が保持する出庫JOBを取得
            string? pickingJobId = equipmentModel.PickingJobId;

            if (pickingJobId is not null)
            {
                // JOB番号が設定されていたら異常終了させる
                await AbortJobAsync(
                    connection,
                    transaction,
                    equipmentId,
                    pickingJobId,
                    abortReasonMessage);
            }

            // 装置が保持する入庫JOBを取得
            string? putawayJobId = equipmentModel.PutawayJobId;

            if(putawayJobId is not null)
            {
                // JOB番号が設定されていたら異常終了させる
                await AbortJobAsync(
                    connection,
                    transaction,
                    equipmentId,
                    putawayJobId,
                    abortReasonMessage);
            }

            // 装置が保持するJOBを解除し、装置状態を nextStatus にする
            await equipments.UpdateEquipmentStatusByIdAsync(
                connection,
                transaction,
                equipmentId,
                equipmentModel.Status,
                nextStatus);

            // コミット
            await transaction.CommitAsync();
        }
        catch
        {
            // ロールバックしてスロー
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// JOB状態を正常遷移させる
    /// </summary>
    /// <param name="jobId">JOB番号</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="nextStatus">次JOB状態</param>
    public async Task ChangeJobStatusAsync(
        string jobId,
        string equipmentId,
        JobStatus nextStatus)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // トランザクション開始
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // JobIdが存在するか
            JobModel currentJob =
                await jobs.GetJobByIdAsync(connection, transaction, jobId) ??
                throw new JobNotFoundException();

            // 入力値の検証
            ValidateEquipmentOwnership(currentJob, equipmentId);

            // 指定された状態遷移が正しいか検証する
            if (!_statusTransitionMap.TryGetValue(currentJob.JobType, out var transitionMap) ||
                !transitionMap.TryGetValue(currentJob.JobStatus, out JobStatus idealNextStatus) ||
                nextStatus != idealNextStatus)
            {
                // 取得できない、または、指定状態と不一致の場合は業務例外をスロー
                throw new InvalidStatusException();
            }

            // JOB状態を更新する
            await jobs.UpdateJobStatusByIdAsync(
                connection,
                transaction,
                jobId,
                currentJob.JobStatus,
                nextStatus);

            // コミット
            await transaction.CommitAsync();

        }
        catch
        {
            // ロールバックしてスロー
            await transaction.RollbackAsync();
            throw;

        }

    }

    /// <summary>
    /// 未割当状態のJOBをキャンセルする。
    /// </summary>
    /// <param name="jobId">JOB番号</param>
    /// <param name="deviceId">指令元スマホID</param>
    public async Task CancelUnassignedJobAsync(
        string jobId,
        string deviceId)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // トランザクション開始
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // JobIdが存在するか
            JobModel currentJob =
                await jobs.GetJobByIdAsync(connection, transaction, jobId) ??
                throw new JobNotFoundException();

            // 入力値の検証
            ValidateDeviceOwnership(currentJob, deviceId);

            if(currentJob.JobStatus != JobStatus.Unassigned)
            {
                // Unssignedでなければ、キャンセルを拒否する
                throw new InvalidStatusException();
            }

            // JOB状態をキャンセルにする
            await jobs.UpdateJobStatusByIdAsync(
                connection,
                transaction,
                jobId,
                currentJob.JobStatus,
                JobStatus.Canceled);

            logger.LogInformation(
                "JOBキャンセル JobId={JobId} DeviceId={DeviceId} Reason={Reason}",
                jobId,
                deviceId,
                "端末要求");

            // コミット
            await transaction.CommitAsync();

        }
        catch
        {
            // ロールバックしてスロー
            await transaction.RollbackAsync();
            throw;
        }

    }

    /// <summary>
    /// 未割当状態のJOBを異常終了する。
    /// </summary>
    /// <param name="jobId">JOB番号</param>
    public async Task AbortUnassignedJobAsync(
        string jobId,
        string abortReasonMessage)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // トランザクション開始
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // JobIdが存在するか
            JobModel currentJob =
                await jobs.GetJobByIdAsync(connection, transaction, jobId) ??
                throw new JobNotFoundException();

            if (currentJob.JobStatus != JobStatus.Unassigned)
            {
                // Unssignedでなければ、異常終了を拒否する
                throw new InvalidStatusException();
            }

            // JOBを異常終了させる
            await jobs.UpdateJobStatusByIdAsync(
                connection,
                transaction,
                jobId,
                currentJob.JobStatus,
                JobStatus.Aborted);

            logger.LogWarning(
                "JOB異常終了 JobId={JobId} Reason={Reason}",
                jobId,
                abortReasonMessage);

            // コミット
            await transaction.CommitAsync();
        }
        catch
        {
            // ロールバックしてスロー
            await transaction.RollbackAsync();
            throw;
        }

    }


    // =========================
    //   プライベートメソッド
    // =========================

    /// <summary>
    /// 出庫JOBの依頼元スマホIDを検証する。
    /// </summary>
    /// <param name="job">対象JOB</param>
    /// <param name="deviceId">対象自動倉庫</param>
    /// <exception cref="JobAccessDeniedException">
    /// JOBのスマホIDと引数が一致しないときにスローされる
    /// </exception>
    private static void ValidateDeviceOwnership(JobModel job, string deviceId)
    {
        if(job.JobType != JobType.Picking)
        {
            // DBデータ不整合を検知
            throw new InvalidOperationException(
                $"JOBが出庫JOBではない。JobId={job.JobId}");
        }

        if (job.DeviceId is null)
        {
            // DBデータ不整合を検知
            throw new InvalidOperationException(
                $"スマホIDが設定されていない。JobId={job.JobId}");
        }

        // JOBの依頼元スマホIDが、引数と一致するか
        if (job.DeviceId != deviceId)
        {
            // 一致しなければ、アクセス権なし例外をスロー
            throw new JobAccessDeniedException();
        }
    }

    /// <summary>
    /// 自動倉庫IDを検証する。
    /// </summary>
    /// <param name="job">対象JOB</param>
    /// <param name="equipmentId">対象自動倉庫</param>
    /// <exception cref="JobAccessDeniedException">
    /// JOBの自動倉庫IDと引数が一致しないときにスローされる
    /// </exception>
    private static void ValidateEquipmentOwnership(JobModel job, string equipmentId)
    {
        if (job.EquipmentId is null)
        {
            // DBデータ不整合を検知
            throw new InvalidOperationException(
                $"自動倉庫IDが設定されていない。JobId={job.JobId}");
        }

        // JOBの割当済み自動倉庫のIDが、引数と一致するか
        if (job.EquipmentId != equipmentId)
        {
            // 一致しなければ、アクセス権なし例外をスロー
            throw new JobAccessDeniedException();
        }
    }

    /// <summary>
    /// JOBを異常終了させる。
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="jobId">JOB番号</param>
    /// <param name="abortReasonMessage">異常終了理由</param>
    /// <exception cref="InvalidOperationException">
    /// DBデータ不整合でスローされる
    /// </exception>
    public async Task AbortJobAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string equipmentId,
        string jobId,
        string abortReasonMessage)
    {

        // JOB取得
        JobModel job =
            await jobs.GetJobByIdAsync(connection, transaction, jobId) ??
            throw new InvalidOperationException($"DBデータ不正 EquipmentId={equipmentId}");

        // 出庫JOBかつJob状態がAssignedならば、Itemの在庫状態を保管中にする
        if (job.JobType == JobType.Picking &&
            job.JobStatus == JobStatus.Assigned)
        {

            /*
             * 
             * items.stock_status_id を StockStatus.Stored に変更
             * 
             */

        }

        // JOBを異常終了させる
        await jobs.UpdateJobStatusByIdAsync(
            connection,
            transaction,
            job.JobId,
            job.JobStatus,
            JobStatus.Aborted);

        logger.LogWarning(
            "JOB異常終了 JobId={JobId} Reason={Reason}",
            job.JobId,
            abortReasonMessage);
    }

}
