using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases.UseCaseDto;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Ports;

public interface IItemsRepository
{
    // =========================
    //   参照
    // =========================

    /// <summary>
    /// 在庫商品をID指定で取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemId">商品ID</param>
    /// <returns>ItemModel?</returns>
    Task<ItemModel?> GetItemByIdForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId);

    /// <summary>
    /// 品種コードを指定し、FIFOで割当可能な在庫を取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemCode">品種コード</param>
    /// <returns>割当可能な商品、存在しない場合はnull</returns>
    Task<ItemModel?> GetAvailableItemForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode);

    /// <summary>
    /// 品種コードと自動倉庫IDを指定して、割当可能な在庫を取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemCode">品種コード</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <returns>割当可能な在庫</returns>
    Task<ItemModel?> GetPickableItemForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode,
        string equipmentId);

    /// <summary>
    /// 自動倉庫IDを指定して、現在の在庫数を取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <returns>在庫数</returns>
    Task<int> GetStockCountByEquipmentAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId);

    /// <summary>
    /// ItemCodeからItemTypeを探す
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemCode">品種コード</param>
    /// <returns>存在すればture、しなければfalse</returns>
    Task<bool> AnyItemTypeAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode);

    /// <summary>
    /// 在庫登録のある商品の一覧を返す
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <returns>品種データ</returns>
    Task<IEnumerable<InventoryItemsRawInfo>> GetPickableItemListAsync(
        SqlConnection connection,
        SqlTransaction? transaction);


    // =========================
    //   作成
    // =========================

    /// <summary>
    /// 商品IDを採番し、在庫へ登録する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemCode">品種コード</param>
    /// <param name="equipmentId">保管先の自動倉庫</param>
    /// <returns>採番された商品ID</returns>
    Task<string> RegisterItemAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode,
        string equipmentId);


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
    /// <returns>影響した行数</returns>
    Task<int> UpdateItemStatusByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId,
        StockStatus currentStatus,
        StockStatus nextStatus);

    /// <summary>
    /// 商品を出庫する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemId">商品ID</param>
    /// <param name="currentStatus">現在の保管状態</param>
    /// <returns>影響した行数</returns>
    Task<int> PickItemByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId);

}
