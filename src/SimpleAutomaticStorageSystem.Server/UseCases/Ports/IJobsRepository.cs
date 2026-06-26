using Microsoft.Data.SqlClient;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Dto;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Ports;

/// <summary>
/// jobsテーブル操作
/// </summary>
public interface IJobsRepository
{
    // =========================
    //   参照
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
    /// JOBをID指定で、ロック付き取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="jobId">JOB番号</param>
    /// <returns>JobModel</returns>
    Task<JobModel?> GetJobByIdForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId);

    /// <summary>
    /// 未完了JOB一覧を取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <returns>JobModelリスト</returns>
    Task<IEnumerable<JobModel>> GetIncompleteJobModelsAsync(
        SqlConnection connection,
        SqlTransaction? transaction);

    /// <summary>
    /// 未割当状態のJOB一覧を取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <returns>JobModelリスト</returns>
    Task<IEnumerable<JobModel>> GetUnassignedPickingJobsForUpdateAsync(
        SqlConnection connection,
        SqlTransaction? transaction);

    /// <summary>
    /// 指定したスマホIDを依頼元とする、未完了JOB情報のリストを取得
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="deviceId">スマホID</param>
    /// <returns></returns>
    Task<IEnumerable<IncompleteJobRawInfo>> GetIncompleteJobRawInfosAsync(
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
    Task<IEnumerable<HistoryJobRawInfo>> GetHistoryJobRawInfosAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string deviceId,
        DateTime? from,
        DateTime? to,
        HistorySortOrder sort);


    // =========================
    //   作成
    // =========================

    /// <summary>
    /// JOBを新規作成する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="newJob">作成するJOB</param>
    /// <returns>影響した行数</returns>
    Task<int> CreateJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        CreateJobDto newJob);


    // =========================
    //   更新
    // =========================

    /// <summary>
    /// JOBに商品、自動倉庫を割り当て
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="jobId">JOB番号</param>
    /// <param name="itemId">商品ID</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <returns>影響した行数</returns>
    Task<int> AssignJobAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId,
        string itemId,
        string equipmentId);

    /// <summary>
    /// JOBの状態遷移
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="jobId">JOB番号</param>
    /// <param name="jobType">JOB種別</param>
    /// <param name="currentStatus">現在のJOB状態</param>
    /// <param name="nextStatus">次のJOB状態</param>
    /// <returns>影響した行数</returns>
    Task<int> UpdateJobStatusByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId,
        JobType jobType,
        JobStatus currentStatus,
        JobStatus nextStatus);

    /// <summary>
    /// JOBの状態遷移
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <param name="jobId">JOB番号</param>
    /// <param name="jobType">JOB種別</param>
    /// <param name="currentStatus">現在のJOB状態</param>
    /// <param name="nextStatus">次のJOB状態</param>
    /// <returns>影響した行数</returns>
    Task<int> CloseJobByIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string jobId,
        JobType jobType,
        JobStatus currentStatus,
        JobStatus nextStatus);


    // =========================
    //   ユーティリティ
    // =========================

    /// <summary>
    /// 最新のJOBを参照し、JOB番号の採番を行う
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <returns>新しいJOB番号</returns>
    Task<string> GenerateJobIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction);

}
