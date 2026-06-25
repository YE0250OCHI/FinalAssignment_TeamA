using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobManager(
    IOptions<DatabaseSettings> settings,
    ILogger<JobManager> logger,
    IJobsRepository jobs,
    IItemsRepository items,
    IEquipmentsRepository equipments)
{
    // DB接続文字列
    private readonly string _defaultConnection = settings.Value.DefaultConnection;

    // JOB状態遷移マップ
    private readonly Dictionary<
        JobType,
        Dictionary<JobStatus, JobStatus>>
        _jobStatusTransitionMap =
            new()
            {
                [JobType.Picking] = new Dictionary<JobStatus, JobStatus> // 出庫JOBの場合
                {
                    [JobStatus.Assigned] = JobStatus.Transferring, // 割当済み→搬送中
                    [JobStatus.Transferring] = JobStatus.WaitOut, // 搬送中→取出待ち
                    [JobStatus.WaitOut] = JobStatus.Completed, // 取出待ち→完了
                },
                [JobType.Putaway] = new Dictionary<JobStatus, JobStatus> // 入庫JOBの場合
                {
                    [JobStatus.Assigned] = JobStatus.Transferring, // 割当済み→搬送中
                    [JobStatus.Transferring] = JobStatus.Completed, // 搬送中→完了
                }
            };

    // 在庫状態遷移マップ
    private readonly Dictionary<
        JobType,
        Dictionary<JobStatus, StockStatus>>
        _stockStatusTransitionMap =
            new()
            {
                [JobType.Picking] = new Dictionary<JobStatus, StockStatus>
                {
                    [JobStatus.Transferring] = StockStatus.Transferring, // 搬送中→搬送中
                    [JobStatus.WaitOut] = StockStatus.None, // 取出待ち→管理外
                    [JobStatus.Completed] = StockStatus.None // 完了→管理外
                },
                [JobType.Putaway] = new Dictionary<JobStatus, StockStatus>
                {
                    [JobStatus.Transferring] = StockStatus.Transferring, // 搬送中→搬送中
                    [JobStatus.Completed] = StockStatus.Stored // 完了→保管中
                },
            };


    // =========================
    //   公開メソッド
    // =========================

    /// <summary>
    /// JOB状態を正常遷移させる
    /// </summary>
    /// <param name="jobId">JOB番号</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="nextStatus">次JOB状態</param>
    public async Task ChangeJobStatusAsync(
        string jobId,
        string equipmentId,
        JobStatus nextJobStatus)
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

            // 指定された自動倉庫は、JOB割当済みの自動倉庫と同じか
            ValidateEquipmentOwnership(currentJob, equipmentId);

            // 指定された状態遷移が正しいか検証する
            if (!_jobStatusTransitionMap.TryGetValue(currentJob.JobType, out var jobStatusMap) ||
                !jobStatusMap.TryGetValue(currentJob.JobStatus, out JobStatus idealNextStatus) ||
                nextJobStatus != idealNextStatus)
            {
                // 取得できない、または、指定状態と不一致の場合は業務例外をスロー
                throw new InvalidStatusException();
            }

            // 商品IDを取得、商品が割り当てられていなければ例外スロー
            string? itemId =
                currentJob.ItemId ??
                throw new InvalidOperationException(
                    $"商品IDが設定されていない。 JobId={jobId}");

            // 商品を取得
            ItemModel itemModel =
                await items.GetItemByIdAsync(connection, transaction, itemId) ??
                throw new KeyNotFoundException(
                    $"商品ID：{itemId}は存在しない。");

            // 次JOB状態に対応する次在庫状態を取得する
            if (!_stockStatusTransitionMap.TryGetValue(currentJob.JobType, out var stockStatusMap) ||
                !stockStatusMap.TryGetValue(nextJobStatus, out var nextItemStatus))
            {
                throw new InvalidOperationException(
                    $"商品の保管状態が不正。 ItemId={itemId}");
            }

            // JOB状態を更新する
            await jobs.UpdateJobStatusByIdAsync(
                connection,
                transaction,
                jobId,
                currentJob.JobStatus,
                nextJobStatus);

            // 保管状態を更新する
            await items.UpdateItemStatusByIdAsync(
                connection,
                transaction,
                itemId,
                itemModel.Status,
                nextItemStatus);

            // コミット
            await transaction.CommitAsync();

        }
        catch
        {
            try
            {
                // ロールバックしてスロー
                await transaction.RollbackAsync();
            }
            catch
            {
                /* ロールバック失敗は無視 */
            }

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

            // 指定されたスマホは、JOBの依頼送信元と同じか
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

            // コミット
            await transaction.CommitAsync();

            // ログ
            logger.LogInformation(
                "JOBキャンセル JobId={JobId} DeviceId={DeviceId} Reason={Reason}",
                jobId,
                deviceId,
                "端末要求");

        }
        catch
        {
            try
            {
                // ロールバックしてスロー
                await transaction.RollbackAsync();
            }
            catch
            {
                /* ロールバック失敗は無視 */
            }

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

            // コミット
            await transaction.CommitAsync();

            // ログ
            logger.LogWarning(
                "JOB異常終了 JobId={JobId} Reason={Reason}",
                jobId,
                abortReasonMessage);

        }
        catch
        {
            try
            {
                // ロールバックしてスロー
                await transaction.RollbackAsync();
            }
            catch
            {
                /* ロールバック失敗は無視 */
            }

            throw;

        }

    }

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
                await equipments.GetEquipmentByIdAsync(connection, transaction, equipmentId) ??
                throw new KeyNotFoundException($"自動倉庫ID：{equipmentId}は存在しない。");

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

            if (putawayJobId is not null)
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
            try
            {
                // ロールバックしてスロー
                await transaction.RollbackAsync();
            }
            catch
            {
                /* ロールバック失敗は無視 */
            }

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
    private async Task AbortJobAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string equipmentId,
        string jobId,
        string abortReasonMessage)
    {

        // JOB取得
        JobModel jobModel =
            await jobs.GetJobByIdAsync(connection, transaction, jobId) ??
            throw new InvalidOperationException($"DBデータ不正 EquipmentId={equipmentId}");

        // 商品を取得
        ArgumentNullException.ThrowIfNullOrWhiteSpace(jobModel.ItemId);

        ItemModel itemModel =
            await items.GetItemByIdAsync(connection, transaction, jobModel.ItemId) ??
            throw new InvalidOperationException($"DBデータが不正です。 JobId={jobId}");

        // Itemの保管状態がReservedならば、Storedにする
        if (itemModel.Status == StockStatus.Reserved)
        {
            // 商品状態を更新
            await items.UpdateItemStatusByIdAsync(
                connection,
                transaction,
                jobModel.ItemId,
                StockStatus.Reserved,
                StockStatus.Stored);

        }

        // JOBを異常終了させる
        await jobs.UpdateJobStatusByIdAsync(
            connection,
            transaction,
            jobModel.JobId,
            jobModel.JobStatus,
            JobStatus.Aborted);

        logger.LogWarning(
            "JOB異常終了 JobId={JobId} Reason={Reason}",
            jobModel.JobId,
            abortReasonMessage);
    }

}
