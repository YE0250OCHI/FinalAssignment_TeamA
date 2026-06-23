using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.Backgrounds;

/// <summary>
/// タイムアウト設定データ
/// </summary>
public class TimeoutSettings
{
    /// <summary>
    /// 監視周期
    /// </summary>
    public required int MonitorIntervalSeconds { get; init; } 

    /// <summary>
    /// 設定時間リスト
    /// </summary>
    public required List<JobStatusTimeoutSetting> Timeouts { get; init; }
}

/// <summary>
/// 状態別タイムアウト秒数
/// </summary>
public class JobStatusTimeoutSetting
{
    /// <summary>
    /// JOB状態
    /// </summary>
    public required JobStatus Status { get; init; }

    /// <summary>
    /// タイムアウト設定(秒)
    /// </summary>
    public required int TimeoutSeconds { get; init; }
}