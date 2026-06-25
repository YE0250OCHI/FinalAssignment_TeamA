using Dapper;
using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.Repositories;

public class ItemsRepository:IItemsRepository
{
    // =========================
    //   参照
    // =========================

    /// <inheritdoc/>
    public async Task<ItemModel?> GetItemByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId)
    {
        string fromSql = transaction is null
            ? "FROM items i"
            : "FROM items i WITH (UPDLOCK, HOLDLOCK)";

        string sql = $"""
            SELECT
                i.id AS [ItemId],
                i.item_code AS [ItemCode],
                i.stock_status_id AS [Status],
                i.equipment_id AS [EquipmentId],
                i.registered_at AS [RegisteredAt],
                i.picked_at AS [PickedAt]
            {fromSql}
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
        const string sql = """
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




    }

    /// <inheritdoc/>
    public async Task<ItemModel?> GetPickableItemAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode,
        string equipmentId)
    {

    }

    /// <inheritdoc/>
    public Task<int> GetStockCountByEquipmentAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId)
    {

    }

    /// <inheritdoc/>
    public async Task<ItemTypeModel?> GetItemTypeAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode)
    {

    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ItemTypeModel>> GetItemTypesAsync(
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
    public Task<int> UpdateItemStatusByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId,
        StockStatus currentStatus,
        StockStatus nextStatus)
    {

    }

    /// <inheritdoc/>
    public async Task<int> PickItemByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId)
    {

    }


}
