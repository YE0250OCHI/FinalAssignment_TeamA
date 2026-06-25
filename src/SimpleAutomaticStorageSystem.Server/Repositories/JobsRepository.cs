using Dapper;
using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.Repositories;

public class JobsRepository: IJobsRepository
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
                }
            };


    // =========================
    //   参照：エンティティ
    // =========================

    /// <inheritdoc/>
    public Task<JobModel?> GetJobByIdAsync(
        SqlConnection connection, SqlTransaction? transaction, string jobId) =>
        GetJobByIdInternalAsync(connection, transaction, jobId, false);

    /// <inheritdoc/>
    public Task<JobModel?> GetJobByIdForUpdateAsync(
        SqlConnection connection, SqlTransaction? transaction, string jobId) =>
        GetJobByIdInternalAsync(connection, transaction, jobId, true);


    // =========================
    //   プライベートメソッド
    // =========================

    // JOBをID指定で取得する
    private Task<JobModel?> GetJobByIdInternalAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId,
        bool forUpdate)
    {
        string fromClause = forUpdate
            ? "jobs j WITH (UPDLOCK, HOLDLOCK)"
            : "jobs j";

        string sql = $"""
            SELECT
                j.id AS [JobId],
                j.job_type AS [JobType],
                j.job_status AS [JobStatus],
                j.device_id AS [DeviceId],
                j.item_code AS [ItemCode],
                j.item_id AS [ItemId],
                j.equipment_id AS [EquipmentId],
                j.created_at AS [CreatedAt],
                j.assigned_at AS [AssignedAt],
                j.initiated_at AS [InitiatedAt],
                j.completed_at AS [CompletedAt],
                j.removed_at AS [RemovedAt],
                j.closed_at AS [ClosedAt]
            FROM
                {fromClause}
            WHERE
                j.id = @jobId
            """;

        return connection.QueryFirstOrDefaultAsync<JobModel>(
            sql,
            new
            {
                jobId
            },
            transaction);
    }

}
