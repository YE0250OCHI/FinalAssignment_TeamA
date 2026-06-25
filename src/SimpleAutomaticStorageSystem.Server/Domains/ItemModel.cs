namespace SimpleAutomaticStorageSystem.Server.Domains;

/// <summary>
/// 在庫商品のモデル
/// </summary>
/// <param name="ItemId">商品ID</param>
/// <param name="ItemCode">品種コード</param>
/// <param name="EquipmentId">保管ID</param>
/// <param name="Status">在庫状態</param>
/// <param name="RegisterdAt">登録日時（保管日時）</param>
/// <param name="PickedAt">出庫日時</param>
public record ItemModel(
    string ItemId,
    string ItemCode,
    string EquipmentId,
    StockStatus Status,
    DateTime RegisterdAt,
    DateTime? PickedAt);

/// <summary>
/// 商品の品種
/// </summary>
/// <param name="ItemCode">品種コード</param>
/// <param name="ItemName">品種名</param>
public record ItemTypeModel(
    string ItemCode,
    string ItemName);

/// <summary>
/// 在庫状態
/// </summary>
public enum StockStatus
{
    None, // 管理対象外になった
    Stored, // 保管中
    Reserved, // 割当が実行された
    Transferring // 搬送中
}