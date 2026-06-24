using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.Repositories;

public class JobsRepository
{
    // タイムスタンプカラム対応マップ
    private readonly IReadOnlyDictionary<
        JobType,
        IReadOnlyDictionary<JobStatus, string>>
        _timestampColumnMap =
            new Dictionary<JobType, IReadOnlyDictionary<JobStatus, string>>
            {
                [JobType.Picking] = new Dictionary<JobStatus, string> // 出庫JOBの場合
                {
                    [JobStatus.Assigned] = "assigned_at", // 未割当→割当済み
                    [JobStatus.Transferring] = "initiated_at", // 割当済み→搬送中
                    [JobStatus.WaitOut] = "completed_at", // 搬送中→取出待ち
                    [JobStatus.Completed] = "removed_at", // 取出待ち→完了
                },
                [JobType.Putaway] = new Dictionary<JobStatus, string> // 入庫JOBの場合
                {
                    [JobStatus.Assigned] = "assigned_at", // 未割当→割当済み
                    [JobStatus.Transferring] = "initiated_at", // 割当済み→搬送中
                    [JobStatus.Completed] = "completed_at", // 搬送中→完了
                },
            };




}
