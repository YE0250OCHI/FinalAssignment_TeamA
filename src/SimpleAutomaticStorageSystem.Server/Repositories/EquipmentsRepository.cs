using Dapper;
using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.Repositories;

public class EquipmentsRepository:IEquipmentsRepository
{
    // =========================
    //   参照
    // =========================

    /// <inheritdoc/>
    public Task<EquipmentModel?> GetEquipmentByIdForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId)
    {
        const string sql = """
            SELECT
                e.id AS [Id],
                e.equipment_status AS [Status],
                e.available_capacity AS [AvailableCapacity],
                e.picking_job_id AS [PickingJobId],
                e.putaway_job_id AS [PutawayJobId]
            FROM
                equipments e WITH (UPDLOCK, HOLDLOCK)
            WHERE
                e.id = @equipmentId            
            """;

        return connection.QuerySingleOrDefaultAsync<EquipmentModel>(
            sql,
            new { equipmentId },
            transaction: transaction);
    }


    // =========================
    //   更新
    // =========================

    /// <inheritdoc/>
    public Task<int> AssignPickingJobAsync(
        SqlConnection connection, SqlTransaction? transaction, string equipmentId, string pickingJobId) =>
        AssignJobAsync(connection, transaction, equipmentId, pickingJobId, JobType.Picking);

    /// <inheritdoc/>
    public Task<int> AssignPutawayJobAsync(
        SqlConnection connection, SqlTransaction? transaction, string equipmentId, string putawayJobId) =>
        AssignJobAsync(connection, transaction, equipmentId, putawayJobId, JobType.Putaway);


    /// <inheritdoc/>
    public Task<int> UpdateEquipmentByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId,
        int availableCapacity,
        EquipmentStatus currentStatus,
        EquipmentStatus nextStatus)
    {
        const string sql = """
            UPDATE
                equipments
            SET
                equipment_status = @nextStatus,
                available_capacity = @availableCapacity,
                picking_job_id = NULL,
                putaway_job_id = NULL
            WHERE
                id = @equipmentId
                AND equipment_status = @currentStatus
            """;

        return connection.ExecuteAsync(
            sql,
            new
            {
                nextStatus,
                availableCapacity,
                equipmentId,
                currentStatus
            },
            transaction: transaction);

    }


    // =========================
    //   プライベートメソッド
    // =========================

    // 自動倉庫へJOB番号を割り当てる
    private static Task<int> AssignJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string equipmentId,
        string jobId,
        JobType jobType)
    {
        string targetCol = jobType switch
        {
            JobType.Picking => "picking_job_id",
            JobType.Putaway => "putaway_job_id",
            _ => throw new InvalidOperationException(
                $"JOB種別が不正 JobType:{jobType}")
        };

        string sql = $"""
            UPDATE
                equipments
            SET
                {targetCol} = @jobId
            WHERE
                id = @equipmentId
                AND equipment_status = @Online
                AND {targetCol} IS NULL
            """;

        return connection.ExecuteAsync(
            sql,
            new
            {
                jobId,
                equipmentId,
                Online = EquipmentStatus.Online
            },
            transaction: transaction);
    }
}
