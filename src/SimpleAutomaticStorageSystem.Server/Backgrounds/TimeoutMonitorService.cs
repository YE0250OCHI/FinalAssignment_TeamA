using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Controllers;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.UseCases;

namespace SimpleAutomaticStorageSystem.Server.Backgrounds;

public class TimeoutMonitorService(
    ILogger<RacksApiController> logger,
    IOptions<TimeoutSettings> options,
    JobViewer jobViewer,
    JobManager jobManager)
{
    // 監視周期の設定
    private readonly TimeSpan _monitorInterval =
        TimeSpan.FromSeconds(options.Value.MonitorIntervalSeconds);

    // タイムアウト設定辞書
    private readonly IReadOnlyDictionary<JobStatus, TimeSpan> _timeoutsMap =
        options.Value.Timeouts.ToDictionary(
            x => x.Status,
            x => TimeSpan.FromSeconds(x.TimeoutSeconds));

    // キャンセルオブジェクト
    private readonly CancellationTokenSource _cts = new();

    // =========================
    //   パブリックメソッド
    // =========================

    //
    public async Task StartMonitoringAsync()
    {
        try
        {
            // キャンセルされない間継続
            while (!_cts.IsCancellationRequested)
            {
                // JOB一覧を取得
                List<JobModel> incompleteJobs = [];

                /* jobViewerで未完了JOBを取得 */

                // JOBの時間経過をチェック

                foreach (var job in incompleteJobs)
                {
                    // 未達（セーフ）ならスキップ
                    if (!IsTimeout(job))
                    {
                        continue;
                    }

                    // ロギング
                    logger.LogWarning(
                        "JOBタイムアウト JobId={JobId} Status={Status}",
                        job.JobId,
                        job.JobStatus);

                    // タイムアウトしたので異常終了させる

                        /* jobManagerで異常終了処理 */


                }

                // 指定時間待機
                await Task.Delay(
                    _monitorInterval,
                    _cts.Token);

            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "プログラム実装エラー");
            throw;
        }
    }

    /// <summary>
    /// タイムアウト検知
    /// </summary>
    /// <param name="job">JOB</param>
    /// <returns>タイムアウトならtrue、問題なしならfalse</returns>
    /// <exception cref="InvalidOperationException">設定ミス、DB設定不正でスローされる</exception>
    private bool IsTimeout(JobModel job)
    {
        JobStatus status = job.JobStatus;


        // 比較対象の決定
        DateTime? baseTime = status switch
        {
            JobStatus.Unassigned => job.CreatedAt,
            JobStatus.Assigned => job.AssignedAt,
            JobStatus.Transferring => job.InitiatedAt,
            JobStatus.WaitOut => job.CompletedAt,
            _ => throw new InvalidOperationException(
                $"未定義の状態が指定された Status={status}")
        }
        ?? throw new InvalidOperationException(
            $"状態にタイムスタンプが設定されていない Status={status}");

        // 制限時間
        TimeSpan timeoutSpan = _timeoutsMap[status];

        // 判定
        return DateTime.Now > baseTime + timeoutSpan;
    }

}