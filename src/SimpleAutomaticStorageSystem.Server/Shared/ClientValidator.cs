using Microsoft.Extensions.Options;

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
    /// <returns></returns>
    public bool IsValidDevice(string ipAddress, out string? device) =>
        _devicesMap.TryGetValue(ipAddress, out device);
    
    /// <summary>
    /// 自動倉庫の認証を行う
    /// </summary>
    /// <param name="ipAddress">IPアドレス</param>
    /// <returns></returns>
    public bool IsValidEquipment(string ipAddress, out string? device) =>
        _equipmentsMap.TryGetValue(ipAddress, out device);

}