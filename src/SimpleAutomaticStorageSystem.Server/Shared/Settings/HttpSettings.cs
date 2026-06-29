namespace SimpleAutomaticStorageSystem.Server.Shared.Settings;

/// <summary>
/// Http通信の設定
/// </summary>
public class HttpSettings
{
    /// <summary>
    /// プッシュ送信の応答タイムアウト時間（秒）
    /// </summary>
    public required int PushTimeoutSeconds { get; init; }

    /// <summary>
    /// スマホ端末設定
    /// </summary>
    public required IReadOnlyList<DeviceSetting> Devices { get; init; }

    /// <summary>
    /// 自動倉庫端末設定
    /// </summary>
    public required IReadOnlyList<EquipmentSetting> Equipments { get; init; }
}

/// <summary>
/// 登録済みスマホ
/// </summary>
public class DeviceSetting
{
    /// <summary>
    /// スマホID
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// IPアドレス
    /// </summary>
    public required string IpAddress { get; init; }
}

/// <summary>
/// 登録済み自動倉庫
/// </summary>
public class EquipmentSetting
{
    /// <summary>
    /// 自動倉庫ID
    /// </summary>
    public required string EquipmentId { get; init; }

    /// <summary>
    /// IPアドレス
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// ポート番号（Push時に使用）
    /// </summary>
    public required int Port { get; init; }
}