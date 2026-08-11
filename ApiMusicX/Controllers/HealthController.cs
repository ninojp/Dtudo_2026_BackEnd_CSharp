using ApiMusicX.Configuration;
using ApiMusicX.Data;
using ApiMusicX.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace ApiMusicX.Controllers;

/// <summary>
/// Exposes the authenticated liveness status of the ApiMusicX process.
/// </summary>
[ApiController]
[Route("apiLocal/[controller]")]
[Authorize(Policy = ApiAuthorizationPolicies.HealthReadPolicy)]
public sealed class HealthController(
    MusicContext context,
    ILogger<HealthController> logger) : ControllerBase
{
    /// <summary>
    /// Checks whether the ApiMusicX process is running.
    /// </summary>
    /// <remarks>
    /// This endpoint reports only the API process. Database readiness will be added with the persistence model in a later phase.
    /// </remarks>
    /// <returns>A minimal health response without configuration or secret values.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        try
        {
            if (!await context.Database.CanConnectAsync(cancellationToken))
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new HealthResponse("unavailable", "ApiMusicX", "unavailable"));
            }

            return Ok(new HealthResponse("ok", "ApiMusicX", "ok"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao verificar a disponibilidade do banco local da ApiMusicX.");
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new HealthResponse("unavailable", "ApiMusicX", "unavailable"));
        }
    }
}
