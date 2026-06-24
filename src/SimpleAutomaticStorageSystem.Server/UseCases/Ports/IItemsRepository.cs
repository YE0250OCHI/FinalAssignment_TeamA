using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Ports;

public interface IItemsRepository
{
    // =========================
    //   参照：エンティティ
    // =========================

    /// <summary>
    /// 在庫商品をID指定で取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemId">商品ID</param>
    /// <returns>EquipmentModel</returns>
    Task<ItemModel?> GetItemByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId);


    // =========================
    //   更新
    // =========================

    /// <summary>
    /// 在庫商品の保管状態を、ID指定で変更する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemId">商品ID</param>
    /// <param name="currentStatus">現在の保管状態</param>
    /// <param name="nextStatus">次の保管状態</param>
    /// <returns></returns>
    Task UpdateItemStatusByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId,
        StockStatus currentStatus,
        StockStatus nextStatus);
}
