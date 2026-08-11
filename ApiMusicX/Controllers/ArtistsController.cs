using ApiMusicX.Configuration;
using ApiMusicX.Dtos;
using ApiMusicX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiMusicX.Controllers;

/// <summary>
/// Consulta artistas, bandas e grupos da Colecao local.
/// </summary>
[ApiController]
[Route("apiLocal/artists")]
[Authorize]
public sealed class ArtistsController(IMusicCollectionService collectionService) : ControllerBase
{
    /// <summary>
    /// Busca artistas por nome ou alias com paginacao.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogReadPolicy)]
    [ProducesResponseType(typeof(PagedResponse<MusicArtistSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<MusicArtistSummaryDto>>> Search(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await collectionService.SearchArtistsAsync(search, page, pageSize, cancellationToken));

    /// <summary>
    /// Obtem um artista com aliases e Colecoes relacionadas.
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogReadPolicy)]
    [ProducesResponseType(typeof(MusicArtistDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MusicArtistDto>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var artist = await collectionService.GetArtistAsync(id, cancellationToken);
        return artist is null ? NotFound() : Ok(artist);
    }
}
