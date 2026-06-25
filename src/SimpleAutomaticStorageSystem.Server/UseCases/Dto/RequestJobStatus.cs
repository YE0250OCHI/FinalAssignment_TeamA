using System.Text.Json.Serialization;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Dto;

/// <summary>
/// スマホ公開用のJOB状態定義
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestJobStatus
{
    Waiting,
    Working,
    WaitOut,
    Completed,
    Canceled,
    Aborted
}
