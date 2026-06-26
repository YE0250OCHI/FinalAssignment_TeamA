using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using SimpleAutomaticStorageSystem.Server.Dto;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using System.Text.Json;

namespace SimpleAutomaticStorageSystem.Server.Infrastructures;

public class JobDispatcher(
    HttpClient httpClient,
    IOptions<HttpSettings> options,
    ILogger<JobDispatcher> logger,
    JobManager jobManager,
    IOptions<JsonOptions> jsonOptions) : IJobDispatcher
{
    // 定数
    private const string PostUri = "/api/v1/next-picking-order";

    // 自動倉庫の通信設定
    private readonly Dictionary<string, EquipmentSetting> _equipmentsMap =
        options.Value.Equipments.ToDictionary(x => x.EquipmentId);

    // JSON変換設定
    private readonly JsonSerializerOptions _serializeOptions =
        jsonOptions.Value.SerializerOptions;

    /// <inheritdoc/>
    public async Task PushAsync(AssignedJobDto jobDto)
    {
        if (!_equipmentsMap.TryGetValue(jobDto.EquipmentId, out var targetEquipment))
        {
            throw new ArgumentException($"配信JOBデータが不正です Data={jobDto}");
        }

        // 通信先の設定
        string ipAddress = targetEquipment.IpAddress;
        int port = targetEquipment.Port;

        Uri baseUri = new($"http://{ipAddress}:{port}");
        Uri uri = new(baseUri, PostUri);

        try
        {
            // POST送信
            using HttpResponseMessage responseMessage =
                await httpClient.PostAsJsonAsync(
                    uri,
                    new
                    {
                        jobId = jobDto.JobId,
                        jobType = jobDto.JobType,
                        itemId = jobDto.ItemId
                    },
                    _serializeOptions);

            // レスポンス評価
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw (int)responseMessage.StatusCode switch
                {
                    StatusCodes.Status400BadRequest => new InvalidRequestException(),
                    StatusCodes.Status409Conflict => new JobCannotDispatchException(),
                    _ => new HttpRequestException(
                        $"予期しないHTTPステータス StatusCode={(int)responseMessage.StatusCode} {responseMessage.StatusCode}")
                };
            }
            
            logger.LogInformation(
                "Push送信成功 Ip={Ip} Port={Port}",
                ipAddress,
                port);

        }
        catch (Exception ex)
        {
            // 通信失敗

            try
            {
                // 相手装置をオフライン化
                await jobManager.ChangeEquipmentOfflineAsync(
                    jobDto.EquipmentId, "Push送信失敗によるオフライン化");
            }
            catch (Exception offlineEx)
            {
                /* 状態変更失敗はログのみ */

                logger.LogError(
                    offlineEx,
                    "Push失敗後の装置オフライン化に失敗 EquipmentId={EquipmentId}",
                    jobDto.EquipmentId);
            }

            logger.LogWarning(
                ex,
                "Push送信失敗 Ip={Ip} Port={Port}",
                ipAddress,
                port);

            throw;
        }
    }
}
