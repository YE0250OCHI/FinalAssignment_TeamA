namespace SimpleAutomaticStorageSystem.Server.Domains;

/// <summary>
/// 自動倉庫のモデル
/// </summary>
/// <param name="Id">自動倉庫ID</param>
/// <param name="Status">自動倉庫状態</param>
/// <param name="AvailableCapacity">空き容量</param>
/// <param name="PickingJobId">実行中の出庫JOB、nullは未割当を表す</param>
/// <param name="PutawayJobId">実行中の入庫JOB、nullは未割当を表す</param>
public record EquipmentModel(
    string Id,
    EquipmentStatus Status,
    int AvailableCapacity,
    string? PickingJobId,
    string? PutawayJobId);

/// <summary>
/// 自動倉庫状態
/// </summary>
public enum EquipmentStatus
{
    Offline,
    Online
}