using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class InventoryViewer(
    IOptions<DatabaseSettings> settings,
    IItemsRepository items)
{
    // DB接続文字列
    private readonly string _defaultConnection = settings.Value.DefaultConnection;

    /// <summary>
    /// 部品リストを取得
    /// </summary>
    /// <returns>部品リスト</returns>
    public async Task<List<ItemTypeModel>> GetItemListAsync()
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // item_typesテーブルの中身を取得
        return await items.GetItemTypesAsync(connection, null);
    }

}
