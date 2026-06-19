using Microsoft.AspNetCore.Mvc;

namespace SimpleAutomaticStorageSystem.Server.Controllers;

[Route("api/v1/picking-orders")]
[ApiController]
public class OrdersApiController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetJobsAsync()
    {
        return Ok();
    }

}
