namespace SimpleAutomaticStorageSystem.Server.UseCases.UseCaseDto;

public class InventoryItemsRawInfo
{
    // 品種コード
    public required string ItemCode { get; set; }

    // 品種名
    public required string ItemName { get; set; }

    // 出庫可能な個数
    public required int AvailableCount { get; set; }
}
