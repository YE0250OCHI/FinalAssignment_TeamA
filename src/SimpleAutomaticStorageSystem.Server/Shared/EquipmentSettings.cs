using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.Shared;

/// <summary>
/// 自動倉庫設定
/// </summary>
public class EquipmentSettings
{
    /// <summary>
    /// 自動倉庫の設定リスト
    /// </summary>
    public required IReadOnlyList<EquipmentOption> Equipments { get; init; }

}

/// <summary>
/// 自動倉庫単体の設定
/// </summary>
public class EquipmentOption
{
    /// <summary>
    /// JOB状態
    /// </summary>
    public required string EquipmentId { get; init; }

    /// <summary>
    /// 容量
    /// </summary>
    public required int Capacity { get; init; }

}