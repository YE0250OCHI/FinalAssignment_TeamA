using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Ports;

/// <summary>
/// equipmentsテーブル操作
/// </summary>
public interface IEquipmentsRepository
{
    // =========================
    //   参照：エンティティ
    // =========================

    /// <summary>
    /// 自動倉庫をID指定で取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <returns>EquipmentModel</returns>
    Task<EquipmentModel?> GetEquipmentByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId);

    // =========================
    //   更新
    // =========================

    /// <summary>
    /// 自動倉庫をID指定で、状態変更する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <param name="currentStatus">現在の自動倉庫状態</param>
    /// <param name="nextStatus">次の自動倉庫状態</param>
    /// <returns></returns>
    Task UpdateEquipmentStatusByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId,
        EquipmentStatus currentStatus,
        EquipmentStatus nextStatus);

}
