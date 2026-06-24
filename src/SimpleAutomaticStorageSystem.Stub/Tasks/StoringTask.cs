using NLog;
using SimpleAutomaticStorageSystem.Stub.Models;
using SimpleAutomaticStorageSystem.Stub.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SimpleAutomaticStorageSystem.Stub.Tasks;

internal class StoringTask
{
    private readonly Logger sysLogger = LogManager.GetLogger("Stub.System");
    private readonly Logger actLogger = LogManager.GetLogger("Action.Storage");
    private readonly SystemState _state;
    private readonly JobManager _manager;
    private readonly ConsoleInput _key;
    private readonly HttpClient _client;
    private readonly string _deviceUrl;
    private readonly JsonSerializerOptions _options;

    public StoringTask(SystemState state, JobManager manager, ConsoleInput key, HttpClient client, string deviceUrl, JsonSerializerOptions options)
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
        sysLogger.Info($"入庫処理起動");
    }
}
