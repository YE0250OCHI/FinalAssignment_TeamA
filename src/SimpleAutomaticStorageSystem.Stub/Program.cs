using NLog;
using SimpleAutomaticStorageSystem.Stub.Models;
using SimpleAutomaticStorageSystem.Stub.Tasks;
using SimpleAutomaticStorageSystem.Stub.Tools;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using static System.Net.WebRequestMethods;

// ロガー生成
var sysLogger = LogManager.GetLogger("Stub.System");
var actLogger = LogManager.GetLogger("Action.Storage");

sysLogger.Info("アプリケーション起動");
actLogger.Info("アプリケーション起動");

// Ctrl+C無効化
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
};

// 設定ファイル読出し
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// HTTPクライアント生成
HttpClient client = new()
{
    Timeout = TimeSpan.FromSeconds(10),
};
string serverUrl = config["ServerSettings:ServerUrl"];
sysLogger.Info($"HTTPクライアント起動:URL={serverUrl}");

// HTTPリスナー生成
HttpListener listener = new();
string listenerPrefixes = config["ListenerSettings:Prefixes"];
listener.Prefixes.Add(listenerPrefixes);

try
{
    listener.Start();
    sysLogger.Info($"HTTPリスナー起動:{listenerPrefixes}");
}
catch(Exception ex)
{
    listener.Close();
    sysLogger.Error($"リスナー起動失敗:{ex}");
    listener = new();
    listener.Prefixes.Add("http://localhost:8080/api/");
    try
    {
        listener.Start();
        sysLogger.Info("HTTPリスナー起動:localhost");
    }
    catch (Exception)
    {
        listener.Close();
        sysLogger.Error($"リスナー起動失敗:{ex}");
        return;
    }
}

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};

// インスタンス生成
var state = new SystemState();
var manager = new JobManager();
var key = new ConsoleInput();

var waitPickingReq = new WaitPickingReq(state,manager,listener, options);
var pollingPicking = new PollingPicking(state,manager,client,serverUrl,options);
var waitStoringReq = new WaitStoringReq(state,manager,client,serverUrl,options);
var pickingTask = new PickingTask(state,manager,key,client,serverUrl,options);
var storingTask = new StoringTask(state,manager,key,client,serverUrl,options);

// 常駐タスク起動
_ = Task.Run(waitPickingReq.ExecuteAsync);
_ = Task.Run(pollingPicking.ExecuteAsync);
//_ = Task.Run(waitStoringReq.ExecuteAsync);
_ = Task.Run(pickingTask.ExecuteAsync);
//_ = Task.Run(storingTask.ExecuteAsync);

Console.CursorVisible = false;

while (true)
{
    Console.Clear();
    Console.WriteLine("オンライン通知：サーバーからのレスポンスを待っています……");

    // オンライン通知
    try
    {
        sysLogger.Info($"オンライン通知:IP={serverUrl}/api/v1/racks/online");
        var response = await client.PostAsync($"{serverUrl}/api/v1/racks/online",null);

        if(response.IsSuccessStatusCode)
        {
            sysLogger.Info($"オンライン受理：Status={(int)response.StatusCode}");
            state.State = RackState.Online;

            
        }
        else
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
            sysLogger.Error($"{error?.Error ?? "No Content"}：Status={(int)response.StatusCode}");
            state.State = RackState.Fatal;
        }

    }
    catch (HttpRequestException ex)
    {
        sysLogger.Warn($"オンライン通知 通信エラー：{ex.Message}");
        state.State = RackState.Emergency;
    }
    catch (TaskCanceledException)
    {
        sysLogger.Warn($"オンライン通知 タイムアウト");
        state.State = RackState.Emergency;
    }

    Console.Clear();
    Console.WriteLine("============ 出庫 ============");
    while (state.State != RackState.Offline)
    {   
        // 異常確認
        if (state.State != RackState.Online)
        {
            Console.Clear();
            Console.WriteLine("装置異常停止中");
            try
            {
                sysLogger.Info($"アラーム報告:IP={serverUrl}/api/v1/racks/alarms");

                var alarm = new AlarmBody("EMERGENCY_OFF", DateTime.Now);
                await client.PostAsJsonAsync($"{serverUrl}/api/v1/racks/online", alarm,options);

            }
            catch (HttpRequestException ex)
            {
                sysLogger.Warn($"オンライン通知 通信エラー：{ex.Message}");
            }
            catch (TaskCanceledException)
            {
                sysLogger.Warn($"オンライン通知 タイムアウト");
            }

            if (state.State == RackState.Emergency)
            {
                if (key.InputAction("復帰：Enter 終了：Esc"))
                {
                    sysLogger.Info($"装置復帰");
                    state.State = RackState.Offline;
                }
                else
                {
                    state.State = RackState.Fatal;
                }
            }
            if (state.State == RackState.Fatal)
            {
                break;
            }
        }
    }
    if (state.State == RackState.Fatal)
    {
        sysLogger.Info("アプリケーション終了");
        listener.Close();
        return;
    }
        
}


