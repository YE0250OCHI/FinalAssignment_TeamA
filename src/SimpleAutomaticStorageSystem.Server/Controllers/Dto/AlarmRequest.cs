namespace SimpleAutomaticStorageSystem.Server.Controllers.Dto;

/// <summary>
/// アラーム報告リクエストボディ定義
/// </summary>
public class AlarmRequest
{
    /// <summary>
    /// アラームコード
    /// </summary>
    public required string AlarmCode { get; init; }

    /// <summary>
    /// 発生日時
    /// </summary>
    public required DateTime OccurredAt { get; init; }
}