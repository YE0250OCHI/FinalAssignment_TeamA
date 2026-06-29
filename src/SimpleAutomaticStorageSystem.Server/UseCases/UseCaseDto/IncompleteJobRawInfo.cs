using SimpleAutomaticStorageSystem.Server.Domains;
using System.Text.Json.Serialization;

namespace SimpleAutomaticStorageSystem.Server.UseCases.UseCaseDto;



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
