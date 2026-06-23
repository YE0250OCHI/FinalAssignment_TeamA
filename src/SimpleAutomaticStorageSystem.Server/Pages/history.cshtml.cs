using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Json.Serialization;

public class HistoryModel : PageModel
{
    public class FinishedTask
    {
        public string jobId;
        public string status;
        public string name;
        public DateTime finishedAt;
    }

    private readonly IHttpClientFactory _httpClientFactory;

    public HistoryModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    //入力 ：左側のカレンダー（検索開始日）
    [BindProperty(SupportsGet = true)]
    public DateTime taskEndDateFrom { get; set; } = DateTime.Today;

    //入力 ：右側のカレンダー（検索終了日）
    [BindProperty(SupportsGet = true)]
    public DateTime? taskEndDateTo { get; set; }

    //入力 ：並び替え条件プルダウン
    [BindProperty(SupportsGet = true)]
    public string sort { get; set; } = "latest";

    //生成 ：完了タスクリスト
    public List<FinishedTask> FinishedTaskList { get; set; } = new();


    //メソッド ：起動時、更新ボタン押下時の履歴タスクリスト取得
    //public async Task<IActionResult> OnGetAsync()
    //{
    //    return await GetFinishedTask();
    //}

    //メソッド ：完了タスクGETの結果判別
    private async Task<IActionResult> GetFinishedTask()
    {
        bool success = await LoadFinishedOrdersFromApiAsync();
        //正常終了しなかった場合の処理
        if (!success)
        {
            return RedirectToPage("/Error");
        }
        //正常終了時の処理
        return Page();
    }

    // メソッド ：完了タスクリストのGET
    private async Task<bool> LoadFinishedOrdersFromApiAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();

            //クエリパラメータを組み立ててURLを生成
            var fromStr = taskEndDateFrom.ToString("yyyy-MM-dd");
            var toStr = taskEndDateTo?.ToString("yyyy-MM-dd") ?? "";
            var url = $"https://your-api-server/api/v1/picking-orders/history?from={fromStr}&to={toStr}&sort={sort}";

            //サーバにGETリクエストする
            var response = await client.GetAsync(url);

            // 正常終了時の処理
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var apiData = JsonSerializer.Deserialize<List<FinishedTaskResponse>>(jsonString, options);

                //サーバからのjsonがnullではないときの処理
                if (apiData != null)
                {
                    //List<FinishedTask>を生成
                    FinishedTaskList = apiData.Select(x => new FinishedTask
                    {
                        jobId = x.JobId,
                        status = ConvertStatus(x.Status), // status対応表に基づいて変換
                        name = x.ItemName,
                        finishedAt = x.CompletedAt ?? DateTime.MinValue
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

    // メソッド ：ステータスを日本語に変換
    private string ConvertStatus(string apiStatus)
    {
        return apiStatus switch
        {
            "Canceled" => "キャンセル",
            "Completed" => "完了",
            "Aborted" => "異常終了",
            _ => apiStatus // 想定外はそのまま
        };
    }
}

public class FinishedTaskResponse
{
    [JsonPropertyName("jobId")] public string JobId { get; set; } = string.Empty;
    [JsonPropertyName("itemName")] public string ItemName { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("completedAt")] public DateTime? CompletedAt { get; set; }
}