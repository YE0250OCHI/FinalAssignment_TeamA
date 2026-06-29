namespace SimpleAutomaticStorageSystem.Server.UseCases.Response;

public class AvailableItemsResponse
{
    // 品種コード
    public required string KeyCode { get; set; }

    // 表示名
    public required string Text { get; set; }
}


