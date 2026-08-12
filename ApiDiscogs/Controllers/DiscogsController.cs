using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiDiscogs.Controllers;

/// <summary>
/// Expõe a superfície HTTP da API Discogs local.
/// </summary>
[ApiController]
[Route("ApiDiscogs")]
[Authorize]
public sealed class DiscogsController : ControllerBase
{
    /// <summary>
    /// Verifica somente se a API local está disponível; nenhuma requisição externa é feita.
    /// </summary>
    /// <returns>O estado operacional local da API Discogs.</returns>
    [HttpGet("health")]
    [Authorize(Policy = ApiAuthorizationPolicies.HealthReadPolicy)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ApiHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<ApiHealthResponse> Health()
        => Ok(new ApiHealthResponse("ok", "ApiDiscogs"));
}

/// <summary>
/// Resposta mínima e não sensível do health local.
/// </summary>
public sealed record ApiHealthResponse(string Status, string Service);
