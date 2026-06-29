using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;
using SimpleAutomaticStorageSystem.Server.UseCases.Response;
using SimpleAutomaticStorageSystem.Server.UseCases.UseCaseDto;

namespace SimpleAutomaticStorageSystem.Server.Controllers;

/// <summary>
/// スマホ向けAPIエンドポイント
/// </summary>
[Route("api/v1/picking-orders")]
[ApiController]
public class OrdersApiController(
    ILogger<OrdersApiController> logger,
    JobViewer jobViewer,
    InventoryViewer inventoryViewer,
    ClientValidator validator) : ControllerBase
{
    /// <summary>
    /// 未完了出庫依頼状況取得
    /// </summary>
    /// <response code="200">未完了JOB状態</response>
    /// <response code="401">未登録端末からのアクセス</response>
    /// <response code="500">内部エラー</response>
    [HttpGet]
    public async Task<IActionResult> GetJobsAsync()
    {
        try
        {
            // ログ＆認証を行う
            // 未登録スマホからの要求 -> 401スロー
            OrdersApiInitialize(out var deviceId);

            // 出庫可能リスト取得
            List<AvailableItemsResponse> availableItems =
                await inventoryViewer.GetItemListAsync();


            // 未完了JOBの最新状態を取得する
            IncompleteJobsResponse incompleteJobs =
                await jobViewer.GetIncompleteJobsResponseAsync(deviceId);

            // レスポンス組立
            var response = new
            {
                Available = availableItems,
                Statuses = incompleteJobs.Results
            };

            // JOB状態の返却
            return Ok(response);

        }
        catch (ApiException ex)
        {
            // 業務エラー系の例外処理
            logger.LogWarning(
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                ex.StatusCode,
                ex.Message);

            return StatusCode(
                ex.StatusCode,
                new { error = ex.ErrorCode });

        }
        catch (Exception ex)
        {
            // 内部エラーの例外処理
            logger.LogError(
                ex,
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                StatusCodes.Status500InternalServerError,
                "内部エラー");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = HttpErrors.UNEXPECTED_ERROR });

        }
    }

    // =========================
    //   プライベートメソッド
    // =========================

    // 初期化
    private void OrdersApiInitialize(out string deviceId)
    {
        HttpContext context = HttpContext;

        string? ip = context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty;
        HttpRequest request = context.Request;

        string method = request.Method;
        PathString path = request.Path;
        QueryString query = request.QueryString;


        // ログ
        logger.LogInformation(
            "API要求受信 Ip={Ip} Method={Method} Uri={Path}{Query}",
            ip,
            method,
            path,
            query);

        // スマホの認証
        if (!validator.IsValidDevice(ip, out var device))
        {
            throw new UnregisteredDeviceException();
        }

        deviceId = device ??
            throw new UnregisteredDeviceException();
    }
}