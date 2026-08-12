using ApiDiscogs.Dtos;
using ApiDiscogs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiDiscogs.Controllers;

/// <summary>
/// Consulta artistas, bandas e suas discografias na fonte externa Discogs.
/// </summary>
[ApiController]
[Route("ApiDiscogs/artists")]
[Authorize]
public sealed class ArtistsController(IDiscogsService discogsService) : ControllerBase
{
    /// <summary>
    /// Busca artistas e bandas por nome, com paginacao limitada.
    /// </summary>
    /// <param name="query">Termo e parametros de pagina da busca.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Resultados de artistas normalizados pela ApiDiscogs.</returns>
    [HttpGet("search")]
    [Authorize(Policy = ApiAuthorizationPolicies.ExternalCatalogReadPolicy)]
    [Produces("application/json", "application/problem+json")]
    [ProducesResponseType(typeof(DiscogsPagedResponse<DiscogsArtistSearchItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<DiscogsPagedResponse<DiscogsArtistSearchItem>>> Search(
        [FromQuery] ArtistSearchQuery query,
        CancellationToken cancellationToken)
        => Ok(await discogsService.SearchArtistsAsync(query, cancellationToken));

    /// <summary>
    /// Obtem os detalhes normalizados de um artista ou banda.
    /// </summary>
    /// <param name="discogsArtistId">ID decimal positivo do artista na Discogs.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Perfil, aliases, membros, URLs e imagens do artista.</returns>
    [HttpGet("{discogsArtistId}")]
    [Authorize(Policy = ApiAuthorizationPolicies.ExternalCatalogReadPolicy)]
    [Produces("application/json", "application/problem+json")]
    [ProducesResponseType(typeof(DiscogsArtistDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<DiscogsArtistDetails>> GetById(
        string discogsArtistId,
        CancellationToken cancellationToken)
    {
        var artistId = DiscogsRequestValidator.ParseResourceId(
            discogsArtistId,
            nameof(discogsArtistId));
        return Ok(await discogsService.GetArtistAsync(artistId, cancellationToken));
    }

    /// <summary>
    /// Obtem uma pagina da discografia normalizada do artista.
    /// </summary>
    /// <param name="discogsArtistId">ID decimal positivo do artista na Discogs.</param>
    /// <param name="query">Pagina, limite e expansao opcional de master.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisicao.</param>
    /// <returns>Releases agregados e paginacao da fonte externa.</returns>
    [HttpGet("{discogsArtistId}/releases")]
    [Authorize(Policy = ApiAuthorizationPolicies.ExternalCatalogReadPolicy)]
    [Produces("application/json", "application/problem+json")]
    [ProducesResponseType(typeof(DiscogsArtistReleasesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<DiscogsArtistReleasesResponse>> Releases(
        string discogsArtistId,
        [FromQuery] ArtistReleasesQuery query,
        CancellationToken cancellationToken)
    {
        var artistId = DiscogsRequestValidator.ParseResourceId(
            discogsArtistId,
            nameof(discogsArtistId));
        return Ok(await discogsService.GetArtistReleasesAsync(
            artistId,
            query,
            cancellationToken));
    }
}
