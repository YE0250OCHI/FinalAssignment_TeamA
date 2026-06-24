using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using static IndexModel;

public class HistoryModel : PageModel
{
    public class FinishedTask
    {
        public string jobId { get; set; }
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public string status { get; set; }
        public DateTime completedAt { get; set; }

    }

    [BindProperty]
    public List<FinishedTask> FinishedTaskList { get; set; } = new();

    //クラス ：終了済みタスク取得通信用
    public class FinishedTaskResponse
    {
        //生成 ：終了済みタスクリスト
        [BindProperty(SupportsGet = true)]
        public List<FinishedTask> FinishedTaskList { get; set; } = new();

        public int statusCode;
    }

    //フラグ ：エラーポップアップ
    public bool ShowCancelErrorPopup { get; set; } = false;

    // プロパティ：Viewのasp-forとバインドするための定義を追加
    [BindProperty(SupportsGet = true)]
    public DateTime? completedAt { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? taskEndDateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? sort { get; set; }

    //メソッド ：起動時の未完了タスクリスト取得
    //public async Task<IActionResult> OnGetAsync()
    //{
    //    return await GetIncompleteTask();
    //}

    // メソッド ：データ更新用のGET



    public IActionResult OnGet()
    {
        var response = new FinishedTaskResponse();
        /*データ取得を実行するプログラム追加予定*/
        FinishedTaskList = response.FinishedTaskList;
        return Page();

    }


    public async Task<IActionResult> OnPostReloadHistoryAsync()
    {
        try
        {
            var response = new FinishedTaskResponse();
            /*プルダウンの選択内容POSTのコード追加予定*/
            /*JSでデータ取得を実行するプログラム追加予定*/

            // 正常終了(ステータスコード:201)時の処理
            if (response.statusCode == 200)
            {
                return new JsonResult(new
                {
                    success = true
                });
            }

            // 異常終了時の処理
            else
            {
                string ErrorMessage = await HandleApiErrorAsync(response.statusCode);
                bool success = false;
                return new JsonResult(new
                {
                    success = false,
                    message = ErrorMessage
                });
            }
        }
        //サーバからのレスポンスがない場合の処理
        catch
        {
            string ErrorMessage = "通信エラーが発生しました。しばらく経ってから再度お試しください。";
            return Page();
        }
    }



    public async Task<string> HandleApiErrorAsync(int statusCode)
    {
        string errorCode = "";

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