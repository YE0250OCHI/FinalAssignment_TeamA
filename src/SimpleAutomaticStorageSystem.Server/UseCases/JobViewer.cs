using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Dto;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobViewer(
    IOptions<DatabaseSettings> settings,
    IJobsRepository jobs)
{
    // DB接続文字列
    private readonly string _defaultConnection = settings.Value.DefaultConnection;

    // =========================
    //   公開メソッド
    // =========================

    /// <summary>
    /// 指定されたJOB番号のJobModelを取得する
    /// </summary>
    /// <param name="jobId">JOB番号</param>
    /// <returns>JobModel</returns>
    /// <exception cref="JobNotFoundException">
    /// JOBが存在しない場合にスローされる
    /// </exception>
    public async Task<JobModel> GetJobAsync(string jobId)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // 指定されたjobIdのデータを参照する
        JobModel job =
            await jobs.GetJobByIdAsync(connection, null, jobId) ??
            throw new JobNotFoundException();

        return job;
    }

    /// <summary>
    /// 未完了JOB一覧を取得する
    /// </summary>
    /// <returns>未完了JOBのJobModelリスト</returns>
    public async Task<List<JobModel>> GetIncompleteJobsAsync()
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // JobModelリストを返却
        return [
            .. await jobs.GetIncompleteJobModelsAsync(connection, null)];
    }

    /// <summary>
    /// 指定したスマホIDを依頼元とする、未完了JOB一覧DTOを取得する
    /// </summary>
    /// <param name="deviceId">スマホID</param>
    /// <returns>未完了JOB一覧DTO</returns>
    public async Task<IncompleteJobsResponse>
        GetIncompleteJobsResponseAsync(string deviceId)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // JOBリストを取得
        IEnumerable<IncompleteJobRawInfo> rawInfos = 
            await jobs.GetIncompleteJobRawInfosAsync(connection, null, deviceId);

        // 公開用リストに加工
        List<IncompleteJobInfo> jobInfos = [
            .. rawInfos.Select(x =>
                new IncompleteJobInfo
                {
                    JobId = x.JobId,
                    ItemCode = x.ItemCode,
                    ItemName = x.ItemName,
                    Status = ConvertToRequestJobStatus(x.Status),
                    EquipmentId = x.EquipmentId,
                    CanCancel = (x.Status is JobStatus.Unassigned)
                })];

        // DTOを組立てリターン
        return new IncompleteJobsResponse
        {
            Count = jobInfos.Count,
            Results = jobInfos
        };
    }

    /// <summary>
    /// 指定したスマホIDを依頼元とする、終了済みJOB一覧DTOを取得する
    /// </summary>
    /// <param name="deviceId">スマホID</param>
    /// <returns>終了済みJOB一覧DTO</returns>
    public async Task<HistoryJobsResponse> GetHistoryJobsResponseAsync(
        string deviceId,
        DateTime? from,
        DateTime? to,
        HistorySortOrder sort)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // JOBリストを取得
        IEnumerable<HistoryJobRawInfo> rawInfos =
            await jobs.GetHistoryJobRawInfosAsync(
                connection, null, deviceId, from, to, sort);

        // 公開用リストに加工
        List<HistoryJobInfo> jobInfos = [
            .. rawInfos.Select(x =>
                new HistoryJobInfo
                {
                    JobId = x.JobId,
                    ItemCode = x.ItemCode,
                    ItemName = x.ItemName,
                    Status = ConvertToRequestJobStatus(x.Status),
                    EquipmentId = x.EquipmentId,
                    ClosedAt = x.ClosedAt
                })];

        // DTOを組立てリターン
        return new HistoryJobsResponse
        {
            Count = jobInfos.Count,
            Results = jobInfos
        };
    }

    // =========================
    //   プライベートメソッド
    // =========================

    // JobStatus -> RequestJobStatus への変換
    private static RequestJobStatus ConvertToRequestJobStatus(
        JobStatus jobStatus) =>
            jobStatus switch
            {
                JobStatus.Unassigned or
                JobStatus.Assigned => RequestJobStatus.Waiting,
                JobStatus.Transferring => RequestJobStatus.Working,
                JobStatus.WaitOut => RequestJobStatus.WaitOut,
                JobStatus.Completed => RequestJobStatus.Completed,
                JobStatus.Canceled => RequestJobStatus.Canceled,
                JobStatus.Aborted => RequestJobStatus.Aborted,
                _ => throw new InvalidOperationException(
                    $"公開用JOB状態に変換できません: {jobStatus}")
            };

}

/// <summary>
/// ソート順
/// </summary>
public enum HistorySortOrder
{
    Latest,
    Oldest
}
