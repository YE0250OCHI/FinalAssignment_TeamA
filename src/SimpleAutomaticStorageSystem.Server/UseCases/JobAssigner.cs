using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases.Dto;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobAssigner(
    IConfiguration config,
    ILogger<JobManager> logger,
    IJobsRepository jobs,
    IItemsRepository items,
    IEquipmentsRepository equipments)
{
    // DB接続文字列
    private readonly string _defaultConnection =
        config.GetConnectionString("DefaultConnection") ?? string.Empty;


    // =========================
    //   公開メソッド
    // =========================

    /// <summary>
    /// JOB番号を指定して、商品の割り当てを実行する
    /// </summary>
    /// <param name="jobId">JOB番号</param>
    public async Task AssignItemForJobAsync(string jobId)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // トランザクション開始
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // JobIdが存在するか、なければ存在なし例外スロー
            JobModel currentJob =
                await jobs.GetJobByIdAsync(connection, transaction, jobId) ??
                throw new JobNotFoundException();

            // Jobは商品割り当てが可能な状態か、不可なら不正状態例外スロー
            if (currentJob.ItemId is not null ||
                currentJob.EquipmentId is not null ||
                currentJob.JobStatus != JobStatus.Unassigned)
            {
                // 割り当てを拒否
                throw new InvalidStatusException();
            }


            // 割り当て可能な在庫を取得、なければ在庫なし例外スロー
            ItemModel availableItem =
                await items.GetAvailableItemAsync(connection, transaction, currentJob.ItemCode) ??
                throw new OutOfStockException();

            // 装置IDを取得
            string equipmentId = availableItem.EquipmentId;

            // 商品状態を更新
            await items.UpdateItemStatusByIdAsync(
                connection,
                transaction,
                availableItem.ItemId,
                availableItem.Status,
                StockStatus.Reserved);

            // JOBに商品、自動倉庫を割り当て
            await jobs.AssignJobAsync(
                connection,
                transaction,
                jobId,
                availableItem.ItemId,
                equipmentId);

            // 装置状態を更新
            if (currentJob.JobType == JobType.Picking)
            {
                // 出庫JOB
                await equipments.AssignPickingJobAsync(
                    connection,
                    transaction,
                    equipmentId,
                    jobId);
            }
            else
            {
                // 入庫JOB
                await equipments.AssignPutawayJobAsync(
                    connection,
                    transaction,
                    equipmentId,
                    jobId);
            }

            // コミット
            await transaction.CommitAsync();

            // ログ
            logger.LogInformation(
                "JOB割当成功 JobId={JobId} ItemId={ItemId} EquipmentId={EquipmentId}",
                jobId,
                availableItem.ItemId,
                equipmentId);


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

            // ログ
            logger.LogWarning(
                "JOBへの商品割当失敗 JobId={JobId}",
                jobId);

            throw;

        }

    }

    /// <summary>
    /// 出庫JOBの割り当て
    /// </summary>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <returns>割り当てられた出庫JOB</returns>
    public async Task<AssignedJobDto?> AssignPickingJobForEquipmentAsync(string equipmentId)
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

            // 自動倉庫はJOB割り当てが可能な状態か、不可なら不正状態例外スロー
            if (equipmentModel.PickingJobId is not null ||
                equipmentModel.Status != EquipmentStatus.Online)
            {
                // 割り当てを拒否
                throw new InvalidStatusException();
            }

            // 未割当JOB一覧を取得
            List<JobModel> incompleteJobs =
                await jobs.GetUnassignedPickingJobsAsync(connection, transaction);

            // 在庫が存在するJOBを検索し、そのJOB割当可能な在庫を検索する
            JobModel? jobModel = null;
            ItemModel? itemModel = null;

            foreach (JobModel j in incompleteJobs)
            {
                itemModel =
                    await items.GetPickableItemAsync(
                        connection, transaction, j.ItemCode, equipmentId);

                if(itemModel is not null)
                {
                    // 割当可能な商品が見つかった
                    jobModel = j;

                    break;

                }
                
            }

            // なければ、割当可能JOBなしとして終了
            if (itemModel is null || jobModel is null)
            {
                await transaction.RollbackAsync();

                logger.LogInformation(
                    "割当可能な出庫JOBなし EquipmentId={EquipmentId}",
                    equipmentId);

                return null;

            }


            // 商品状態を更新
            await items.UpdateItemStatusByIdAsync(
                connection,
                transaction,
                itemModel.ItemId,
                itemModel.Status,
                StockStatus.Reserved);

            // JOBに商品、自動倉庫を割り当て
            await jobs.AssignJobAsync(
                connection,
                transaction,
                jobModel.JobId,
                itemModel.ItemId,
                equipmentId);


            // 自動倉庫に出庫JOBを割り当て
            await equipments.AssignPickingJobAsync(
                connection,
                transaction,
                equipmentId,
                jobModel.JobId);

            // コミット
            await transaction.CommitAsync();

            // ログ
            logger.LogInformation(
                "JOB割当成功 JobId={JobId} ItemId={ItemId} EquipmentId={EquipmentId}",
                jobModel.JobId,
                itemModel.ItemId,
                equipmentId);

            // 割当に成功
            return new AssignedJobDto
            {
                JobId = jobModel.JobId,
                JobType = jobModel.JobType,
                ItemId = itemModel.ItemId
            };

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

            // ログ
            logger.LogWarning(
                "自動倉庫へのJOB割当失敗 EquipmentId={EquipmentId}",
                equipmentId);

            throw;

        }

    }

    /// <summary>
    /// 入庫JOBの割り当て
    /// </summary>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="putawayJobId">入庫JOB</param>
    /// /// <param name="itemId">商品ID</param>
    /// <returns>割り当てられた入庫JOB</returns>
    public async Task<AssignedJobDto> AssignPutawayJobForEquipmentAsync(
        string equipmentId,
        string itemId,
        string putawayJobId)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // トランザクション開始
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // Jobの取得
            JobModel jobModel =
                await jobs.GetJobByIdAsync(connection, transaction, putawayJobId) ??
                throw new InvalidOperationException($"JOB番号が不正 JobId={putawayJobId}");

            // 入庫JOBの検証
            if (jobModel.JobType != JobType.Putaway ||
                jobModel.ItemId is not null ||
                jobModel.EquipmentId is not null ||
                jobModel.JobStatus != JobStatus.Unassigned)
            {
                throw new InvalidStatusException();
            }

            // 対象自動倉庫は存在するか
            EquipmentModel? equipmentModel =
                await equipments.GetEquipmentByIdAsync(connection, transaction, equipmentId) ??
                throw new KeyNotFoundException($"自動倉庫ID：{equipmentId}は存在しない。");

            // 自動倉庫はJOB割り当てが可能な状態か、不可なら不正状態例外スロー
            if (equipmentModel.PutawayJobId is not null ||
                equipmentModel.Status != EquipmentStatus.Online)
            {
                // 割り当てを拒否
                throw new InvalidStatusException();
            }

            // JOBに商品、自動倉庫を割り当て
            await jobs.AssignJobAsync(
                connection,
                transaction,
                putawayJobId,
                itemId,
                equipmentId);

            // 自動倉庫に入庫JOBを割り当て
            await equipments.AssignPutawayJobAsync(
                connection,
                transaction,
                equipmentId,
                putawayJobId);



            // コミット
            await transaction.CommitAsync();

            // ログ
            logger.LogInformation(
                "JOB割当成功 JobId={JobId} ItemId={ItemId} EquipmentId={EquipmentId}",
                jobModel.JobId,
                itemId,
                equipmentId);

            // 割当に成功
            return new AssignedJobDto
            {
                JobId = putawayJobId,
                JobType = jobModel.JobType,
                ItemId = itemId
            };

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

            // ログ
            logger.LogWarning(
                "自動倉庫へのJOB割当失敗 EquipmentId={EquipmentId}",
                equipmentId);

            throw;

        }

    }

}
