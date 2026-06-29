using Dapper;
using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using SimpleAutomaticStorageSystem.Server.UseCases.UseCaseDto;
using System.Text;

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
                    // 次の状態 = 更新するカラム名
                    [JobStatus.Assigned] = "assigned_at", // 未割当→割当済み
                    [JobStatus.Transferring] = "initiated_at", // 割当済み→搬送中
                    [JobStatus.WaitOut] = "completed_at", // 搬送中→取出待ち
                    [JobStatus.Completed] = "removed_at", // 取出待ち→完了
                },
                [JobType.Putaway] = new Dictionary<JobStatus, string> // 入庫JOBの場合
                {
                    // 次の状態 = 更新するカラム名
                    [JobStatus.Assigned] = "assigned_at", // 未割当→割当済み
                    [JobStatus.Transferring] = "initiated_at", // 割当済み→搬送中
                    [JobStatus.Completed] = "completed_at", // 搬送中→完了
                }
            };


    // =========================
    //   参照
    // =========================

    /// <inheritdoc/>
    public Task<JobModel?> GetJobByIdAsync(
        SqlConnection connection, SqlTransaction? transaction, string jobId) =>
        GetJobByIdInternalAsync(connection, transaction, jobId, false);

    /// <inheritdoc/>
    public Task<JobModel?> GetJobByIdForUpdateAsync(
        SqlConnection connection, SqlTransaction? transaction, string jobId) =>
        GetJobByIdInternalAsync(connection, transaction, jobId, true);

    /// <inheritdoc/>
    public Task<IEnumerable<JobModel>> GetIncompleteJobModelsAsync(
        SqlConnection connection,
        SqlTransaction? transaction)
    {
        const string sql = """
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
                jobs j
            WHERE
                j.closed_at IS NULL
                AND j.job_type = @Picking
                AND j.job_status IN @JobStatuses
            ORDER BY
                j.created_at ASC
            """;

        return connection.QueryAsync<JobModel>(
            sql,
            new
            {
                Picking = JobType.Picking,
                JobStatuses = new[]
                {
                    JobStatus.Unassigned,
                    JobStatus.Assigned,
                    JobStatus.Transferring,
                    JobStatus.WaitOut
                }
            },
            transaction: transaction);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<JobModel>> GetUnassignedPickingJobsForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction)
    {
        const string sql = """
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
                jobs j WITH (UPDLOCK, HOLDLOCK)
            WHERE
                j.closed_at IS NULL
                AND j.job_type = @Picking
                AND j.job_status = @Unassigned
            ORDER BY
                j.created_at ASC
            """;

        return connection.QueryAsync<JobModel>(
            sql,
            new
            {
                Picking = JobType.Picking,
                Unassigned = JobStatus.Unassigned
            },
            transaction: transaction);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<IncompleteJobRawInfo>> GetIncompleteJobRawInfosAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string deviceId)
    {
        const string sql = """
            SELECT
                j.id AS [JobId],
                j.item_code AS [ItemCode],
                i.name AS [ItemName],
                j.job_status AS [Status],
                j.equipment_id AS [EquipmentId]
            FROM
                jobs j
            JOIN 
                item_types i ON i.code = j.item_code
            WHERE
                j.device_id = @deviceId
                AND j.closed_at IS NULL
                AND j.job_type = @Picking
                AND j.job_status IN @JobStatuses
            ORDER BY
                j.created_at ASC
            """;

        return connection.QueryAsync<IncompleteJobRawInfo>(
            sql,
            new
            {
                deviceId,
                Picking = JobType.Picking,
                JobStatuses = new[]
                {
                    JobStatus.Unassigned,
                    JobStatus.Assigned,
                    JobStatus.Transferring,
                    JobStatus.WaitOut
                }
            },
            transaction: transaction);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<HistoryJobRawInfo>> GetHistoryJobRawInfosAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string deviceId,
        DateTime? from,
        DateTime? to,
        HistorySortOrder sort)
    {
        const string baseSql = """
            SELECT
                j.id AS [JobId],
                j.item_code AS [ItemCode],
                i.name AS [ItemName],
                j.job_status AS [Status],
                j.equipment_id AS [EquipmentId],
                j.closed_at AS [ClosedAt]
            FROM
                jobs j
            JOIN 
                item_types i ON i.code = j.item_code
            WHERE
                j.device_id = @deviceId
                AND j.job_type = @Picking
                AND j.job_status IN @JobStatuses
                AND j.closed_at IS NOT NULL
            """;

        string orderSql = sort switch
        {
            HistorySortOrder.Latest => "ORDER BY j.closed_at DESC",
            HistorySortOrder.Oldest => "ORDER BY j.closed_at ASC",
            _ => throw new InvalidOperationException(
                    $"並び順指定が不正です: {sort}")
        };

        // SQL組立
        StringBuilder sb = new();
        sb.AppendLine(baseSql);

        if(from is not null)
        {
            sb.AppendLine("AND j.closed_at >= @From");
        }

        if (to is not null)
        {
            sb.AppendLine("AND j.closed_at < @To");
        }

        sb.AppendLine(orderSql);

        return connection.QueryAsync<HistoryJobRawInfo>(
            sb.ToString(),
            new
            {
                deviceId,
                Picking = JobType.Picking,
                JobStatuses = new[]
                {
                    JobStatus.Completed,
                    JobStatus.Canceled,
                    JobStatus.Aborted
                },
                From = from?.Date,
                To = to?.Date.AddDays(1)
            },
            transaction: transaction);

    }


    // =========================
    //   作成
    // =========================

    /// <inheritdoc/>
    public Task<int> CreateJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        CreateJobDto newJob)
    {
        const string sql = """
            INSERT INTO jobs(
                id,
                job_type,
                job_status,
                device_id,
                item_code,
                item_id,
                equipment_id
            )
            VALUES(
                @JobId,
                @JobType,
                @JobStatus,
                @DeviceId,
                @ItemCode,
                @ItemId,
                @EquipmentId
            )
            """;

        return connection.ExecuteAsync(
            sql,
            new
            {
                JobId = newJob.JobId,
                JobType = newJob.JobType,
                JobStatus = newJob.JobStatus,
                DeviceId = newJob.DeviceId,
                ItemCode = newJob.ItemCode,
                ItemId = newJob.ItemId,
                EquipmentId = newJob.EquipmentId
            },
            transaction: transaction);

    }


    // =========================
    //   更新
    // =========================

    /// <inheritdoc/>
    public Task<int> AssignJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId,
        string itemId,
        string equipmentId)
    {
        const string sql = """
            UPDATE jobs
            SET
                job_status = @Assigned,
                item_id = @itemId,
                equipment_id = @equipmentId,
                assigned_at = GETDATE()
            WHERE
                id = @jobId
                AND job_status = @Unassigned
                AND item_id IS NULL
                AND equipment_id IS NULL
            """;

        return connection.ExecuteAsync(
            sql,
            new
            {
                Assigned = JobStatus.Assigned,
                itemId,
                equipmentId,
                jobId,
                Unassigned = JobStatus.Unassigned
            },
            transaction: transaction);
    }

    /// <inheritdoc/>
    public Task<int> UpdateJobStatusByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId,
        JobType jobType,
        JobStatus currentStatus,
        JobStatus nextStatus) =>
            UpdateJobStatusInternalAsync(
                connection,
                transaction,
                jobId,
                jobType,
                currentStatus,
                nextStatus,
                isClosed: false);


    /// <inheritdoc/>
    public Task<int> CloseJobByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId,
        JobType jobType,
        JobStatus currentStatus,
        JobStatus nextStatus) => 
            UpdateJobStatusInternalAsync(
                connection,
                transaction,
                jobId,
                jobType,
                currentStatus,
                nextStatus,
                isClosed: true);


    // =========================
    //   ユーティリティ
    // =========================

    /// <inheritdoc/>
    public async Task<string> GenerateJobIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction)
    {
        DateTime today = DateTime.Today;

        const string sql = """
            SELECT TOP(1)
                j.id
            FROM
                jobs j WITH (UPDLOCK, HOLDLOCK)
            WHERE
                j.created_at >= @Today
                AND j.created_at < @Tomorrow
            ORDER BY
                j.created_at DESC,
                j.id DESC
            """;

        string? latestJobId =
            await connection.ExecuteScalarAsync<string?>(
                sql,
                new
                {
                    Today = today,
                    Tomorrow = today.AddDays(1)
                },
                transaction: transaction);

        if(latestJobId is null)
        {
            // 存在しないときは、本日1件目として扱う
            return $"J{today:yyyyMMdd}-01";
        }

        string[] parts = latestJobId.Split('-');

        int sequence = int.Parse(parts[1]) + 1;
        if (sequence > 99)
        {
            throw new InvalidOperationException(
                "JOB番号の連番が上限に達しました。");
        }

        // 最新連番+1を返す
        return $"{parts[0]}-{sequence:00}";
    }



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
        string fromTable = forUpdate
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
                {fromTable}
            WHERE
                j.id = @jobId
            """;

        return connection.QuerySingleOrDefaultAsync<JobModel>(
            sql,
            new { jobId },
            transaction);
    }

    // JOB状態の更新
    private Task<int> UpdateJobStatusInternalAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId,
        JobType jobType,
        JobStatus currentStatus,
        JobStatus nextStatus,
        bool isClosed)
    {
        // 次JOB状態の正当性チェック（終了系のみ）
        if (isClosed)
        {
            if (nextStatus is not
                (JobStatus.Completed or JobStatus.Canceled or JobStatus.Aborted))
            {
                throw new InvalidOperationException(
                    $"終了状態ではありません。NextStatus={nextStatus}");
            }
        }

        const string baseSql = """
            UPDATE jobs
            SET
                job_status = @nextStatus
            """;

        const string whereSql = """
            WHERE
                id = @jobId
                AND job_status = @currentStatus
            """;

        // SQL組立
        StringBuilder sb = new();
        sb.AppendLine(baseSql);

        // 更新するカラム

        if(nextStatus is not (JobStatus.Canceled or JobStatus.Aborted))
        {
            string targetCol = _timestampColumnMap[jobType][nextStatus];
            sb.AppendLine($", {targetCol} = GETDATE()");
        }

        // 遷移後が終了系ステータスの場合、closed_at も更新する
        if (isClosed)
        {
            sb.AppendLine($", closed_at = GETDATE()");
        }

        sb.AppendLine(whereSql);


        if (nextStatus is not (JobStatus.Canceled or JobStatus.Aborted))
        {
            string targetCol = _timestampColumnMap[jobType][nextStatus];
            sb.AppendLine($"AND {targetCol} IS NULL");
        }

        // 完了済みJOBの再更新を防止する
        if (isClosed)
        {
            sb.AppendLine($"AND closed_at IS NULL");
        }

        return connection.ExecuteAsync(
            sb.ToString(),
            new
            {
                nextStatus,
                jobId,
                currentStatus
            },
            transaction: transaction);

    }
}
