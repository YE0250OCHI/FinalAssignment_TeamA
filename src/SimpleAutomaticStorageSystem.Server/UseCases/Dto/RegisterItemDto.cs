using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Dto;

/// <summary>
/// 在庫登録用DTO
/// </summary>
public class RegisterItemDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    public required string ItemId { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public required string ItemCode { get; init; }
    public required StockStatus Status { get; init; }
    public required string EquipmentId { get; init; }
}
