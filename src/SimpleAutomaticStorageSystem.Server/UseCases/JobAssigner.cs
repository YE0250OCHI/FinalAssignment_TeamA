using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.Shared.Settings;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using SimpleAutomaticStorageSystem.Server.UseCases.UseCaseDto;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobAssigner(
    IOptions<DatabaseSettings> settings,
    ILogger<JobAssigner> logger,
    IJobsRepository jobs,
    IItemsRepository items,
    IEquipmentsRepository equipments)
{
    // DB接続文字列
    private readonly string _defaultConnection = settings.Value.DefaultConnection;


    // =========================
    //   公開メソッド
    // =========================

    /// <summary>
    /// JOB番号を指定して、商品の割り当てを実行する
    /// </summary>
    /// <param name="jobId">JOB番号</param>
    public async Task<AssignedJobDto?> AssignItemForJobAsync(string jobId)
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

            // Jobは商品割り当てが可能な状態か、不可なら不正状態例外スロー
            if (currentJob.ItemId is not null ||
                currentJob.EquipmentId is not null ||
                currentJob.JobStatus != JobStatus.Unassigned)
            {
                // 割り当てを拒否
                throw new InvalidStatusException();
            }

            // 割り当て可能な在庫を取得、なければ割り当てを終了
            ItemModel? availableItem =
                await items.GetAvailableItemForUpdateAsync(connection, transaction, currentJob.ItemCode);

            if(availableItem is null)
            {
                return null;
            }

            // 装置IDを取得
            string equipmentId = availableItem.EquipmentId;

            // 商品状態を更新
            int affectedItemRows =
                await items.UpdateItemStatusByIdAsync(
                    connection,
                    transaction,
                    availableItem.ItemId,
                    availableItem.Status,
                    StockStatus.Reserved);

            // JOBに商品、自動倉庫を割り当て
            int affectedJobRows =
                await jobs.AssignJobAsync(
                    connection,
                    transaction,
                    jobId,
                    availableItem.ItemId,
                    equipmentId);

            // 装置状態を更新
            int affectedEquipmentRows = currentJob.JobType switch
            {
                JobType.Picking =>
                    await equipments.AssignPickingJobAsync(
                        connection,
                        transaction,
                        equipmentId,
                        jobId),
                JobType.Putaway =>
                    await equipments.AssignPutawayJobAsync(
                        connection,
                        transaction,
                        equipmentId,
                        jobId),
                _ => throw new InvalidOperationException(
                    $"JOB種別が不正 JobId={jobId} JobType={currentJob.JobType}")
            };

            // 更新失敗を検知
            if (affectedItemRows != 1 ||
                affectedJobRows != 1 ||
                affectedEquipmentRows != 1)
            {
                throw new InvalidOperationException(
                    $"JOB割当に失敗した。JobId={jobId}, ItemId={availableItem.ItemId}, EquipmentId={equipmentId}, JobRows={affectedJobRows} ItemRows={affectedItemRows} EquipmentRows={affectedEquipmentRows}");
            }

            // コミット
            await transaction.CommitAsync();

            // ログ
            logger.LogInformation(
                "JOB割当成功 JobId={JobId} ItemId={ItemId} EquipmentId={EquipmentId}",
                jobId,
                availableItem.ItemId,
                equipmentId);

            // 割り当て済みJOBの返却
            return new()
            {
                JobId = jobId,
                JobType = currentJob.JobType,
                ItemId = availableItem.ItemId,
                EquipmentId = equipmentId
            };

        }
        catch (Exception ex)
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
                ex,
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
                await equipments.GetEquipmentByIdForUpdateAsync(connection, transaction, equipmentId) ??
                throw new KeyNotFoundException($"自動倉庫ID：{equipmentId}は存在しない。");

            // 自動倉庫はJOB割り当てが可能な状態か、不可なら不正状態例外スロー
            if (equipmentModel.PickingJobId is not null ||
                equipmentModel.Status != EquipmentStatus.Online)
            {
                // すでにJOBを配信済み
                throw new AlreadyPostedException();
            }

            // 未割当JOB一覧を取得
            List<JobModel> incompleteJobs = [
                .. await jobs.GetUnassignedPickingJobsForUpdateAsync(connection, transaction)];

            // 対応可能なJOBを検索し、そのJOB割当可能な在庫を検索する
            JobModel? targetJob = null;
            ItemModel? targetItem = null;

            foreach (JobModel j in incompleteJobs)
            {
                targetItem =
                    await items.GetPickableItemForUpdateAsync(
                        connection, transaction, j.ItemCode, equipmentId);

                if(targetItem is not null)
                {
                    // 割当可能な商品が見つかった
                    targetJob = j;

                    break;

                }
                
            }

            // なければ、割当可能JOBなしとして終了
            if (targetItem is null || targetJob is null)
            {
                // ロールバック
                await transaction.RollbackAsync();

                // ログ
                logger.LogInformation(
                    "割当可能な出庫JOBなし EquipmentId={EquipmentId}",
                    equipmentId);

                // JOBなしで返す
                return null;

            }

            // 商品状態を更新
            int affectedItemRows =
                await items.UpdateItemStatusByIdAsync(
                    connection,
                    transaction,
                    targetItem.ItemId,
                    targetItem.Status,
                    StockStatus.Reserved);

            // JOBに商品、自動倉庫を割り当て
            int affectedJobRows =
                await jobs.AssignJobAsync(
                    connection,
                    transaction,
                    targetJob.JobId,
                    targetItem.ItemId,
                    equipmentId);

            // 自動倉庫に出庫JOBを割り当て
            int affectedEquipmentRows =
                await equipments.AssignPickingJobAsync(
                    connection,
                    transaction,
                    equipmentId,
                    targetJob.JobId);

            // 更新失敗を検知
            if (affectedItemRows != 1 ||
                affectedJobRows != 1 ||
                affectedEquipmentRows != 1)
            {
                throw new InvalidOperationException(
                    $"JOB割当に失敗した。JobId={targetJob.JobId}, ItemId={targetItem.ItemId}, EquipmentId={equipmentId}, JobRows={affectedJobRows} ItemRows={affectedItemRows} EquipmentRows={affectedEquipmentRows}");
            }

            // コミット
            await transaction.CommitAsync();

            // ログ
            logger.LogInformation(
                "JOB割当成功 JobId={JobId} ItemId={ItemId} EquipmentId={EquipmentId}",
                targetJob.JobId,
                targetItem.ItemId,
                equipmentId);

            // 割当に成功
            return new()
            {
                JobId = targetJob.JobId,
                JobType = targetJob.JobType,
                ItemId = targetItem.ItemId,
                EquipmentId = equipmentId
            };

        }
        catch (Exception ex)
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
                ex,
                "自動倉庫へのJOB割当失敗 EquipmentId={EquipmentId}",
                equipmentId);

            throw;

        }

    }

}
