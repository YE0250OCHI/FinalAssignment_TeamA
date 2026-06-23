namespace SimpleAutomaticStorageSystem.Server.Shared;

/// <summary>
/// クライアントの登録情報
/// </summary>
public class ClientSettings
{
    /// <summary>
    /// スマホ端末設定
    /// </summary>
    public required List<DeviceSetting> Devices { get; init; }

    /// <summary>
    /// 自動倉庫端末設定
    /// </summary>
    public required List<EquipmentSetting> Equipments { get; init; }
}

/// <summary>
/// 登録済みスマホ
/// </summary>
public class DeviceSetting
{
    /// <summary>
    /// スマホID
    /// </summary>
    public required string DiveceId { get; init; }

    /// <summary>
    /// IPアドレス
    /// </summary>
    public required string IPAddress { get; init; }
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
    public required string IPAddress { get; init; }
}