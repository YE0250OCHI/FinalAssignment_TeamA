using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace SimpleAutomaticStorageSystem.Server.Controllers;

/// <summary>
/// スマホ向けAPIエンドポイント
/// </summary>
[Route("api/v1/picking-orders")]
[ApiController]
public class OrdersApiController : ControllerBase
{
    /// <summary>
    /// 未完了出庫依頼状況取得
    /// </summary>
    /// <returns>レスポンス</returns>
    [HttpGet]
    public async Task<IActionResult> GetJobsAsync()
    {
        return Ok();
    }

    // =========================
    //   プライベートメソッド
    // =========================

    // クライアントの情報を取得する
    private (IPAddress? IpAddress, string Method, PathString PathString, QueryString QueryString) GetClientInfo()
    {
        IPAddress? ip = HttpContext.Connection.RemoteIpAddress;
        HttpRequest request = HttpContext.Request;

        string method = request.Method;
        PathString path = request.Path;
        QueryString query = request.QueryString;

        return (ip, method, path, query);
    }

}
