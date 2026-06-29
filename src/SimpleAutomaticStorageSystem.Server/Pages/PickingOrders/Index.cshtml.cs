using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;
using SimpleAutomaticStorageSystem.Server.UseCases.Ports;
using SimpleAutomaticStorageSystem.Server.UseCases.Response;
using SimpleAutomaticStorageSystem.Server.UseCases.UseCaseDto;
using System.ComponentModel.DataAnnotations;

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

    // 出庫依頼対象の商品
    [BindProperty(SupportsGet = true)]
    [Required(ErrorMessage = "商品を選択してください。")]
    public string? SelectedItem { get; set; }

    // 出庫依頼対象リスト
    public SelectList? ItemOptions { get; set; }

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
    public async Task<IActionResult> OnPostOrderAsync()
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
            return RedirectToPage(new { jobId = (string?)null }); // GET再実行
        }
        catch (InvalidStatusException)
        {
            // キャンセルができなかった
            ErrorMessage = """
                    キャンセル操作はできません。
                    すでに処理が実行中です。
                    """;

            return RedirectToPage(new { jobId = (string?)null }); // GET再実行
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
        List<AvailableItemsResponse> itemTypes =
            await inventoryViewer.GetItemListAsync();

        // ドロップダウンリストに格納
        ItemOptions = new SelectList(
            itemTypes,
            nameof(AvailableItemsResponse.KeyCode),
            nameof(AvailableItemsResponse.Text));
    }
}
