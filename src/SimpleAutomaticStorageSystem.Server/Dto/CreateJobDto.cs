using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.Dto;

/// <summary>
/// JOBの新規作成用DTO
/// </summary>
public class CreateJobDto
{
    /// <summary>
    /// JOB番号
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// JOB種別
    /// </summary>
    public required JobType JobType { get; init; }

    /// <summary>
    /// JOB状態
    /// </summary>
    public required JobStatus JobStatus { get; init; }

    /// <summary>
    /// スマホID（出庫のみ）
    /// </summary>
    public required string? DeviceId { get; init; }

    /// <summary>
    /// 品種コード
    /// </summary>
    public required string ItemCode { get; init; }

    /// <summary>
    /// 割り当てられた商品ID
    /// </summary>
    public required string? ItemId { get; init; }

    /// <summary>
    /// 割り当てられた自動倉庫ID
    /// </summary>
    public required string? EquipmentId { get; init; }
}
