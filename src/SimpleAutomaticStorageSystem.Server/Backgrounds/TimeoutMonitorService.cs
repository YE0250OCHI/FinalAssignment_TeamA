using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Controllers;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared.Settings;
using SimpleAutomaticStorageSystem.Server.UseCases;

namespace SimpleAutomaticStorageSystem.Server.Backgrounds;

public class TimeoutMonitorService(
    ILogger<TimeoutMonitorService> logger,
    IOptions<TimeoutSettings> options,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    // 監視周期の設定
    private readonly TimeSpan _monitorInterval =
        TimeSpan.FromSeconds(options.Value.MonitorIntervalSeconds);

    // タイムアウト設定辞書
    private readonly Dictionary<JobStatus, TimeSpan> _timeoutsMap =
        options.Value.Timeouts.ToDictionary(
            x => x.Status,
            x => TimeSpan.FromSeconds(x.TimeoutSeconds));

    /// <summary>
    /// JOBのタイムアウト監視を実行する
    /// </summary>
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // サーバー起動ログ
        logger.LogInformation(
            "JOBタイムアウト監視サービス起動");

        try
        {
            // キャンセルされない間継続
            while (!stoppingToken.IsCancellationRequested)
            {
                // スコープの作成
                using IServiceScope scope = scopeFactory.CreateScope();

                // サービスの作成
                JobViewer jobViewer =
                    scope.ServiceProvider.GetRequiredService<JobViewer>();
                JobManager jobManager =
                    scope.ServiceProvider.GetRequiredService<JobManager>();

                // JOB一覧を取得
                List<JobModel> incompleteJobs =
                    await jobViewer.GetIncompleteJobsAsync();

                // JOBの時間経過をチェック

                foreach (JobModel job in incompleteJobs)
                {
                    try
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

                        // タイムアウトしたJOBの自動倉庫IDを取得
                        string? equipmentId = job.EquipmentId;

                        // タイムアウト処理
                        if (equipmentId is not null)
                        {
                            // 装置をオフラインにする
                            await jobManager.ChangeEquipmentOfflineAsync(
                                equipmentId, "タイムアウト");
                        }
                        else
                        {
                            // 未割当JOBなのでそのまま異常終了
                            await jobManager.AbortUnassignedJobAsync(
                                job.JobId,
                                "タイムアウト");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "タイムアウト処理失敗 JobId={JobId}",
                            job.JobId);
                    }
                }

                // 指定時間待機
                await Task.Delay(
                    _monitorInterval,
                    stoppingToken);

            }

        }
        catch (OperationCanceledException)
        {
            // 正常な停止処理
            logger.LogInformation(
                "JOBタイムアウト監視サービス終了");

        }
        catch (Exception ex)
        {
            // 異常発生
            logger.LogCritical(
                ex,
                "JOBタイムアウト監視サービス異常終了");

            throw; // 例外スロー

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
        // Jobがすでに完了済みなら終了
        if(job.ClosedAt is not null)
        {
            return false;
        }

        JobStatus currentStatus = job.JobStatus;

        // 比較対象の決定
        DateTime? baseTime = currentStatus switch
        {
            JobStatus.Unassigned => job.CreatedAt,
            JobStatus.Assigned => job.AssignedAt,
            JobStatus.Transferring => job.InitiatedAt,
            JobStatus.WaitOut => job.CompletedAt,
            _ => throw new InvalidOperationException(
                $"未定義の状態が指定された Status={currentStatus}")
        }
        ?? throw new InvalidOperationException(
            $"状態にタイムスタンプが設定されていない Status={currentStatus}");

        // 制限時間
        TimeSpan timeoutSpan = _timeoutsMap[currentStatus];

        // 判定
        return DateTime.Now > baseTime + timeoutSpan;
    }

}