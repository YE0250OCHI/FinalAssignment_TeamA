using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases.Dto;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Ports;

/// <summary>
/// jobsテーブル操作
/// </summary>
public interface IJobsRepository
{
    // =========================
    //   参照：エンティティ
    // =========================

    /// <summary>
    /// JOBをID指定で取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="jobId">JOB番号</param>
    /// <returns>JobModel</returns>
    Task<JobModel?> GetJobByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId);

    /// <summary>
    /// 未完了JOB一覧を取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <returns>JobModelリスト</returns>
    Task<List<JobModel>> GetIncompleteJobsAsync(
        SqlConnection connection,
        SqlTransaction? transaction);


    // =========================
    //   参照：公開用DTO
    // =========================

    /// <summary>
    /// 指定したスマホIDを依頼元とする、未完了JOB情報のリストを取得
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="deviceId">スマホID</param>
    /// <returns></returns>
    Task<List<IncompleteJobInfo>> GetIncompleteJobInfosAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string deviceId);

    /// <summary>
    /// 指定したスマホIDを依頼元とする、終了済みJOB情報のリストを取得
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="deviceId">スマホID</param>
    /// <returns></returns>
    Task<List<HistoryJobInfo>> GetHistoryJobInfosAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string deviceId);


    // =========================
    //   作成
    // =========================

    /// <summary>
    /// JOBを新規作成する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="newJob">作成するJOB</param>
    /// <returns></returns>
    Task CreateJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        CreateJobDto newJob);


    // =========================
    //   更新
    // =========================

    /// <summary>
    /// JOBの状態遷移
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="jobId">JOB番号</param>
    /// <param name="currentStatus">現在のJOB状態</param>
    /// <param name="nextStatus">次のJOB状態</param>
    /// <param name="isClosed">最終状態か</param>
    /// <returns></returns>
    Task UpdateJobStatusByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId,
        JobStatus currentStatus,
        JobStatus nextStatus);

}
