using Dapper;
using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.Repositories;

public class ItemsRepository:IItemsRepository
{
    // =========================
    //   参照：エンティティ
    // =========================

    /// <inheritdoc/>
    public async Task<ItemModel?> GetItemByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId)
    {
        // 全在庫から、指定した商品IDの在庫情報を取得するクエリ
        string sql = """
            SELECT
                i.id AS [ItemId],
                i.item_code AS [ItemCode],
                i.stock_status_id AS [Status],
                i.equipment_id AS [EquipmentId],
                i.registered_at AS [RegisteredAt],
                i.picked_at AS [PickedAt]
            FROM
                items i WITH (UPDLOCK, HOLDLOCK)
            WHERE
                i.id = @itemId
            """;

        return await connection.QueryFirstOrDefaultAsync<ItemModel>(
            sql,
            new { itemId },
            transaction);
    }




    /// <inheritdoc/>
    public async Task<ItemModel?> GetAvailableItemAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode)
    {
        // 出庫作業待機中の自動倉庫から、指定品種の在庫情報を取得するクエリ
    }

    /// <inheritdoc/>
    public async Task<ItemModel?> GetPickableItemAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode,
        string equipmentId)
    {
        // 指定した自動倉庫がのもつ、指定品種の在庫情報を取得するクエリ
    }


    // =========================
    //   参照：マスター
    // =========================

    /// <inheritdoc/>
    public async Task<ItemTypeModel?> GetItemTypeAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode)
    {

    }

    /// <inheritdoc/>
    public async Task<List<ItemTypeModel>> GetItemTypesAsync(
        SqlConnection connection,
        SqlTransaction? transaction)
    {

    }


    // =========================
    //   作成
    // =========================

    /// <inheritdoc/>
    public async Task<string> RegisterItemAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode,
        string equipmentId)
    {

    }


    // =========================
    //   更新
    // =========================

    /// <inheritdoc/>
    public async Task UpdateItemStatusByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId,
        StockStatus currentStatus,
        StockStatus nextStatus)
    {

    }

    /// <inheritdoc/>
    public async Task PickItemByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId)
    {

    }


}
