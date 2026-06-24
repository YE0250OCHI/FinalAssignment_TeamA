using NLog;
using SimpleAutomaticStorageSystem.Stub.Models;
using SimpleAutomaticStorageSystem.Stub.Tools;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SimpleAutomaticStorageSystem.Stub.Tasks;

internal class PickingTask
{
    private readonly Logger sysLogger = LogManager.GetLogger("Stub.System");
    private readonly Logger actLogger = LogManager.GetLogger("Action.Storage");
    private readonly SystemState _state;
    private readonly JobManager _manager;
    private readonly ConsoleInput _key;
    private readonly HttpClient _client;
    private readonly string _deviceUrl;
    private readonly JsonSerializerOptions _options;

    public PickingTask(SystemState state, JobManager manager, ConsoleInput key, HttpClient client, string deviceUrl, JsonSerializerOptions options)
    {
        _state = state;
        _manager = manager;
        _key = key;
        _client = client;
        _deviceUrl = deviceUrl;
        _options = options;
    }

    public async Task ExecuteAsync()
    {
        sysLogger.Info($"出庫処理起動");

        while (true)
        {
            if (_state.State != RackState.Online)
            {
                await Task.Delay(1000);
                continue;
            }
            if (!_manager.TryGetPickingJob(out var job))
            {
                Console.WriteLine("待機中                    ");
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine(new string(' ', Console.WindowWidth));
                }
                Console.SetCursorPosition(0, 1);
                await Task.Delay(100);
                continue;
            }
            if (!_state.TryStartPicking())
            {
                continue;
            }
            try
            {
                if(job == null)
                {
                    sysLogger.Info($"JOB情報不正");
                    _state.State = RackState.Fatal;
                    continue;
                }

                Console.WriteLine($"JOB番号:{job.JobId}");
                Console.WriteLine($"商品ID:{job.ItemId}");

                sysLogger.Info($"作業開始報告:URL={_deviceUrl}/api/v1/racks/job/{job.JobId}/initiate");
                var response = await _client.PostAsync($"{_deviceUrl}/api/v1/racks/job/{job.JobId}/initiate",null);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorBody>(_options);
                    sysLogger.Error($"通信異常:{error?.Error ?? "No Content"}");
                    _state.State = RackState.Fatal;
                    continue;
                }

                
                if(!_key.InputAction("出庫完了確認 (Enter:続行 Esc:非常停止)"))
                {
                    sysLogger.Error($"非常停止");
                    actLogger.Info($"出庫異常終了:JOB番号={job.JobId} 商品ID={job.ItemId}");
                    _state.State = RackState.Emergency;
                    continue;
                }

                sysLogger.Info($"作業完了報告:URL={_deviceUrl}/api/v1/racks/job/{job.JobId}/complete");
                response = await _client.PostAsync($"{_deviceUrl}/api/v1/racks/job/{job.JobId}/complete", null);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorBody>(_options);
                    sysLogger.Error($"通信異常:{error?.Error ?? "No Content"}");
                    actLogger.Info($"出庫異常終了:JOB番号={job.JobId} 商品ID={job.ItemId}");
                    _state.State = RackState.Fatal;
                    continue;
                }

                if (!_key.InputAction("取出完了確認 (Enter:続行 Esc:非常停止)"))
                {
                    sysLogger.Error($"非常停止");
                    actLogger.Info($"取出異常終了:JOB番号={job.JobId} 商品ID={job.ItemId}");
                    _state.State = RackState.Emergency;
                    continue;
                }

                sysLogger.Info($"取出完了報告:URL={_deviceUrl}/api/v1/racks/job/{job.JobId}/remove");
                response = await _client.PostAsync($"{_deviceUrl}/api/v1/racks/job/{job.JobId}/remove", null);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorBody>(_options);
                    sysLogger.Error($"通信異常:{error?.Error ?? "No Content"}");
                    actLogger.Info($"取出異常終了:JOB番号={job.JobId} 商品ID={job.ItemId}");
                    _state.State = RackState.Fatal;
                    continue;
                }

                actLogger.Info($"出庫正常完了:JOB番号={job.JobId} 商品ID={job.ItemId}");

            }
            catch (HttpRequestException ex)
            {
                sysLogger.Warn($"通信エラー：{ex.Message}");
                actLogger.Info($"出庫異常:JOB番号={job.JobId} 商品ID={job.ItemId}");
                _state.State = RackState.Emergency;
            }
            catch (TaskCanceledException)
            {
                sysLogger.Warn($"タイムアウト");
                actLogger.Info($"出庫異常:JOB番号={job.JobId} 商品ID={job.ItemId}");
                _state.State = RackState.Emergency;
            }
            finally
            {
                Console.SetCursorPosition(0, 1);
                _state.EndStoring();
            }

        }
    }
}
