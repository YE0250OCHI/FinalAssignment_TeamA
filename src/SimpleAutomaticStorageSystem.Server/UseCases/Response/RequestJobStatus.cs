using System.Text.Json.Serialization;

namespace SimpleAutomaticStorageSystem.Server.UseCases.Response;

/// <summary>
/// スマホ公開用のJOB状態定義
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestJobStatus
{
    Waiting,
    Transferring,
    WaitOut,
    Completed,
    Canceled,
    Aborted
}