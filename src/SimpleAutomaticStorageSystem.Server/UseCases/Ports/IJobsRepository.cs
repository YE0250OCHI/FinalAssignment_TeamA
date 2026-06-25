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

    /// <summary>
    /// 未割当のJOB一覧を取得する
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <returns>JobModelリスト</returns>
    Task<List<JobModel>> GetUnassignedPickingJobsAsync(
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

    /// <summary>
    /// 最新のJOBを参照し、JOB番号の採番を行う
    /// </summary>
    /// <param name="connection">DB接続</param>
    /// <param name="transaction">トランザクション、nullの場合はトランザクションなし</param>
    /// <returns>新しいJOB番号</returns>
    Task<string> GenerateJobIdAsync(
        SqlConnection connection,
        SqlTransaction? transaction);


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
    Task AssignJobAsync(
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
