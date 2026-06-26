using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using NLog;
using SimpleAutomaticStorageSystem.Stub.Models;
using SimpleAutomaticStorageSystem.Stub.Tools;

namespace SimpleAutomaticStorageSystem.Stub.Tasks;

internal class WaitPickingReq
{
    private readonly Logger sysLogger = LogManager.GetLogger("Stub.System");
    private readonly SystemState _state;
    private readonly JobManager _manager;
    private readonly HttpListener _listener;
    private readonly JsonSerializerOptions _options;
    private readonly PollingPicking _polling;

    public WaitPickingReq(SystemState state, JobManager manager, HttpListener listener, JsonSerializerOptions options, PollingPicking polling)
    {
        _state = state;
        _manager = manager;
        _listener = listener;
        _options = options;
        _polling = polling;
    }

    public async Task ExecuteAsync()
    {
        sysLogger.Info($"出庫指示待機起動");
        while (true)
        {
            if(_state.State != RackState.Online)
            {
                await Task.Delay(1000);
                continue;
            }

            sysLogger.Info($"リスナー起動");

            HttpListenerContext context = await _listener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string? path = request.Url?.AbsolutePath;
            string? method = request.HttpMethod;
            string json;

            sysLogger.Info($"リクエスト受信:Path={path} Method={method}");

            if(method != "POST")
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                sysLogger.Warn($"レスポンス送信:Code={response.StatusCode}");
                response.Close();
                continue;
            }

            if(_state.IsPicking)
            {
                var error = new ErrorBody("CANNOT_DISPATCH");
                json = JsonSerializer.Serialize(error,_options);

                response.StatusCode = (int)HttpStatusCode.Conflict;
                sysLogger.Warn($"レスポンス送信:Code={response.StatusCode}");
                response.ContentType = "application/json";

                byte[] buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer);

                response.Close();
                continue;
            }

            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            json = await reader.ReadToEndAsync();

            JobBody? job;
            try
            {
                job = JsonSerializer.Deserialize<JobBody>(json,_options);
                sysLogger.Debug($"リクエスト受信:{job}");
            }
            catch(JsonException ex)
            {
                sysLogger.Debug($"JSON変換失敗:{ex.Message}");
                job = null;
            }

            if(job == null|| string.IsNullOrWhiteSpace(job.JobId))
            {
                var error = new ErrorBody("INVALID_REQUEST");
                json = JsonSerializer.Serialize(error, _options);

                response.StatusCode = (int)HttpStatusCode.BadRequest;
                sysLogger.Warn($"レスポンス送信:Code={response.StatusCode}");
                response.ContentType = "application/json";

                byte[] buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer);

                response.Close();
                continue;
            }

            sysLogger.Info($"出庫指示あり:JobID={job.JobId}");

            if(!_manager.TryAddJob(job))
            {
                sysLogger.Warn($"JOB番号重複:JobID={job.JobId}");
                var error = new ErrorBody("CANNOT_DISPATCH");
                json = JsonSerializer.Serialize(error, _options);

                response.StatusCode = (int)HttpStatusCode.Conflict;
                sysLogger.Warn($"レスポンス送信:Code={response.StatusCode}");
                response.ContentType = "application/json";

                byte[] buffer = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer);

                response.Close();
                continue;
            }

            _polling.Pause();
            _polling.CancelCurrentRequest();

            response.StatusCode = (int)HttpStatusCode.NoContent;
            sysLogger.Info($"レスポンス送信:Code={response.StatusCode}");
            response.Close();

        }



    }
}
