using Dapper;
using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Dto;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using System.Text;

namespace SimpleAutomaticStorageSystem.Server.Repositories;

public class ItemsRepository:IItemsRepository
{
    // =========================
    //   参照
    // =========================

    /// <inheritdoc/>
    public Task<ItemModel?> GetItemByIdForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId)
    {
        const string sql = """
            SELECT
                i.id AS [ItemId],
                i.item_code AS [ItemCode],
                i.stock_status AS [Status],
                i.equipment_id AS [EquipmentId],
                i.registered_at AS [RegisteredAt],
                i.picked_at AS [PickedAt]
            FROM
                items i WITH (UPDLOCK, HOLDLOCK)
            WHERE
                i.id = @itemId
                AND i.stock_status <> @OutOfControl
                AND i.picked_at IS NULL
            """;

        return connection.QuerySingleOrDefaultAsync<ItemModel>(
            sql,
            new
            {
                itemId,
                OutOfControl = StockStatus.OutOfControl
            },
            transaction);
    }

    /// <inheritdoc/>
    public Task<ItemModel?> GetAvailableItemForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode) =>
            GetItemByItemCodeAsync(
                connection, transaction, itemCode, null);

    /// <inheritdoc/>
    public Task<ItemModel?> GetPickableItemForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode,
        string equipmentId) =>
            GetItemByItemCodeAsync(
                connection, transaction, itemCode, equipmentId);

    /// <inheritdoc/>
    public Task<int> GetStockCountByEquipmentAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId)
    {
        const string sql = """            
            SELECT
                COUNT(*)
            FROM
                items i
            WHERE
                i.equipment_id = @equipmentId
                AND i.stock_status IN @Statuses
            """;

        return connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                equipmentId,
                Statuses = new[]
                {
                    StockStatus.Stored,
                    StockStatus.Reserved
                }
            },
            transaction: transaction);
    }

    /// <inheritdoc/>
    public async Task<bool> AnyItemTypeAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode)
    {
        const string sql = """            
            SELECT 1
            FROM
                item_types i
            WHERE
                i.code = @itemCode
            """;

        return (await connection.ExecuteScalarAsync<int?>(
            sql,
            new
            {
                itemCode
            },
            transaction: transaction)) is not null;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ItemTypeModel>> GetItemTypesAsync(
        SqlConnection connection,
        SqlTransaction? transaction)
    {
        const string sql = """            
            SELECT
                i.code AS [ItemCode],
                i.name AS [ItemName]
            FROM
                item_types i
            ORDER BY
                i.code ASC
            """;

        return connection.QueryAsync<ItemTypeModel>(
            sql,
            transaction: transaction);
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
        // 連番取得
        string nextItemId =
            await GenerateItemIdForUpdateAsync(
                connection, transaction, itemCode);

        // 商品データ作成
        CreateItemDto newItem = new()
        {
            ItemId = nextItemId,
            ItemCode = itemCode,
            Status = StockStatus.Transferring,
            EquipmentId = equipmentId,
        };

        // クエリ
        const string sql = """
            INSERT INTO items(
                id,
                item_code,
                stock_status,
                equipment_id
            )
            VALUES(
                @ItemId,
                @ItemCode,
                @Status,
                @EquipmentId
            )
            """;

        int affectedRows =
            await connection.ExecuteAsync(
                sql,
                newItem,
                transaction: transaction);

        if (affectedRows != 1) 
        {
            throw new InvalidOperationException(
                $"商品登録に失敗。ItemId={nextItemId} ItemCode={itemCode} EquipmentId={equipmentId}");
        }

        return nextItemId;

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
        const string sql = """
            UPDATE
                items
            SET
                stock_status = @nextStatus
            WHERE
                id = @itemId
                AND stock_status = @currentStatus
            """;

        return connection.ExecuteAsync(
            sql,
            new
            {
                nextStatus,
                itemId,
                currentStatus
            },
            transaction: transaction);
    }

    /// <inheritdoc/>
    public Task<int> PickItemByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemId)
    {
        const string sql = """
            UPDATE
                items
            SET
                stock_status = @Picked,
                picked_at = GETDATE()
            WHERE
                id = @itemId
                AND stock_status = @Transferring
                AND picked_at IS NULL
            """;

        return connection.ExecuteAsync(
            sql,
            new
            {
                Picked = StockStatus.Picked,
                itemId,
                Transferring = StockStatus.Transferring
            },
            transaction: transaction);
    }


    // =========================
    //   プライベートメソッド
    // =========================

    // 品種コードで在庫商品を取得する
    private Task<ItemModel?> GetItemByItemCodeAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode,
        string? equipmentId)
    {
        const string baseSql = """
            
            SELECT TOP(1)
                i.id AS [ItemId],
                i.item_code AS [ItemCode],
                i.stock_status AS [Status],
                i.equipment_id AS [EquipmentId],
                i.registered_at AS [RegisteredAt],
                i.picked_at AS [PickedAt]
            FROM
                items i WITH (UPDLOCK, HOLDLOCK)
            JOIN
                equipments e WITH (UPDLOCK, HOLDLOCK)
                ON e.id = i.equipment_id
            """;

        const string baseWhere = """
            WHERE
                i.item_code = @itemCode
                AND i.picked_at IS NULL
                AND i.stock_status = @Stored
                AND e.equipment_status = @Online
                AND e.picking_job_id IS NULL
            """;

        const string orderBy = """            
            ORDER BY
                i.registered_at ASC,                        
                i.id ASC
            """;

        // Sql組立
        StringBuilder sb = new();
        sb.AppendLine(baseSql);
        sb.AppendLine(baseWhere);

        if (equipmentId is not null)
        {
            sb.AppendLine("AND e.id = @equipmentId");
        }

        sb.AppendLine(orderBy);

        return connection.QuerySingleOrDefaultAsync<ItemModel>(
            sb.ToString(),
            new
            {
                itemCode,
                Stored = StockStatus.Stored,
                Online = EquipmentStatus.Online,
                equipmentId
            },
            transaction);
    }

    // 商品IDを採番する
    private async Task<string> GenerateItemIdForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string itemCode)
    {
        const string sql = """
            SELECT TOP(1)
                i.id
            FROM
                items i WITH (UPDLOCK, HOLDLOCK)
            WHERE
                i.item_code = @itemCode
                AND i.registered_at >= @Today
                AND i.registered_at < @Tomorrow
            ORDER BY
                i.registered_at DESC,
                i.id DESC
            """;

        DateTime today = DateTime.Today;
        string? latestId = await connection.ExecuteScalarAsync<string?>(
            sql,
            new
            {
                itemCode,
                Today = today,
                Tomorrow = today.AddDays(1)
            },
            transaction: transaction);

        if (latestId is null) 
        {
            return $"{itemCode}-{today:yyMMdd}-001";
        }

        string[] parts = latestId.Split('-');

        int sequence = int.Parse(parts[2]) + 1;
        if (sequence > 999)
        {
            throw new InvalidOperationException(
                "商品IDの連番が上限に達しました。");
        }

        // 最新連番+1を返す
        return $"{parts[0]}-{parts[1]}-{sequence:000}";
    }

}
