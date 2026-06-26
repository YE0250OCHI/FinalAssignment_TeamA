using NLog;
using SimpleAutomaticStorageSystem.Stub.Models;
using SimpleAutomaticStorageSystem.Stub.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private readonly string _serverUrl;
    private readonly JsonSerializerOptions _options;

    private CancellationTokenSource? _requestCts = new();
    private CancellationTokenSource? oldCts = new();
    private readonly object _ctsLock = new();

    private volatile bool _pause;

    public PollingPicking(SystemState state, JobManager manager, HttpClient client, string serverUrl, JsonSerializerOptions options)
    {
        _state = state;
        _manager = manager;
        _client = client;
        _serverUrl = serverUrl;
        _options = options;
    }

    public async Task ExecuteAsync()
    {
        sysLogger.Info($"出庫指示問合せ起動");
        while (true) 
        {
            if (_pause)
            {
                await Task.Delay(100);
                continue;
            }
            if (_state.State != RackState.Online)
            {
                await Task.Delay(100);
                continue;
            }
            if (_state.IsPicking)
            {
                await Task.Delay(100);
                continue;
            }

            CancellationToken token = CancellationToken.None;

            try
            {
                lock (_ctsLock)
                {
                    oldCts =_requestCts;
                    _requestCts = new CancellationTokenSource();
                    token = _requestCts.Token;
                }

                oldCts.Dispose();

                sysLogger.Info($"問合せ送信:URL={_serverUrl}/api/v1/racks/jobs");
                var response = await _client.GetAsync($"{_serverUrl}/api/v1/racks/jobs",token);

                sysLogger.Debug($"問合せ レスポンス受信:{(int)response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorBody>(_options,token);
                    sysLogger.Error($"通信異常:{error?.Error ?? "No Content"}");
                    if(response.StatusCode == HttpStatusCode.UnprocessableContent)
                    {
                        continue;
                    }
                    _state.State = RackState.Fatal;
                    continue;
                }
                else if (response.StatusCode == HttpStatusCode.OK)
                {
                    var job = await response.Content.ReadFromJsonAsync<JobBody>(_options,token);
                    if(job == null)
                    {
                        sysLogger.Warn("レスポンス不正");
                        await Task.Delay(5000);
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
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                sysLogger.Info("問合せ中断");
                continue;
            }
            catch (HttpRequestException ex)
            {
                sysLogger.Warn($"出庫指示問合せ 通信エラー：{ex.Message}");
                _state.State = RackState.Emergency;
            }
            catch (TaskCanceledException)
            {
                sysLogger.Warn($"出庫指示問合せ タイムアウト");
                _state.State = RackState.Emergency;
            }
            catch (Exception ex)
            {
                sysLogger.Warn($"通信形式不正：{ex.Message}");
                _state.State = RackState.Fatal;
            }


            await Task.Delay(5000);
        }
        
    }

    public void Pause()
    {
        _pause = true;
    }

    public void Resume()
    {
        _pause = false;
    }

    public void CancelCurrentRequest()
    {
        lock (_ctsLock)
        {
            _requestCts.Cancel();
        }
    }
}
