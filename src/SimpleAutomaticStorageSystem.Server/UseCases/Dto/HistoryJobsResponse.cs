namespace SimpleAutomaticStorageSystem.Server.UseCases.Dto;

/// <summary>
/// 終了済みJOB一覧レスポンス
/// </summary>
public class HistoryJobsResponse
{
    /// <summary>
    /// 終了済みJOBの個数
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// 終了済みJOB一覧
    /// </summary>
    public required List<HistoryJobInfo> Results { get; init; }

}

/// <summary>
/// 終了済みJOBの情報
/// </summary>
public class HistoryJobInfo
{
    /// <summary>
    /// JOB番号
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// 品種コード
    /// </summary>
    public required string ItemCode { get; init; }

    /// <summary>
    /// 部品名
    /// </summary>
    public required string ItemName { get; init; }

    /// <summary>
    /// スマホ公開用JOB状態
    /// </summary>
    public required RequestJobStatus Status { get; init; }

    /// <summary>
    /// 自動倉庫ID
    /// </summary>
    public required string? EquipmentId { get; init; }

    /// <summary>
    /// 終了日時
    /// </summary>
    public required DateTime ClosedAt { get; init; }

}