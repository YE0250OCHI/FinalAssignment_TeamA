using Microsoft.Extensions.Options;

namespace SimpleAutomaticStorageSystem.Server.Shared;

/// <summary>
/// 端末認証を行うクラス
/// </summary>
/// <param name="options">端末設定</param>
public class ClientValidator(IOptions<ClientSettings> options)
{
    // スマホの認証設定
    private readonly IReadOnlyDictionary<string, string> _devices =
        options.Value.Devices.ToDictionary(
            x => x.IPAddress,
            x => x.DiveceId);

    // 自動倉庫の認証設定
    private readonly IReadOnlyDictionary<string, string> _equipments =
        options.Value.Equipments.ToDictionary(
            x => x.IPAddress,
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
        _devices.TryGetValue(ipAddress, out device);
    
    /// <summary>
    /// 自動倉庫の認証を行う
    /// </summary>
    /// <param name="ipAddress">IPアドレス</param>
    /// <returns></returns>
    public bool IsValidEquipment(string ipAddress, out string? device) =>
        _equipments.TryGetValue(ipAddress, out device);

}
