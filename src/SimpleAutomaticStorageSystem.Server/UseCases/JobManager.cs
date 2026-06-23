using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobManager(
    IConfiguration config,
    ILogger<JobManager> logger,
    IJobsRepository jobs,
    IItemsRepository items,
    IEquipmentsRepository equipments)
{
    // JOB異常終了の理由
    private const string INVALID_STATUS_TRANSITION = "";
    private const string TIMEOUT = "";
    private const string EQUIPMENT_STOPPED = "";

    // DB接続文字列
    private readonly string DefaultConnection =
        config.GetConnectionString("DefaultConnection") ?? string.Empty;


    /// <summary>
    /// 装置の保持JOBを異常終了させ、装置をオフライン状態にする。
    /// </summary>
    /// <returns></returns>
    public async Task SetOnlineAsync(string equipmentId) =>
        await SetEquipmentStatusAsync(equipmentId, EquipmentStatus.Online);

    /// <summary>
    /// 装置の保持JOBを異常終了させ、装置をオフライン状態にする。
    /// </summary>
    /// <returns></returns>
    public async Task SetOfflineAsync(string equipmentId) =>
        await SetEquipmentStatusAsync(equipmentId, EquipmentStatus.Offline);



    // =========================
    //   プライベートメソッド
    // =========================

    /// <summary>
    /// 装置の保持JOBを異常終了させ、装置を指定した状態にする。
    /// </summary>
    /// <returns></returns>
    public async Task SetEquipmentStatusAsync(string equipmentId, EquipmentStatus nextStatus)
    {
        // IDの空白がないか
        ArgumentNullException.ThrowIfNullOrWhiteSpace(equipmentId);

        // DB接続開始
        await using SqlConnection connection = new(DefaultConnection);
        await connection.OpenAsync();

        // トランザクション開始
        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // 装置が保持する出庫JOBを異常終了にする
            string? PickingJobId;


            logger.LogWarning(
                "JOB異常終了 JobId={JobId} Reason={Reason}",
                PickingJobId,
                "装置停止による強制終了");

            // 装置が保持する入庫JOBを異常終了にする
            string? PutawayJobId;



            // 装置が保持するJOBを解除する

            // 装置状態を nextStatus にする

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

}
