using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.Controllers;

/// <summary>
/// 未完了JOB一覧レスポンスボディ定義
/// </summary>
public class IncompleteJobStatesResponse
{
    /// <summary>
    /// JOBの個数
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// JOBリスト
    /// </summary>
    public required List<JobStateResponse> Results { get; init; }
}

/// <summary>
/// 未完了JOB1件を表すデータ
/// </summary>
public class JobStateResponse
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
    /// JOB状態
    /// </summary>
    public required JobStatus Status { get; init; }

    /// <summary>
    /// 自動倉庫ID、nullは未割当を表す
    /// </summary>
    public required string? EquipmentId { get; init; }

    /// <summary>
    /// キャンセル可能か
    /// </summary>
    public required bool CanCancel { get; init; }
}
