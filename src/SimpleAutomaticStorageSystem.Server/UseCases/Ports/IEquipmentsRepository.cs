using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Ports;

/// <summary>
/// equipmentsテーブル操作
/// </summary>
public interface IEquipmentsRepository
{
    // =========================
    //   参照
    // =========================

    /// <summary>
    /// 自動倉庫をID指定で取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <returns>EquipmentModel</returns>
    Task<EquipmentModel?> GetEquipmentByIdForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId);

    // =========================
    //   更新
    // =========================

    /// <summary>
    /// 出庫JOBの割り当て
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="pickingJobId">出庫JOB番号</param>
    /// <returns>影響した行数</returns>
    Task<int> AssignPickingJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId,
        string pickingJobId);

    /// <summary>
    /// 出庫JOBの解除
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="pickingJobId">現在実行中の出庫JOB番号</param>
    /// <returns>影響した行数</returns>
    Task<int> ReleasePickingJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId,
        string pickingJobId);

    /// <summary>
    /// 入庫JOBの割り当て
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="putawayJobId">入庫JOB番号</param>
    /// <returns>影響した行数</returns>
    Task<int> AssignPutawayJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId,
        string putawayJobId);

    /// <summary>
    /// 入庫JOBの解除
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="putawayJobId">現在実行中の入庫JOB番号</param>
    /// <returns>影響した行数</returns>
    Task<int> ReleasePutawayJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId,
        string putawayJobId);

    /// <summary>
    /// 自動倉庫をID指定で、状態変更する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="availableCapacity">空き容量</param>
    /// <param name="currentStatus">現在の自動倉庫状態</param>
    /// <param name="nextStatus">次の自動倉庫状態</param>
    /// <returns>影響した行数</returns>
    Task<int> UpdateEquipmentByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId,
        int availableCapacity,
        EquipmentStatus currentStatus,
        EquipmentStatus nextStatus);
}
