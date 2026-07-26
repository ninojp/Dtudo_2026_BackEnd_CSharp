using Microsoft.AspNetCore.Mvc;

namespace ApiMyAnimes.Controllers;

[ApiController]
[Route("apiLocal/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "ok", service = "ApiMyAnimes" });
}
