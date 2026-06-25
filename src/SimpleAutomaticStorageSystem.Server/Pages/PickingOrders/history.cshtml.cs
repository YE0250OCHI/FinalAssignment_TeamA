using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleAutomaticStorageSystem.Server.Dto;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;

namespace SimpleAutomaticStorageSystem.Server.Pages.PickingOrders;

public class HistoryModel(
    ILogger<IndexModel> logger,
    JobViewer jobViewer,
    ClientValidator clientValidator) : PageModel
{
    // 完了タスクリスト
    public List<HistoryJobInfo> ClosedJobList { get; set; } = [];

    // メソッド ：履歴ページ表示
    public async Task<IActionResult> OnGetAsync(
        DateTime? from,
        DateTime? to,
        SortOrder sort = SortOrder.Latest)
    {
        // 認可外スマホの場合は拒否
        if (!IsValidDevice(out string? deviceId))
        {
            return Unauthorized(); // 401 Unauthorized
        }

        // クエリパラメータは正しく受け取れたか
        if (!ModelState.IsValid)
        {
            return BadRequest("検索条件が不正です。");
        }

        try
        {
            // デバイスIDの確認
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new InvalidOperationException("デバイスIDの取得に失敗。");
            }

            // 出庫履歴の取得
            HistoryJobsResponse response =
                await jobViewer.GetHistoryJobsResponseAsync(
                    deviceId, from, to, sort);

            // プロパティに保存
            ClosedJobList = response.Results;

            // 画面表示
            return Page();
        }
        catch (Exception ex)
        {
            // 内部エラー
            logger.LogError(
                ex,
                "履歴ページ表示処理に失敗");

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
            "スマホアクセス DeviceId={DeviceId}",
            deviceId);

        return true;

    }


}

