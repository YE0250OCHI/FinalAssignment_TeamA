using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Shared.Settings;

namespace SimpleAutomaticStorageSystem.Server.Shared;

/// <summary>
/// 端末認証を行うクラス
/// </summary>
/// <param name="options">端末設定</param>
public class ClientValidator(IOptions<HttpSettings> options)
{
    // スマホの認証設定
    private readonly Dictionary<string, string> _devicesMap =
        options.Value.Devices.ToDictionary(
            x => x.IpAddress,
            x => x.DeviceId);

    // 自動倉庫の認証設定
    private readonly Dictionary<string, string> _equipmentsMap =
        options.Value.Equipments.ToDictionary(
            x => x.IpAddress,
            x => x.EquipmentId);

    // =========================
    //   パブリックメソッド
    // =========================

    /// <summary>
    /// スマホ認証を行う
    /// </summary>
    /// <param name="ipAddress">IPアドレス</param>
    /// <param name="deviceId">スマホID</param>
    /// <returns>検証結果</returns>
    public bool IsValidDevice(string ipAddress, out string? deviceId) =>
        _devicesMap.TryGetValue(ipAddress, out deviceId);

    /// <summary>
    /// 自動倉庫の認証を行う
    /// </summary>
    /// <param name="ipAddress">IPアドレス</param>
    /// <param name="equipmentId">自動倉庫ID</param>
    /// <returns>検証結果</returns>
    public bool IsValidEquipment(string ipAddress, out string? equipmentId) =>
        _equipmentsMap.TryGetValue(ipAddress, out equipmentId);

}