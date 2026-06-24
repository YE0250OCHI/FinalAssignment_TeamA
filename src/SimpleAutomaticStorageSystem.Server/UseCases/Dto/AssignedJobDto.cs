using SimpleAutomaticStorageSystem.Server.Domains;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Dto;

public class AssignedJobDto
{
    /// <summary>
    /// JOB番号
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// JOB種別
    /// </summary>
    public required JobType JobType { get; init; }

    /// <summary>
    /// 割り当てられた商品ID
    /// </summary>
    public required string ItemId { get; init; }
}
