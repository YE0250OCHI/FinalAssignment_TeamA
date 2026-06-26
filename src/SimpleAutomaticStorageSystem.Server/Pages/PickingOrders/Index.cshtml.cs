using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Dto;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;

namespace SimpleAutomaticStorageSystem.Server.Pages.PickingOrders;

public class IndexModel(
    ILogger<IndexModel> logger,
    InventoryViewer inventoryViewer,
    JobIssuer jobIssuer,
    JobAssigner jobAssigner,
    JobManager jobManager,
    IJobDispatcher jobDispatcher,
    ClientValidator clientValidator) : PageModel
{
    // エラーメッセージ表示部
    [TempData]
    public string? ErrorMessage { get; set; }

    // ドロップダウンリスト本体
    public List<SelectListItem> ItemList { get; set; } = [];

    // ドロップダウンリストの選択肢
    [BindProperty(SupportsGet = true)]
    public string SelectedItem { get; set; } = string.Empty;

    // メソッド ：一覧ページ表示 
    public async Task<IActionResult> OnGetAsync()
    {
        // 認可外スマホには画面を提供しない
        if (!IsValidDevice(out _))
        {
            return Unauthorized(); // 401 Unauthorized
        }

        try
        {
            // ドロップダウンリスト組立
            await LoadItemListAsync();

            // 画面を表示
            return Page(); // 200 OK
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "画面表示失敗");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "内部エラーが発生しました。");
        }

    }

    // メソッド　：出庫依頼
    public async Task<IActionResult> OnPostAddOrderAsync()
    {
        // 認可外スマホの場合は拒否
        if (!IsValidDevice(out string? deviceId))
        {
            return Unauthorized(); // 401 Unauthorized
        }

        try
        {
            // デバイスIDの確認
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new InvalidOperationException("デバイスIDの取得に失敗。");
            }

            // 入力値確認
            if (string.IsNullOrWhiteSpace(SelectedItem))
            {
                // 品種指定が不正
                ErrorMessage = "品種が選択されていません。";

                return RedirectToPage(); // GET再実行
            }

            // 出庫JOBの作成
            string jobId = await jobIssuer.CreatePickingJobAsync(deviceId, SelectedItem);

            // 出庫JOBの割当試行
            AssignedJobDto? jobDto = await jobAssigner.AssignItemForJobAsync(jobId);

            if (jobDto is not null) 
            {
                // 割当成功したらプッシュ送信
                await jobDispatcher.PushAsync(jobDto);
            }

            // 正常終了として元に戻る
            return RedirectToPage(); // GET再実行

        }
        catch (InvalidItemCodeException)
        {
            // 品種コードが不正
            ErrorMessage = $"品種コード:{SelectedItem}は存在しません。";

            return RedirectToPage(); // GET再実行
        }
        catch (Exception ex)
        {
            // 内部エラー
            logger.LogError(
                ex,
                "JOB作成処理を失敗");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "内部エラーが発生しました。");
        }
    }


    // メソッド　：キャンセル依頼
    public async Task<IActionResult> OnPostCancelAsync(string? jobId)
    {
        // 認可外スマホの場合は拒否
        if (!IsValidDevice(out string? deviceId))
        {
            return Unauthorized(); // 401 Unauthorized
        }

        try
        {
            // デバイスIDの確認
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new InvalidOperationException("デバイスIDの取得に失敗。");
            }

            // jobIdがなければエラー
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new InvalidOperationException("JOB番号が指定されていない。");
            }

            // 出庫JOBキャンセルを呼出し
            await jobManager.CancelUnassignedJobAsync(jobId, deviceId);
            

            // 正常終了として元に戻る
            return RedirectToPage(); // GET再実行
        }
        catch (InvalidStatusException)
        {
            // キャンセルができなかった
            ErrorMessage = """
                    キャンセル操作はできません。
                    すでに処理が実行中です。
                    """;

            return RedirectToPage(); // GET再実行
        }
        catch (JobAccessDeniedException)
        {
            // アクセス許可がない（担当端末ではない）
            return StatusCode(
                StatusCodes.Status403Forbidden,
                "JOBのアクセス許可がありません。");
        }
        catch (JobNotFoundException)
        {
            // JobIdが存在しない
            return StatusCode(
                StatusCodes.Status404NotFound,
                $"JOB番号:{jobId}は存在しません。");
        }
        catch (Exception ex)
        {
            // 内部エラー
            logger.LogError(
                ex,
                "JOBキャンセル処理を失敗");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "内部エラーが発生しました。");
        }
    }

    // =========================
    //   プライベートメソッド
    // =========================

    /// <summary>
    /// スマートフォンの認証を行う
    /// </summary>
    /// <param name="deviceId">取得したスマホID</param>
    /// <returns>認可されたスマートフォンか</returns>
    private bool IsValidDevice(out string? deviceId)
    {
        // IPアドレスを取得
        string ipAddress =
            HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ??
            string.Empty;

        // 認証を行う
        if (!clientValidator.IsValidDevice(ipAddress, out deviceId))
        {
            logger.LogWarning(
                "認可外スマホからのアクセス Ip={Ip}",
                ipAddress);

            return false;
        }

        logger.LogInformation(
            "スマホアクセス DeviceId={DeviceId} Method={Method} Url={Url}",
            deviceId,
            HttpContext.Request.Method,
            HttpContext.Request.Path);

        return true;

    }

    /// <summary>
    /// ドロップダウンリスト初期化
    /// </summary>
    private async Task LoadItemListAsync()
    {
        // 部品リストをDBから取得
        List<ItemTypeModel> itemTypes =
            await inventoryViewer.GetItemListAsync();

        // ドロップダウンリストに格納
        ItemList =
            [.. itemTypes.Select(x =>
                    new SelectListItem
                    {
                        Text = $"{x.ItemCode} {x.ItemName}",
                        Value = x.ItemCode
                    })];
    }





    /*
     * JSON変換はJS側の仕事
     * 
     * 
     * 
     * 
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
    *
    *
    *
    *
    */

}
