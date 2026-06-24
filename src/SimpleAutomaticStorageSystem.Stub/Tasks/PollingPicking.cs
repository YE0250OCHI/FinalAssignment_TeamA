using NLog;
using SimpleAutomaticStorageSystem.Stub.Models;
using SimpleAutomaticStorageSystem.Stub.Tools;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SimpleAutomaticStorageSystem.Stub.Tasks;

internal class PollingPicking
{
    private readonly Logger sysLogger = LogManager.GetLogger("Stub.System");
    private readonly SystemState _state;
    private readonly JobManager _manager;
    private readonly HttpClient _client;
    private readonly string _deviceUrl;
    private readonly JsonSerializerOptions _options;

    public PollingPicking(SystemState state, JobManager manager, HttpClient client, string deviceUrl, JsonSerializerOptions options)
    {
        _state = state;
        _manager = manager;
        _client = client;
        _deviceUrl = deviceUrl;
        _options = options;
    }

    public async Task ExecuteAsync()
    {
        sysLogger.Info($"出庫指示問合せ起動");
        while (true) 
        {
            if(_state.State != RackState.Online)
            {
                await Task.Delay(1000);
                continue;
            }
            if (_state.IsPicking)
            {
                await Task.Delay(1000);
                continue;
            }

            try
            {
                sysLogger.Info($"問合せ送信:URL={_deviceUrl}/api/v1/racks/job");
                var response = await _client.PostAsync($"{_deviceUrl}/api/v1/racks/job", null);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorBody>(_options);
                    sysLogger.Error($"通信異常:{error?.Error ?? "No Content"}");
                    _state.State = RackState.Fatal;
                    continue;
                }
                else if (response.StatusCode == HttpStatusCode.OK)
                {
                    var job = await response.Content.ReadFromJsonAsync<JobBody>(_options);
                    if(job == null)
                    {
                        sysLogger.Warn("レスポンス不正");
                        continue;
                    }

                    sysLogger.Info($"出庫指示あり:JobID={job.JobId}");

                    if (!_manager.TryAddJob(job))
                    {
                        sysLogger.Warn($"JOB番号重複:JobID={job.JobId}");
                    }
                }
                else
                {
                    sysLogger.Info($"出庫指示なし");
                }
            }
            catch (HttpRequestException ex)
            {
                sysLogger.Warn($"通信エラー：{ex.Message}");
                _state.State = RackState.Emergency;
            }
            catch (TaskCanceledException)
            {
                sysLogger.Warn($"タイムアウト");
                _state.State = RackState.Emergency;
            }

            await Task.Delay(5000);
        }
        
    }
}
