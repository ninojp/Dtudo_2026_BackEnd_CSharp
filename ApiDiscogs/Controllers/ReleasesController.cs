using ApiDiscogs.Dtos;
using ApiDiscogs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiDiscogs.Controllers;

/// <summary>
/// Consulta detalhes de releases e master releases da Discogs.
/// </summary>
[ApiController]
[Route("ApiDiscogs")]
[Authorize]
public sealed class ReleasesController(IDiscogsService discogsService) : ControllerBase
{
    /// <summary>
    /// Obtem os detalhes de um release concreto, incluindo tracklist quando fornecida.
    /// </summary>
    /// <param name="discogsReleaseId">ID decimal positivo do release na Discogs.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Detalhes normalizados do release.</returns>
    [HttpGet("releases/{discogsReleaseId}")]
    [Authorize(Policy = ApiAuthorizationPolicies.ExternalCatalogReadPolicy)]
    [Produces("application/json", "application/problem+json")]
    [ProducesResponseType(typeof(DiscogsReleaseDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<DiscogsReleaseDetails>> GetRelease(
        string discogsReleaseId,
        CancellationToken cancellationToken)
    {
        var releaseId = DiscogsRequestValidator.ParseResourceId(
            discogsReleaseId,
            nameof(discogsReleaseId));
        return Ok(await discogsService.GetReleaseAsync(releaseId, cancellationToken));
    }

    /// <summary>
    /// Obtem os detalhes de um master release sem fallback silencioso para release.
    /// </summary>
    /// <param name="discogsMasterId">ID decimal positivo do master na Discogs.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Detalhes normalizados do master release.</returns>
    [HttpGet("masters/{discogsMasterId}")]
    [Authorize(Policy = ApiAuthorizationPolicies.ExternalCatalogReadPolicy)]
    [Produces("application/json", "application/problem+json")]
    [ProducesResponseType(typeof(DiscogsMasterDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<DiscogsMasterDetails>> GetMaster(
        string discogsMasterId,
        CancellationToken cancellationToken)
    {
        var masterId = DiscogsRequestValidator.ParseResourceId(
            discogsMasterId,
            nameof(discogsMasterId));
        return Ok(await discogsService.GetMasterAsync(masterId, cancellationToken));
    }
}
