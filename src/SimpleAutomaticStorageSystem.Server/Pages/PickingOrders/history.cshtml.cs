using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;
using SimpleAutomaticStorageSystem.Server.UseCases.Response;

namespace SimpleAutomaticStorageSystem.Server.Pages.PickingOrders;

public class HistoryModel(
    ILogger<HistoryModel> logger,
    JobViewer jobViewer,
    ClientValidator clientValidator) : PageModel
{
    // 完了日検索の始まり
    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; } = DateTime.Today;

    // 完了日検索の終わり
    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    // 並び替え条件
    [BindProperty(SupportsGet = true)]
    public HistorySortOrder SelectedOption { get; set; } = HistorySortOrder.Latest;

    // 並び替え条件の選択肢
    public List<SelectListItem> SortOptions { get; set; } = [];

    // 履歴リスト
    public List<HistoryJobInfo> HistoryJobs { get; set; } = [];


    // メソッド ：履歴ページ表示
    public async Task<IActionResult> OnGetAsync()
    {
        // 認可外スマホの場合は拒否
        if (!IsValidDevice(out string? deviceId))
        {
            return Unauthorized(); // 401 Unauthorized
        }

        // 並び替え条件リストの組立
        SortOptions = [
            new SelectListItem
            {
                Value=HistorySortOrder.Latest.ToString(),
                Text="最新順"
            },
            new SelectListItem
            {
                Value=HistorySortOrder.Oldest.ToString(),
                Text="終了順"
            }];

        try
        {

            // デバイスIDの確認
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new InvalidOperationException("デバイスIDの取得に失敗。");
            }

            if (FromDate.HasValue &&
                ToDate.HasValue &&
                ToDate.Value.Date < FromDate.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(ToDate),
                    "終了日は開始日以降を指定してください。");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 出庫履歴の取得
            HistoryJobsResponse response =
                await jobViewer.GetHistoryJobsResponseAsync(
                    deviceId, FromDate, ToDate, SelectedOption);

            // プロパティに保存
            HistoryJobs = response.Results;

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
