using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;



public class IndexModel : PageModel
{
    public class IncompleteTask
    {
        public string jobId;
        public string itemCode;
        public string itemName;
        public string status;
        public string? equipmentId;
        public bool canCancel;
    }
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    //出力　：エラーメッセージ表示部
    [BindProperty]
    public string? ErrorMessage { get; set; }
    //入力　：部品選択のプルダウン
    [BindProperty]
    public string selectedItem { get; set; } = string.Empty;

    //生成　：未完了タスクリスト
    public List<IncompleteTask> IncompleteTaskList { get; set; } = new();

    //フラグ　：エラーポップアップ
    public bool ShowCancelErrorPopup { get; set; } = false;

    //メソッド　：起動時の未完了タスクリスト取得
    //public async Task<IActionResult> OnGetAsync()
    //{
    //    return await GetIncompleteTask();
    //}

    // メソッド　：データ更新用のGET








    //以下を修正
    public async Task<IActionResult> OnGetAsync([FromQuery] string? status, [FromQuery] int? limit)
    {
        await LoadPickingOrdersFromApiAsync(status, limit);
        return Page();
    }
    private async Task<bool> LoadPickingOrdersFromApiAsync(string? status, int? limit)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();

            // 1. クエリパラメータを組み立てる
            // 例: https://your-api-server/api/v1/picking-orders?status=Active&limit=10
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (!string.IsNullOrEmpty(status))
            {
                query["status"] = status;
            }
            if (limit.HasValue)
            {
                query["limit"] = limit.Value.ToString();
            }

            // ベースURLにクエリ文字列を結合する
            var baseUri = "https://your-api-server/api/v1/picking-orders";
            var requestUri = query.Count > 0 ? $"{baseUri}?{query}" : baseUri;

            // 2. 組み立てたURLでGETリクエストする
            var response = await client.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var apiData = JsonSerializer.Deserialize<List<PickingOrderResponse>>(jsonString, options);

                if (apiData != null)
                {
                    IncompleteTaskList = apiData.Select(x => new IncompleteTask
                    {
                        jobId = x.JobId,
                        status = ConvertStatus(x.Status),
                        itemName = x.ItemName,
                        canCancel = x.CanCancel
                    }).ToList();
                }
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }













    // メソッド　：追加ボタン押下時処理
    public async Task<IActionResult> OnPostAddOrderAsync()
    {
        if (string.IsNullOrEmpty(selectedItem))
        {
            ErrorMessage = "入力形式が不正です。通常の入力方法で入力してください。";
            await GetIncompleteTask();
            return Page();
        }

        try
        {
            var client = _httpClientFactory.CreateClient();

            //プルダウンの選択内容をPOST
            var response = await client.PostAsJsonAsync(/*"接続先指定は後で"*/"", new { itemCode = selectedItem });

            // 正常終了(ステータスコード:201)時の処理
            if (response.StatusCode == System.Net.HttpStatusCode.Created) // 201
            {
                return await GetIncompleteTask();
            }

            // 異常終了時の処理
            {
                ErrorMessage = await HandleApiErrorAsync(response);
                await GetIncompleteTask();
                return Page();
            }
        }
        //サーバからのレスポンスがない場合の処理
        catch
        {
            ErrorMessage = "通信エラーが発生しました。しばらく経ってから再度お試しください。";
            await GetIncompleteTask();
            return Page();
        }
    }
    //メソッド　：キャンセルボタン押下時処理
    public async Task<IActionResult> OnPostCancelAsync(string jobId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();

            // キャンセルPOST
            var response = await client.PostAsync(/*"接続先指定は後で"*/"", null);

            // 正常終了(ステータスコード:201)時の処理
            if (response.StatusCode == System.Net.HttpStatusCode.Created) // 201
            {
                return await GetIncompleteTask();
            }
            // 異常終了時の処理
            else
            {
                ErrorMessage = await HandleApiErrorAsync(response);
                //キャンセル失敗のポップアップ
                ShowCancelErrorPopup = true;
                await GetIncompleteTask();
                return Page();
            }
        }
        //サーバからのレスポンスがない場合の処理
        catch
        {
            ShowCancelErrorPopup = true;
            await GetIncompleteTask();
            return Page();
        }
    }

    //メソッド　：未完了タスクGETの結果判別
    private async Task<IActionResult> GetIncompleteTask()
    {
        bool success = await LoadPickingOrdersFromApiAsync();
        //正常終了しなかった場合の処理
        if (!success)
        {
            return RedirectToPage("/Error");
        }
        //正常終了時の処理
        return Page();
    }

    // メソッド　：未完了タスクリストのGET
    private async Task<bool> LoadPickingOrdersFromApiAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            // ii) サーバにGETリクエストする
            var response = await client.GetAsync("https://your-api-server/api/v1/picking-orders");

            // 正常終了時の処理
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var apiData = JsonSerializer.Deserialize<List<PickingOrderResponse>>(jsonString, options);

                //サーバからのjsonがnullではないときの処理
                if (apiData != null)
                {
                    //List<IncompleteTask>を生成
                    IncompleteTaskList = apiData.Select(x => new IncompleteTask
                    {
                        jobId = x.JobId,
                        status = ConvertStatus(x.Status), // status対応表に基づいて変換
                        itemName = x.ItemName,
                        canCancel = x.CanCancel
                    }).ToList();
                }
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // メソッド　：ステータスを日本語に変換
    private string ConvertStatus(string apiStatus)
    {
        return apiStatus switch
        {
            "Waiting" => "取出待ち",
            "Processing" => "作業中",
            "Recovering" => "調整中",
            "Pending" => "待機中",
            _ => apiStatus // 想定外はそのまま
        };
    }

    // メソッド　：エラーコードを日本語に変換
    private async Task<string> HandleApiErrorAsync(HttpResponseMessage response)
    {
        int statusCode = (int)response.StatusCode;
        string errorCode = "";

        try
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            if (doc.RootElement.TryGetProperty("code", out var codeProp))
            {
                errorCode = codeProp.GetString() ?? "";
            }
        }
        catch { /* JSON解析失敗時はコード空のまま */ }

        // 設計書のエラーメッセージ表を完全再現
        return (statusCode, errorCode) switch
        {
            (400, "INVALID_REQUEST") => "入力形式が不正です。\n通常の入力方法で入力してください。",
            (400, "INVALID_QUERY") => "入力形式が不正です。\n通常の入力方法で入力してください。",
            (403, "UNREGISTERED_DEVICE") => "端末が登録されていません。\nサーバの登録端末を確認してください。",
            (403, "ACCESS_DENIED_JOB") => "JOB削除は登録した端末からしか行えません。",
            (404, "JOB_NOT_FOUND") => "指定したJOBが存在しません。\n画面を更新してください。",
            (422, "OUT_OF_STOCK") => "在庫が不足しています。\n入庫後、再度出庫リクエストしてください。",
            (422, "INVALID_PRODUCT_ID") => "入力形式が不正です。\n通常の入力方法で入力してください。",
            _ => $"予期せぬエラーが発生しました。(Status: {statusCode})"
        };
    }
}

public class PickingOrderResponse
{
    [JsonPropertyName("jobId")] public string JobId { get; set; } = string.Empty;
    [JsonPropertyName("itemCode")] public string ItemCode { get; set; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("canCancel")] public bool CanCancel { get; set; }
}