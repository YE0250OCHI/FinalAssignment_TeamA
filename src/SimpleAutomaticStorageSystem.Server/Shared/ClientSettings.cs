namespace SimpleAutomaticStorageSystem.Server.Shared;

/// <summary>
/// クライアントの登録情報
/// </summary>
/// <param name="Devices">スマホ端末</param>
/// <param name="Equipments">自動倉庫端末</param>
public record ClientSettings(
    List<DeviceSetting> Devices,
    List<EquipmentSetting> Equipments);

/// <summary>
/// 登録済みスマホ
/// </summary>
/// <param name="DiveceId">スマホID</param>
/// <param name="IPAddress">IPアドレス</param>
public record DeviceSetting(
    string DiveceId,
    string IPAddress);

/// <summary>
/// 登録済み自動倉庫
/// </summary>
/// <param name="EquipmentId">自動倉庫ID</param>
/// <param name="IPAddress">IPアドレス</param>
public record EquipmentSetting(
    string EquipmentId,
    string IPAddress);
