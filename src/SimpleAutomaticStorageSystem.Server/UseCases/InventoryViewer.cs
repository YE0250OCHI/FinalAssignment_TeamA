using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Shared.Settings;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using SimpleAutomaticStorageSystem.Server.UseCases.Response;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class InventoryViewer(
    IOptions<DatabaseSettings> settings,
    IItemsRepository items)
{
    // DB接続文字列
    private readonly string _defaultConnection = settings.Value.DefaultConnection;

    /// <summary>
    /// 商品リストを取得
    /// </summary>
    /// <returns>商品リスト</returns>
    public async Task<List<AvailableItemsResponse>> GetItemListAsync()
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // item_typesテーブルの中身を取得
        var itemlist = await items.GetPickableItemListAsync(connection, null);

        // 公開用に加工して返却
        return [
            .. itemlist.Select(x=>
                new AvailableItemsResponse
                {
                    KeyCode = x.ItemCode,
                    Text = $"{x.ItemCode} : {x.ItemName}"
                })];
    }
}
