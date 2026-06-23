namespace SimpleAutomaticStorageSystem.Server.Controllers;

/// <summary>
/// 入庫要求リクエストボディ定義
/// </summary>
public class PutawayRequest
{
    /// <summary>
    /// 品種コード
    /// </summary>
    public required string ItemCode { get; init; }
}