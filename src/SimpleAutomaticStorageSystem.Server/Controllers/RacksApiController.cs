using Microsoft.AspNetCore.Mvc;
using SimpleAutomaticStorageSystem.Server.Infrastructures;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;
using System.Text.Json;

namespace SimpleAutomaticStorageSystem.Server.Controllers;

/// <summary>
/// 自動倉庫向けAPIエンドポイント
/// </summary>
[Route("api/v1/racks")]
[ApiController]
public class RacksApiController(
    ILogger<RacksApiController> logger,
    JobManager jobManager,
    JobAssigner jobAssigner,
    JobIssuer jobIssuer,
    ClientValidator validator) : ControllerBase
{
    /// <summary>
    /// オンライン要求
    /// </summary>
    /// <param name="">JOB番号</param>
    /// <returns>レスポンス</returns>
    [HttpPost("online")]
    public async Task<IActionResult> RequestOnlineAsync()
    {
        int statusCode;

        try
        {
            // ログ＆認証を行う
            if (TryInitialize(HttpContext, out var equipmentId) == false)
            {
                // 未登録自動倉庫からの要求
                statusCode = 403;

                logger.LogWarning(
                    "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                    statusCode,
                    "未登録自動倉庫からの要求");

                return StatusCode(
                    statusCode,
                    new { error = HttpErrors.UNREGISTERED_EQUIPMENT });

            }

            // オンライン状態へ移行する
            await jobManager.SetOnlineAsync(equipmentId);

            // 成功：オンラインに遷移した
            statusCode = 204;

            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                statusCode);

            return NoContent();

        }
        catch (Exception ex)
        {
            // 内部エラー発生
            statusCode = 500;

            logger.LogError(
                ex,
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                statusCode,
                "内部エラー");

            return StatusCode(
                statusCode,
                new { error = HttpErrors.UNEXPECTED_ERROR });

        }
    }

    /// <summary>
    /// 次JOB問合せ
    /// </summary>
    /// <returns>レスポンス</returns>
    [HttpGet("job")]
    public async Task<IActionResult> GetNextJobAsync()
    {
        int statusCode;

        try
        {
            // ログ＆認証を行う
            if (TryInitialize(HttpContext, out var equipmentId) == false)
            {
                // 未登録自動倉庫からの要求
                statusCode = 403;

                logger.LogWarning(
                    "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                    statusCode,
                    "未登録自動倉庫からの要求");

                return StatusCode(
                    statusCode,
                    new { error = HttpErrors.UNREGISTERED_EQUIPMENT });

            }

            // JOBの取得を試みる
            // JobModel? job = xxx







            // 成功：次JOBを配信


            // 成功：配信するJOBがない
            statusCode = 204;

            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                statusCode);

            return NoContent();

        }
        catch (Exception ex)
        {
            // 内部エラー発生
            statusCode = 500;

            logger.LogError(
                ex,
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                statusCode,
                "内部エラー");

            return StatusCode(
                statusCode,
                new { error = HttpErrors.UNEXPECTED_ERROR });

        }

    }

    /// <summary>
    /// JOB作業開始報告
    /// </summary>
    /// <param name="id">JOB番号</param>
    /// <returns>レスポンス</returns>
    [HttpPost("job/{id}/initiate")]
    public async Task<IActionResult> ReportJobInitiateAsync(string id)
    {
        int statusCode;

        try
        {
            // ログ＆認証を行う
            if (TryInitialize(HttpContext, out var equipmentId) == false)
            {
                // 未登録自動倉庫からの要求
                statusCode = 403;

                logger.LogWarning(
                    "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                    statusCode,
                    "未登録自動倉庫からの要求");

                return StatusCode(
                    statusCode,
                    new { error = HttpErrors.UNREGISTERED_EQUIPMENT });

            }










            // 成功：遷移した
            statusCode = 204;

            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                statusCode);

            return NoContent();

        }
        catch (Exception ex)
        {
            // 内部エラー発生
            statusCode = 500;

            logger.LogError(
                ex,
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                statusCode,
                "内部エラー");

            return StatusCode(
                statusCode,
                new { error = HttpErrors.UNEXPECTED_ERROR });

        }
    }

    /// <summary>
    /// JOB作業完了報告
    /// </summary>
    /// <param name="id">JOB番号</param>
    /// <returns>レスポンス</returns>
    [HttpPost("job/{id}/complete")]
    public async Task<IActionResult> ReportJobCompleteAsync(string id)
    {
        int statusCode;

        try
        {
            // ログ＆認証を行う
            if (TryInitialize(HttpContext, out var equipmentId) == false)
            {
                // 未登録自動倉庫からの要求
                statusCode = 403;

                logger.LogWarning(
                    "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                    statusCode,
                    "未登録自動倉庫からの要求");

                return StatusCode(
                    statusCode,
                    new { error = HttpErrors.UNREGISTERED_EQUIPMENT });

            }










            // 成功：遷移した
            statusCode = 204;

            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                statusCode);

            return NoContent();

        }
        catch (Exception ex)
        {
            // 内部エラー発生
            statusCode = 500;

            logger.LogError(
                ex,
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                statusCode,
                "内部エラー");

            return StatusCode(
                statusCode,
                new { error = HttpErrors.UNEXPECTED_ERROR });

        }
    }



    /// <summary>
    /// 取出し完了報告
    /// </summary>
    /// <param name="id">JOB番号</param>
    /// <returns>レスポンス</returns>
    [HttpPost("job/{id}/remove")]
    public async Task<IActionResult> ReportItemRemoveAsync(string id)
    {
        int statusCode;

        try
        {
            // ログ＆認証を行う
            if (TryInitialize(HttpContext, out var equipmentId) == false)
            {
                // 未登録自動倉庫からの要求
                statusCode = 403;

                logger.LogWarning(
                    "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                    statusCode,
                    "未登録自動倉庫からの要求");

                return StatusCode(
                    statusCode,
                    new { error = HttpErrors.UNREGISTERED_EQUIPMENT });

            }










            // 成功：遷移した
            statusCode = 204;

            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                statusCode);

            return NoContent();

        }
        catch (Exception ex)
        {
            // 内部エラー発生
            statusCode = 500;

            logger.LogError(
                ex,
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                statusCode,
                "内部エラー");

            return StatusCode(
                statusCode,
                new { error = HttpErrors.UNEXPECTED_ERROR });

        }
    }

    /// <summary>
    /// 入庫要求
    /// </summary>
    /// <returns>レスポンス</returns>
    [HttpPost("putaway-order")]
    public async Task<IActionResult> RequestPutawayJobAsync()
    {
        int statusCode;

        try
        {
            // ログ＆認証を行う
            if (TryInitialize(HttpContext, out var equipmentId) == false)
            {
                // 未登録自動倉庫からの要求
                statusCode = 403;

                logger.LogWarning(
                    "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                    statusCode,
                    "未登録自動倉庫からの要求");

                return StatusCode(
                    statusCode,
                    new { error = HttpErrors.UNREGISTERED_EQUIPMENT });

            }










            // 成功：入庫JOBを作成した
            statusCode = 201;

            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                statusCode);

            return Created();

        }
        catch (Exception ex)
        {
            // 内部エラー発生
            statusCode = 500;

            logger.LogError(
                ex,
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                statusCode,
                "内部エラー");

            return StatusCode(
                statusCode,
                new { error = HttpErrors.UNEXPECTED_ERROR });

        }
    }

    /// <summary>
    /// エラー報告
    /// </summary>
    /// <returns>レスポンス</returns>
    [HttpPost("alarms")]
    public async Task<IActionResult> ReportAlarmsAsync()
    {
        int statusCode;

        try
        {
            // ログ＆認証を行う
            if (TryInitialize(HttpContext, out var equipmentId) == false)
            {
                // 未登録自動倉庫からの要求
                statusCode = 403;

                logger.LogWarning(
                    "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                    statusCode,
                    "未登録自動倉庫からの要求");

                return StatusCode(
                    statusCode,
                    new { error = HttpErrors.UNREGISTERED_EQUIPMENT });

            }







            // 成功：受領した
            statusCode = 204;

            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                statusCode);

            return NoContent();

        }
        catch (JsonException)
        {
            // JSONデシリアライズ失敗
            statusCode = 400;

            logger.LogWarning(
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                statusCode,
                "JSON変換に失敗");

            return StatusCode(
                statusCode,
                new { error = HttpErrors.INVALID_REQUEST });

        }
        catch (Exception ex)
        {
            // 内部エラー発生
            statusCode = 500;

            logger.LogError(
                ex,
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                statusCode,
                "内部エラー");

            return StatusCode(
                statusCode,
                new { error = HttpErrors.UNEXPECTED_ERROR });

        }

    }


    // =========================
    //   プライベートメソッド
    // =========================

    // API受信時の初期化処理
    private bool TryInitialize(HttpContext context, out string? EquipmentId)
    {
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
        bool result = validator.IsValidDevice(ip, out EquipmentId);

        return result;
    }

    // JOBをDtoに変換する


}
