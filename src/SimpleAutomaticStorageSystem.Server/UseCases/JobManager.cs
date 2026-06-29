using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.Shared.Settings;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobManager(
    IOptions<DatabaseSettings> settings,
    IOptions<EquipmentSettings> eqSettings,
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
                    // 現在JOB状態 = 次JOB状態
                    [JobStatus.Assigned] = JobStatus.Transferring, // 割当済み→搬送中
                    [JobStatus.Transferring] = JobStatus.WaitOut, // 搬送中→取出待ち
                    [JobStatus.WaitOut] = JobStatus.Completed, // 取出待ち→完了
                },
                [JobType.Putaway] = new Dictionary<JobStatus, JobStatus> // 入庫JOBの場合
                {
                    // 現在JOB状態 = 次JOB状態
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
                    // 次JOB状態 = 次保管状態
                    [JobStatus.Transferring] = StockStatus.Transferring, // 搬送中→搬送中
                    [JobStatus.WaitOut] = StockStatus.Transferring, // 取出待ち→搬送中
                    [JobStatus.Completed] = StockStatus.Picked // 完了→出庫済み
                },
                [JobType.Putaway] = new Dictionary<JobStatus, StockStatus>
                {
                    // 次JOB状態 = 次保管状態
                    [JobStatus.Transferring] = StockStatus.Transferring, // 搬送中→搬送中
                    [JobStatus.Completed] = StockStatus.Stored // 完了→保管中
                },
            };

    // 自動倉庫の容量
    private readonly Dictionary<string, int> _capacities =
        eqSettings.Value.Equipments.ToDictionary(
            x => x.EquipmentId,
            x => x.Capacity);


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
            // JOB番号を指定して、ロック付きでJOBを取得する
            JobModel currentJob =
                await jobs.GetJobByIdForUpdateAsync(connection, transaction, jobId) ??
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
            ItemModel assignedItem =
                await items.GetItemByIdForUpdateAsync(connection, transaction, itemId) ??
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
            int affectedJobRows = nextJobStatus switch
            {
                // 次JOB状態が搬送中または取出待ちならば、JOB状態と遷移日時のみ更新する
                JobStatus.Transferring or
                JobStatus.WaitOut =>
                    await jobs.UpdateJobStatusByIdAsync(
                        connection,
                        transaction,
                        jobId,
                        currentJob.JobType,
                        currentJob.JobStatus,
                        nextJobStatus),
                // 次JOB状態が完了ならば、JOB状態と遷移日時、終了日時を更新する
                JobStatus.Completed =>
                    await jobs.CloseJobByIdAsync(
                        connection,
                        transaction,
                        jobId,
                        currentJob.JobType,
                        currentJob.JobStatus,
                        nextJobStatus),
                // それ以外は例外
                _ => throw new InvalidOperationException(
                        $"次JOB状態が不正。JobId={jobId} NextJobStatus={nextJobStatus}")
            };

            if (affectedJobRows != 1)
            {
                throw new InvalidOperationException(
                    $"JOB状態更新に失敗しました。JobId={jobId} CurrentStatus={currentJob.JobStatus} NextStatus={nextJobStatus} AffectedRows={affectedJobRows}");
            }

            // 保管状態を更新する
            int affectedItemRows = nextItemStatus switch
            {
                // 次保管状態が搬送中または保管中ならば、保管状態のみ更新する
                StockStatus.Transferring or
                StockStatus.Stored =>
                    await items.UpdateItemStatusByIdAsync(
                        connection, transaction, itemId, assignedItem.Status, nextItemStatus),
                // 次保管状態が出庫済みならば、保管状態と出庫日時を更新する
                StockStatus.Picked =>
                    await items.PickItemByIdAsync(
                        connection, transaction, itemId),
                // それ以外は例外スロー
                _ => throw new InvalidOperationException(
                        $"次保管状態が不正。ItemId={itemId} NextStockStatus={nextItemStatus}")
            };

            if (affectedItemRows != 1)
            {
                throw new InvalidOperationException(
                    $"商品の状態更新に失敗しました。 ItemId={assignedItem.ItemId}  CurrentStatus={assignedItem.Status}  NextStatus={nextItemStatus}  AffectedRows={affectedItemRows}");
            }

            // 次JOB状態が完了ならば、装置が保持するJOBを解除する
            if (nextJobStatus is JobStatus.Completed)
            {
                int affectedEquipmentRows = currentJob.JobType switch
                {
                    JobType.Picking =>
                        await equipments.ReleasePickingJobAsync(
                            connection, transaction, equipmentId, jobId),
                    JobType.Putaway =>
                        await equipments.ReleasePutawayJobAsync(
                            connection, transaction, equipmentId, jobId),
                    _ => throw new InvalidOperationException(
                        $"JOB種別が不正。JobID={jobId} JobType={currentJob.JobType}")
                };

                if (affectedEquipmentRows != 1)
                {
                    throw new InvalidOperationException(
                        $"装置状態更新に失敗しました。EquipmentId={equipmentId} AffectedRows={affectedEquipmentRows}");
                }
            }

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
            // JOB番号を指定して、ロック付きでJOBを取得する
            JobModel currentJob =
                await jobs.GetJobByIdForUpdateAsync(connection, transaction, jobId) ??
                throw new JobNotFoundException();

            // 指定されたスマホは、JOBの依頼送信元と同じか
            ValidateDeviceOwnership(currentJob, deviceId);

            if(currentJob.JobStatus != JobStatus.Unassigned)
            {
                // Unassignedでなければ、キャンセルを拒否する
                throw new InvalidStatusException();
            }

            // JOB状態をキャンセルにする
            int affectedJobRows =
                await jobs.CloseJobByIdAsync(
                    connection,
                    transaction,
                    jobId,
                    currentJob.JobType,
                    currentJob.JobStatus,
                    JobStatus.Canceled);

            if (affectedJobRows != 1)
            {
                throw new InvalidOperationException(
                    $"JOB状態更新に失敗しました。 JobId={jobId}  CurrentStatus={currentJob.JobStatus}  NextStatus={JobStatus.Canceled}  AffectedRows={affectedJobRows}");
            }

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
            // JOB番号を指定して、ロック付きでJOBを取得する
            JobModel currentJob =
                await jobs.GetJobByIdForUpdateAsync(connection, transaction, jobId) ??
                throw new JobNotFoundException();

            if (currentJob.JobStatus != JobStatus.Unassigned)
            {
                // Unssignedでなければ、異常終了を拒否する
                throw new InvalidStatusException();
            }

            // JOBを異常終了させる
            int affectedJobRows =
                await jobs.CloseJobByIdAsync(
                    connection,
                    transaction,
                    jobId,
                    currentJob.JobType,
                    currentJob.JobStatus,
                    JobStatus.Aborted);

            if (affectedJobRows != 1)
            {
                throw new InvalidOperationException(
                    $"JOB状態更新に失敗しました。 JobId={jobId}  CurrentStatus={currentJob.JobStatus}  NextStatus={JobStatus.Aborted}  AffectedRows={affectedJobRows}");
            }

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
    /// 自動倉庫の保持JOBを異常終了させ、オンラインにする。
    /// </summary>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="abortReasonMessage">中断事由</param>
    /// <returns></returns>
    public async Task ChangeEquipmentOnlineAsync(
        string equipmentId, string abortReasonMessage) =>
        await ChangeEquipmentStateAsync(
            equipmentId, EquipmentStatus.Online, abortReasonMessage);

    /// <summary>
    /// 自動倉庫の保持JOBを異常終了させ、オフラインにする。
    /// </summary>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="abortReasonMessage">中断事由</param>
    /// <returns></returns>
    public async Task ChangeEquipmentOfflineAsync(
        string equipmentId, string abortReasonMessage) =>
        await ChangeEquipmentStateAsync(
            equipmentId, EquipmentStatus.Offline, abortReasonMessage);



    // =========================
    //   プライベートメソッド
    // =========================

    // JOBを異常終了させた後、自動倉庫を指定した状態にする。
    private async Task ChangeEquipmentStateAsync(
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
            EquipmentModel? currentEquipment =
                await equipments.GetEquipmentByIdForUpdateAsync(connection, transaction, equipmentId) ??
                throw new KeyNotFoundException($"自動倉庫ID：{equipmentId}は存在しない。");

            // 装置が保持する出庫JOBを取得
            string? pickingJobId = currentEquipment.PickingJobId;

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
            string? putawayJobId = currentEquipment.PutawayJobId;

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

            // 空き容量の設定
            int availableCapacity = nextStatus switch
            {
                EquipmentStatus.Online =>
                    await GetAvailableCapacityAsync(connection, transaction, equipmentId), // オンラインなら更新する
                EquipmentStatus.Offline =>
                    currentEquipment.AvailableCapacity, // オフラインでは更新しない
                _ =>
                    throw new InvalidOperationException(
                    $"次の自動倉庫状態が不正 nextStatus={nextStatus}")
            };

            // 装置が保持するJOBを解除し、装置状態を nextStatus にする
            int affectedEquipmentRows =
                await equipments.UpdateEquipmentByIdAsync(
                    connection,
                    transaction,
                    equipmentId,
                    availableCapacity,
                    currentEquipment.Status,
                    nextStatus);

            // 更新失敗検知
            if (affectedEquipmentRows != 1)
            {
                throw new InvalidOperationException(
                    $"装置状態の更新に失敗。EquipmentId={equipmentId}");
            }

            // コミット
            await transaction.CommitAsync();


            // ログ処理
            if (pickingJobId is not null)
            {
                logger.LogWarning(
                    "JOB異常終了 JobId={JobId} EquipmentId={EquipmentId} Reason={Reason}",
                    pickingJobId,
                    equipmentId,
                    abortReasonMessage);
            }

            if (putawayJobId is not null)
            {
                logger.LogWarning(
                    "JOB異常終了 JobId={JobId} EquipmentId={EquipmentId} Reason={Reason}",
                    putawayJobId,
                    equipmentId,
                    abortReasonMessage);
            }

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
    /// 自動倉庫の空き容量を取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <returns>空き容量</returns>
    private async Task<int> GetAvailableCapacityAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId)
    {
        // 装置の総容量を取得
        if (!_capacities.TryGetValue(equipmentId, out int total))
        {
            throw new InvalidOperationException(
                $"自動倉庫ID:{equipmentId} の容量設定が存在しません。");
        }

        int stockCount = 
            await items.GetStockCountByEquipmentAsync(
                connection, transaction, equipmentId);

        int availableCapacity = total - stockCount;

        if (availableCapacity < 0)
        {
            throw new InvalidOperationException(
                $"自動倉庫ID:{equipmentId} の在庫数が容量を超えています。");
        }

        return availableCapacity;
    }


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
        SqlTransaction? transaction,
        string equipmentId,
        string jobId,
        string abortReasonMessage)
    {
        // JOB番号を指定して、ロック付きでJOBを取得する
        JobModel currentJob =
            await jobs.GetJobByIdForUpdateAsync(connection, transaction, jobId) ??
            throw new InvalidOperationException($"DBデータ不正 EquipmentId={equipmentId}");

        // 商品を取得
        ArgumentNullException.ThrowIfNullOrWhiteSpace(currentJob.ItemId);

        ItemModel assignedItem =
            await items.GetItemByIdForUpdateAsync(connection, transaction, currentJob.ItemId) ??
            throw new InvalidOperationException($"DBデータが不正です。 JobId={jobId}");

        // 商品状態を更新
        int affectedItemRows = assignedItem.Status switch
        {
            // 商品は棚に残っているので、保管中へ戻す
            StockStatus.Reserved =>
                await items.UpdateItemStatusByIdAsync(
                        connection,
                        transaction,
                        currentJob.ItemId,
                        StockStatus.Reserved,
                        StockStatus.Stored),

            // 商品は棚から搬送されたので、管理対象外とする
            StockStatus.Transferring =>
                await items.UpdateItemStatusByIdAsync(
                        connection,
                        transaction,
                        currentJob.ItemId,
                        StockStatus.Transferring,
                        StockStatus.OutOfControl),

            // それ以外は状態不整合
            _ => throw new InvalidOperationException(
                    $"商品の状態が不正です。ItemId={assignedItem.ItemId} Status={assignedItem.Status}")
        };

        if (affectedItemRows != 1)
        {
            throw new InvalidOperationException(
                $"商品の状態更新に失敗しました。ItemId={assignedItem.ItemId} CurrentStatus={assignedItem.Status} AffectedRows={affectedItemRows}");
        }

        // JOBを異常終了させる
        int affectedJobRows =
            await jobs.CloseJobByIdAsync(
                connection,
                transaction,
                currentJob.JobId,
                currentJob.JobType,
                currentJob.JobStatus,
                JobStatus.Aborted);

        if (affectedJobRows != 1)
        {
            throw new InvalidOperationException(
                $"JOB状態更新に失敗しました。 JobId={jobId}  CurrentStatus={currentJob.JobStatus}  NextStatus={JobStatus.Aborted}  AffectedRows={affectedJobRows}");
        }
    }

}
