using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Dto;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using System.Data;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobIssuer(
    IOptions<DatabaseSettings> settings,
    ILogger<JobIssuer> logger,
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
    /// 入庫JOBの作成
    /// </summary>
    /// <param name="itemCode">品種コード</param>
    /// <returns>JOB番号</returns>
    public async Task<AssignedJobDto> CreatePutawayJobAsync(
        string itemCode,
        string equipmentId)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // トランザクション開始（SERIALIZABLE有効）
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            // 自動倉庫存在確認
            _ = await equipments.GetEquipmentByIdForUpdateAsync(connection, transaction, equipmentId) ??
                throw new KeyNotFoundException(
                    $"自動倉庫ID:{equipmentId}は存在しません。");

            // 品種コードは存在するか
            if (!await items.AnyItemTypeAsync(
                connection, transaction, itemCode))
            {
                throw new InvalidItemCodeException();
            }

            // 商品の採番、登録
            string itemId =
                await items.RegisterItemAsync(
                    connection, transaction, itemCode, equipmentId);

            // JOB番号採番
            string jobId =
                await jobs.GenerateJobIdAsync(connection, transaction);

            // JOB作成DTO
            CreateJobDto newJob = new()
            {
                JobId = jobId,
                JobType = JobType.Putaway,
                JobStatus = JobStatus.Assigned,
                DeviceId = null,
                ItemCode = itemCode,
                ItemId = itemId,
                EquipmentId = equipmentId
            };

            // jobsテーブル登録
            int affectedJobRows =
                await jobs.CreateJobAsync(
                    connection,
                    transaction,
                    newJob);

            // 自動倉庫の更新
            int affectedEquipmentRows =
                await equipments.AssignPutawayJobAsync(
                    connection,
                    transaction,
                    equipmentId,
                    jobId);

            // JOB作成失敗検知
            if (affectedJobRows != 1 ||
                affectedEquipmentRows != 1)
            {
                throw new InvalidOperationException(
                    $"JOBまたは装置更新に失敗 JobId={jobId} EquipmentId={equipmentId}");
            }

            // コミット
            await transaction.CommitAsync();

            // ログ
            logger.LogInformation(
                "入庫JOB作成成功 JobId={JobId} ItemCode={ItemCode} ItemId={ItemId} EquipmentId={EquipmentId}",
                jobId,
                itemCode,
                itemId,
                equipmentId);

            // 割当済みJOBデータを返却
            return new()
            {
                JobId = jobId,
                JobType = JobType.Putaway,
                ItemId = itemId,
                EquipmentId = equipmentId
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

            throw;

        }
    }

    /// <summary>
    /// 出庫JOBの作成
    /// </summary>
    /// <param name="deviceId">スマホID</param>
    /// <param name="itemCode">品種コード</param>
    /// <returns>JOB番号</returns>
    public async Task<string> CreatePickingJobAsync(
        string deviceId,
        string itemCode)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // トランザクション開始（SERIALIZABLE有効）
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            // 品種コードは存在するか
            if (!await items.AnyItemTypeAsync(
                connection, transaction, itemCode))
            {
                throw new InvalidItemCodeException();
            }                

            // JOB番号採番
            string jobId = await jobs.GenerateJobIdAsync(
                connection,
                transaction);

            // JOB作成DTO
            CreateJobDto newJob = new()
            {
                JobId = jobId,
                JobType = JobType.Picking,
                JobStatus = JobStatus.Unassigned,
                DeviceId = deviceId,
                ItemCode = itemCode,
                ItemId = null,
                EquipmentId = null
            };

            // jobsテーブル登録
            int affectedJobRows = await jobs.CreateJobAsync(
                connection,
                transaction,
                newJob);

            if (affectedJobRows != 1)
            {
                throw new InvalidOperationException(
                    $"JOB作成に失敗。 JobId={jobId}");
            }

            // コミット
            await transaction.CommitAsync();

            // ログ
            logger.LogInformation(
                "出庫JOB作成成功 JobId={JobId} ItemCode={ItemCode}",
                jobId,
                itemCode);

            return jobId;

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

}
