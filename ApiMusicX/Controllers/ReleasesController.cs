using ApiMusicX.Configuration;
using ApiMusicX.Dtos;
using ApiMusicX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiMusicX.Controllers;

/// <summary>
/// Consulta releases locais e suas faixas.
/// </summary>
[ApiController]
[Route("apiLocal/releases")]
[Authorize]
public sealed class ReleasesController(IMusicCollectionService collectionService) : ControllerBase
{
    /// <summary>
    /// Obtem um release completo com artistas, faixas e referencias locais.
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogReadPolicy)]
    [ProducesResponseType(typeof(MusicReleaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MusicReleaseDto>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var release = await collectionService.GetReleaseAsync(id, cancellationToken);
        return release is null ? NotFound() : Ok(release);
    }
}
