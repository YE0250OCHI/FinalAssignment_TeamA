using Microsoft.AspNetCore.Mvc;
using SimpleAutomaticStorageSystem.Server.Controllers.Dto;
using SimpleAutomaticStorageSystem.Server.Domains;
using SimpleAutomaticStorageSystem.Server.Shared;
using SimpleAutomaticStorageSystem.Server.UseCases;
using SimpleAutomaticStorageSystem.Server.UseCases.Dto;
using System.Text.Json;

namespace SimpleAutomaticStorageSystem.Server.Controllers;

/// <summary>
/// 自動倉庫向けAPIエンドポイント
/// </summary>
[Route("api/v1/racks")]
[ApiController]
public class RacksApiController(
    ILogger<RacksApiController> logger,
    JobAssigner jobAssigner,
    JobManager jobManager,
    JobViewer jobViewer,
    JobIssuer jobIssuer,
    ClientValidator validator) : ControllerBase
{
    /// <summary>
    /// オンライン要求
    /// </summary>
    /// <param name="">JOB番号</param>
    /// <response code="204">オンライン状態に移行した</response>
    /// <response code="401">未登録端末からのアクセス</response>
    /// <response code="500">内部エラー</response>
    [HttpPost("online")]
    public async Task<IActionResult> RequestOnlineAsync()
    {
        try
        {
            // ログ＆認証を行う
            // 未登録自動倉庫からの要求 -> 401スロー
            Initialize(HttpContext, out var equipmentId);

            // オンライン状態へ移行する
            await jobManager.ChangeEquipmentStatusAsync(
                equipmentId,
                EquipmentStatus.Online,
                "装置再起動");

            logger.LogInformation(
                "自動倉庫オンライン移行 EquipmentId={EquipmentId}",
                equipmentId);

            // 成功：オンラインに遷移した
            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                StatusCodes.Status204NoContent);

            return NoContent();

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

    /// <summary>
    /// 次JOB問合せ
    /// </summary>
    /// <response code="200">実行可能なJOBが存在する</response>
    /// <response code="204">実行可能なJOBが存在しない</response>
    /// <response code="401">未登録端末からのアクセス</response>
    /// <response code="409">その自動倉庫は出庫JOBを実行可能な状態ではない（状態不一致）</response>
    /// <response code="500">内部エラー</response>
    [HttpGet("jobs")]
    public async Task<IActionResult> GetNextJobAsync()
    {
        try
        {
            // ログ＆認証を行う
            // 未登録自動倉庫からの要求 -> 401スロー
            Initialize(HttpContext, out var equipmentId);

            // JOBの割当を行う
            // その自動倉庫は出庫JOBを実行可能な状態ではない -> 409スロー
            AssignedJobDto? jobDto =
                await jobAssigner.AssignPickingJobForEquipmentAsync(equipmentId);

            if (jobDto is not null)
            {
                // 成功：次JOBを配信
                logger.LogInformation(
                    "API正常応答 StatusCode={StatusCode}",
                    StatusCodes.Status200OK);

                return Ok(jobDto);
            }
            else
            {
                // 成功：配信するJOBがない
                logger.LogInformation(
                    "API正常応答 StatusCode={StatusCode}",
                    StatusCodes.Status204NoContent);

                return NoContent();
            }

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

    /// <summary>
    /// JOB作業開始報告
    /// </summary>
    /// <param name="id">JOB番号</param>
    /// <response code="204">状態遷移に成功した</response>
    /// <response code="401">未登録端末からのアクセス</response>
    /// <response code="403">JOBアクセスの権限がない</response>
    /// <response code="404">指定されたIDが存在しない</response>
    /// <response code="409">状態遷移ができない（状態不一致）</response>
    /// <response code="500">内部エラー</response>
    [HttpPost("jobs/{id}/initiate")]
    public async Task<IActionResult> ReportJobInitiateAsync(string id)
    {
        try
        {
            // ログ＆認証を行う
            // 未登録自動倉庫からの要求 -> 401スロー
            Initialize(HttpContext, out var equipmentId);

            // 次遷移状態を設定
            JobStatus nextStatus = JobStatus.Transferring;

            // JOBの遷移を試みる
            // アクセス権がない -> 403スロー
            // JOB番号がない -> 404スロー
            // 状態遷移に失敗 -> 409スロー
            await jobManager.ChangeJobStatusAsync(id, equipmentId, nextStatus);

            // 成功：遷移した
            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                StatusCodes.Status204NoContent);

            return NoContent();

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

    /// <summary>
    /// JOB作業完了報告
    /// </summary>
    /// <param name="id">JOB番号</param>
    /// <response code="204">状態遷移に成功した</response>
    /// <response code="401">未登録端末からのアクセス</response>
    /// <response code="403">JOBアクセスの権限がない</response>
    /// <response code="404">指定されたIDが存在しない</response>
    /// <response code="409">状態遷移ができない（状態不一致）</response>
    /// <response code="500">内部エラー</response>
    [HttpPost("jobs/{id}/complete")]
    public async Task<IActionResult> ReportJobCompleteAsync(string id)
    {
        try
        {
            // ログ＆認証を行う
            // 未登録自動倉庫からの要求 -> 401スロー
            Initialize(HttpContext, out var equipmentId);

            // JOBの取得
            // JOB番号がない -> 404スロー
            JobModel job = await jobViewer.GetJobAsync(id);

            // 次遷移状態を設定
            JobStatus nextStatus = job.JobType switch
            {
                JobType.Picking => JobStatus.WaitOut,
                JobType.Putaway => JobStatus.Completed,
                _ => throw new InvalidOperationException($"JOB種別異常 JobId={id}")
            };

            // JOBの遷移を試みる
            // アクセス権がない -> 403スロー
            // 状態遷移に失敗 -> 409スロー
            await jobManager.ChangeJobStatusAsync(id, equipmentId, nextStatus);

            // 成功：遷移した
            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                StatusCodes.Status204NoContent);

            return NoContent();

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

    /// <summary>
    /// 取出し完了報告
    /// </summary>
    /// <param name="id">JOB番号</param>
    /// <response code="204">状態遷移に成功した</response>
    /// <response code="401">未登録端末からのアクセス</response>
    /// <response code="403">JOBアクセスの権限がない</response>
    /// <response code="404">指定されたIDが存在しない</response>
    /// <response code="409">状態遷移ができない（状態不一致）</response>
    /// <response code="500">内部エラー</response>
    [HttpPost("jobs/{id}/remove")]
    public async Task<IActionResult> ReportItemRemoveAsync(string id)
    {
        try
        {
            // ログ＆認証を行う
            // 未登録自動倉庫からの要求 -> 401スロー
            Initialize(HttpContext, out var equipmentId);

            // 次遷移状態を設定
            JobStatus nextStatus = JobStatus.Completed;

            // JOBの遷移を試みる
            // アクセス権がない -> 403スロー
            // JOB番号がない -> 404スロー
            // 状態遷移に失敗 -> 409スロー
            await jobManager.ChangeJobStatusAsync(id, equipmentId, nextStatus);

            // 成功：遷移した
            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                StatusCodes.Status204NoContent);

            return NoContent();

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

    /// <summary>
    /// 入庫要求
    /// </summary>
    /// <response code="201">入庫JOBの作成に成功した</response>
    /// <response code="400">入庫要求のレスポンスが不正</response>
    /// <response code="401">未登録端末からのアクセス</response>
    /// <response code="409">その自動倉庫は入庫JOBを実行可能な状態ではない（状態不一致）</response>
    /// <response code="422">商品の品種コードが不正、または、在庫がない</response>
    /// <response code="500">内部エラー</response>
    [HttpPost("putaway-order")]
    public async Task<IActionResult> RequestPutawayJobAsync()
    {
        try
        {
            // ログ＆認証を行う
            // 未登録自動倉庫からの要求 -> 401スロー
            Initialize(HttpContext, out var equipmentId);

            // JSONの確認
            // 変換失敗で、JsonException発生 -> 400スロー
            PutawayRequest putawayRequest =
                await JsonSerializer.DeserializeAsync<PutawayRequest>(Request.Body)
                ?? throw new JsonException();

            // 採番

            string jobId;


            // 入庫JOBの登録を実行
            // 品種コードが不正 -> 422スロー

            string itemId;

            /*
             * 
             * JobIssuerに処理を委譲
             * 
             */

            // 入庫JOBの割り当てを実行
            // 入庫JOBの割り当てに失敗 -> 409スロー
            AssignedJobDto? jobDto =
                await jobAssigner.AssignPutawayJobForEquipmentAsync(equipmentId, itemId, jobId);

            // 成功：入庫JOBを作成した
            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                StatusCodes.Status201Created);

            return Created(
                $"/api/v1/racks/jobs/{jobDto.JobId}",
                jobDto);

        }
        catch (JsonException)
        {
            // JSON変換失敗時の例外処理
            logger.LogWarning(
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                StatusCodes.Status400BadRequest,
                "リクエストボディが不正");

            return StatusCode(
                StatusCodes.Status400BadRequest,
                new { error = "INVALID_REQUEST" });

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

    /// <summary>
    /// エラー報告
    /// </summary>
    /// <response code="204">エラーを受領した</response>
    /// <response code="400">エラー報告のレスポンスが不正</response>
    /// <response code="401">未登録端末からのアクセス</response>
    /// <response code="500">内部エラー</response>
    [HttpPost("alarms")]
    public async Task<IActionResult> ReportAlarmsAsync()
    {
        int statusCode;

        try
        {
            // ログ＆認証を行う
            // 未登録自動倉庫からの要求 -> 401スロー
            Initialize(HttpContext, out var equipmentId);

            // JSONの確認
            // 変換失敗で、JsonException発生 -> 400スロー
            AlarmRequest alarmRequest =
                await JsonSerializer.DeserializeAsync<AlarmRequest>(Request.Body)
                ?? throw new JsonException();

            logger.LogWarning(
                "自動倉庫異常 EquipmentId={EquipmentId} AlarmCode={AlarmCode} OccurredAt={OccurredAt}",
                equipmentId,
                alarmRequest.AlarmCode,
                alarmRequest.OccurredAt);

            // 装置オフライン化
            await jobManager.ChangeEquipmentStatusAsync(
                equipmentId,
                EquipmentStatus.Offline,
                "装置アラーム報告");

            logger.LogWarning(
                "自動倉庫オフライン移行 EquipmentId={EquipmentId}",
                equipmentId);


            // 成功：受領した
            statusCode = 204;

            logger.LogInformation(
                "API正常応答 StatusCode={StatusCode}",
                statusCode);

            return NoContent();

        }
        catch (JsonException)
        {
            // JSON変換失敗時の例外処理
            logger.LogWarning(
                "API異常応答 StatusCode={StatusCode} Reason={Reason}",
                StatusCodes.Status400BadRequest,
                "リクエストボディが不正");

            return StatusCode(
                StatusCodes.Status400BadRequest,
                new { error = "INVALID_REQUEST" });

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

    // API受信時の初期化処理
    private void Initialize(HttpContext context, out string equipmentId)
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
        if (!validator.IsValidEquipment(ip, out var eq))
        {
            throw new UnregisteredDeviceException();
        }

        equipmentId = eq ??
            throw new UnregisteredDeviceException();
    }
}