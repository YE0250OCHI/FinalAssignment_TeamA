using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.Dto;

public class CreateItemDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public required string ItemId { get; init; }

    /// <summary>
    /// 品種コード
    /// </summary>
    public required string ItemCode { get; init; }

    /// <summary>
    /// 保管状態
    /// </summary>
    public required StockStatus Status { get; init; }

    /// <summary>
    /// 保管元自動倉庫
    /// </summary>
    public required string EquipmentId { get; init; }

}
