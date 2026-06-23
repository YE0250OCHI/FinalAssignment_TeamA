using Microsoft.AspNetCore.Mvc;
using NLog;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace SimpleAutomaticStorageSystem.Server.Controllers;

/// <summary>
/// スマホ向けAPIエンドポイント
/// </summary>
[Route("api/v1/picking-orders")]
[ApiController]
public class OrdersApiController(
    ILogger<RacksApiController> logger,
    JobViewer jobViewer,
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
            // API受信時の処理
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

            // 自動倉庫の認証
            if (validator.IsValidDevice(ip, out var EquipmentId) == false)
            {
                // 未登録自動倉庫からの要求 -> 401スロー
                throw new UnregisteredDeviceException();
            }

            // JOB状態の取得

                /* jobViewerから未完了JOBの状態を取得 */

            // JOB状態

                /* 上記で取得したJOBの件数を保持 */

            // JOB状態の返却
            return Ok();

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
}