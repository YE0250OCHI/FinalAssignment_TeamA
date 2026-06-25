using SimpleAutomaticStorageSystem.Server.Dto;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Ports;

public interface IJobDispatcher
{
    /// <summary>
    /// JOBをプッシュ配信する
    /// </summary>
    /// <param name="jobDto">配信用JOBデータ</param>
    Task PushAsync(AssignedJobDto jobDto);
}
