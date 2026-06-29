namespace SimpleAutomaticStorageSystem.Server.UseCases.Response;

/// <summary>
/// 未完了JOB一覧レスポンス
/// </summary>
public class IncompleteJobsResponse
{
    /// <summary>
    /// 未完了JOBの個数
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// 未完了JOB一覧
    /// </summary>
    public required List<IncompleteJobInfo> Results { get; init; }

}

/// <summary>
/// 未完了JOBの情報（JSON用）
/// </summary>
public class IncompleteJobInfo
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
    /// キャンセル可能か
    /// </summary>
    public required bool CanCancel { get; init; }

}