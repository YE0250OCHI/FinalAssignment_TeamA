using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases.Dto;

namespace SimpleAutomaticStorageSystem.Server.UseCases;

public class JobViewer(
    IConfiguration config,
    IJobsRepository jobs)
{
    // DB接続文字列
    private readonly string _defaultConnection =
        config.GetConnectionString("DefaultConnection") ?? string.Empty;

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

        // 指定されたjobIdのデータを取る
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
        return await jobs.GetIncompleteJobsAsync(
            connection,
            null);
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
        List<IncompleteJobInfo> jobInfos =
            await jobs.GetIncompleteJobInfosAsync(connection, null, deviceId);

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
    public async Task<HistoryJobsResponse>
        GetHistoryJobsResponseAsync(string deviceId)
    {
        // DB接続開始
        await using SqlConnection connection = new(_defaultConnection);
        await connection.OpenAsync();

        // JOBリストを取得
        List<HistoryJobInfo> jobInfos =
            await jobs.GetHistoryJobInfosAsync(connection, null, deviceId);

        // DTOを組立てリターン
        return new HistoryJobsResponse
        {
            Count = jobInfos.Count,
            Results = jobInfos
        };
    }

}
