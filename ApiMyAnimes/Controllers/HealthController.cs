using ApiMyAnimes.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace ApiMyAnimes.Controllers;

[ApiController]
[Route("apiLocal/[controller]")]
[Authorize(Policy = "permission:health.read")]
public sealed class HealthController(
    MyAnimesContext context,
    ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            if (!await context.Database.CanConnectAsync(cancellationToken))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "unavailable",
                    service = "ApiMyAnimes",
                    database = "unavailable"
                });
            }

            return Ok(new
            {
                status = "ok",
                service = "ApiMyAnimes",
                database = "ok"
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao verificar a disponibilidade do banco de dados local.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unavailable",
                service = "ApiMyAnimes",
                database = "unavailable"
            });
        }
    }
}
