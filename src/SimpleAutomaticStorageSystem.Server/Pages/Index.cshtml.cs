using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;



public class IndexModel : PageModel
{

    //一覧取得用Handler追加
    public IActionResult OnGetFetchTasks()
    {
        var response = new IncompleteTaskResponse();

        return new JsonResult(response.IncompleteTaskList);
    }


    //出力　：エラーメッセージ表示部
    [BindProperty]
    public string? ErrorMessage { get; set; }
    //入力　：部品選択のプルダウン
    [BindProperty]
    public string selectedItem { get; set; } = string.Empty;
    //フラグ　：エラーポップアップ
    public bool ShowCancelErrorPopup { get; set; } = false;


    public IActionResult OnGet()
    {
        /*JSプログラム起動のトリガ追加*/
        return Page();
    }


    // メソッド　：追加ボタン押下時処理
    public async Task<IActionResult> OnPostAddOrderAsync()
    {
        if (string.IsNullOrEmpty(selectedItem))
        {
            ErrorMessage = "部品を選択してください";
            return Page();
        }

        try
        {
            // 正常終了(ステータスコード:200)時の処理
            if (response.statusCode == 200)
            {
                /*JSプログラム起動のトリガ追加*/
                return Page();
            }

            // 異常終了時の処理
            else
            {
                //エラーメッセージを格納(HandleApiErrorAsync)
                //エラーポップアップのフラグをtrueに
            }
        }
        //サーバからのレスポンスがない場合の処理
        catch
        {
            ErrorMessage = "通信エラーが発生しました。しばらく経ってから再度お試しください。";
            return Page();
        }
    }
    //メソッド　：キャンセルボタン押下時処理
    /*欲しいレスポンス：ステータスコードのみ*/
    public async Task<IActionResult> OnPostCancelAsync(string jobId)
    {
        try
        {
            var Data = new CancelTaskData();
            /*キャンセル処理のコード追加予定*/

            // 正常終了(ステータスコード:201)時の処理
            if (Data.statusCode == 201)
            {
                /*JSでデータ取得を実行するプログラム追加予定*/
                return new JsonResult(new
                {
                    success = true
                });
            }
            // 異常終了時の処理
            else
            {
                ErrorMessage = await HandleApiErrorAsync(Data.statusCode);
                //キャンセル失敗のポップアップ
                ShowCancelErrorPopup = true;
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
            return Page();
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