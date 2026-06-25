using SimpleAutomaticStorageSystem.Server.Domains;
using System.Text.Json.Serialization;

namespace SimpleAutomaticStorageSystem.Server.Dto;

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

/// <summary>
/// 未完了JOBの情報（生データ）
/// </summary>
public class IncompleteJobRawInfo
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
    /// 内部JOB状態
    /// </summary>
    public required JobStatus Status { get; init; }

    /// <summary>
    /// 自動倉庫ID
    /// </summary>
    public required string? EquipmentId { get; init; }

}

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
/// 終了済みJOBの情報（JSON用）
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

/// <summary>
/// 終了済みJOBの情報（生データ）
/// </summary>
public class HistoryJobRawInfo
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
    public required JobStatus Status { get; init; }

    /// <summary>
    /// 自動倉庫ID
    /// </summary>
    public required string? EquipmentId { get; init; }

    /// <summary>
    /// 終了日時
    /// </summary>
    public required DateTime ClosedAt { get; init; }

}

/// <summary>
/// スマホ公開用のJOB状態定義
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestJobStatus
{
    Waiting,
    Working,
    WaitOut,
    Completed,
    Canceled,
    Aborted
}