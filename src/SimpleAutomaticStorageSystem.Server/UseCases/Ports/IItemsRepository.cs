using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases.Dto;

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
    /// <returns>ItemModel?</returns>
    Task<ItemModel?> GetItemByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId);

    /// <summary>
    /// 品種コードを指定し、割り当て可能な在庫を取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemCode">品種コード</param>
    /// <returns>割当可能な商品、存在しない場合はnull</returns>
    Task<ItemModel?> GetAvailableItemAsync(
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
    Task<ItemModel?> GetPickableItemAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode,
        string equipmentId);


    // =========================
    //   参照：マスター
    // =========================

    /// <summary>
    /// ItemCodeから、ItemTypeマスターを取り出す
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="itemCode">品種コード</param>
    /// <returns>品種データ</returns>
    Task<ItemTypeModel?> GetItemTypeAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode);


    // =========================
    //   作成
    // =========================

    /// <summary>
    /// 商品在庫を登録する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="dto">在庫登録DTO</param>
    /// <returns></returns>
    Task RegisterItemAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        RegisterItemDto dto);


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
    Task UpdateItemStatusByIdAsync(
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
    Task PickItemByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId);

}
