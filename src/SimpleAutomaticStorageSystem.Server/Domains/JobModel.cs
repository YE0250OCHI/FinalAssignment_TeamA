namespace SimpleAutomaticStorageSystem.Server.Domains;

/// <summary>
/// JOBのモデル
/// </summary>
/// <param name="JobId">JOB番号</param>
/// <param name="JobType">JOB種別</param>
/// <param name="JobStatus">JOB状態</param>
/// <param name="DeviceId">スマホID（出庫依頼元）、入庫はnull</param>
/// <param name="ItemCode">品種コード</param>
/// <param name="ItemId">商品ID</param>
/// <param name="EquipmentId">自動倉庫ID</param>
/// <param name="CreatedAt">JOB作成日時</param>
/// <param name="AssignedAt">割当日時</param>
/// <param name="InitiatedAt">搬送開始日時</param>
/// <param name="CompletedAt">搬送完了日時</param>
/// <param name="RemovedAt">取出し日時</param>
/// <param name="ClosedAt">JOB終了日時</param>
public record JobModel(
    string JobId,
    JobType JobType,
    JobStatus JobStatus,
    string? DeviceId,
    string ItemCode,
    string? ItemId,
    string? EquipmentId,
    DateTime CreatedAt,
    DateTime? AssignedAt,
    DateTime? InitiatedAt,
    DateTime? CompletedAt,
    DateTime? RemovedAt,
    DateTime? ClosedAt);

/// <summary>
/// JOB種別
/// </summary>
public enum JobType
{
    Piking,
    Putaway
}

/// <summary>
/// JOB状態の定義
/// </summary>
public enum JobStatus
{
    Unassigned,
    Assigned,
    Transferring,
    WaitOut,
    Completed,
    Canceled,
    Aborted
}