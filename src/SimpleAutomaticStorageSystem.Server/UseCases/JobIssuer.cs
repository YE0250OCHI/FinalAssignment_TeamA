using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Controllers;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases.Dto;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobIssuer(
    ILogger<RacksApiController> logger,
    IConfiguration config,
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


    // 採番メソッド


    // 

    private async Task<string> CreateJobAsync(
        JobType jobType,
        string? deviceId,
        string itemCode)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // トランザクション開始
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // 品種は正しいか
            ItemTypeModel itemType =
                await items.GetItemTypeAsync(connection, transaction, itemCode) ??
                throw new InvalidItemCodeException();

            // JOB番号採番
            string jobId = await jobs.GenerateJobIdAsync(
                connection,
                transaction);

            // JOB作成DTO
            CreateJobDto newJob = new CreateJobDto
            {
                JobId = jobId,
                JobType = jobType,
                JobStatus = JobStatus.Unassigned,
                DeviceId = deviceId,
                ItemCode = itemCode,
                ItemId = null,
                EquipmentId = null
            };

            // jobsテーブル登録
            await jobs.CreateJobAsync(
                connection,
                transaction,
                newJob);

            // コミット
            await transaction.CommitAsync();

            logger.LogInformation(
                "JOB作成成功 JobId={JobId} ItemCode={ItemCode}",
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

            logger.LogWarning(
                "JOB作成失敗 ItemCode={ItemCode}",
                itemCode);

            throw;

        }

    }





}
