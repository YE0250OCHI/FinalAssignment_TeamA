using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.UseCases.UseCaseDto;

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