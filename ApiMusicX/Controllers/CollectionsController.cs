using ApiMusicX.Configuration;
using ApiMusicX.Dtos;
using ApiMusicX.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiMusicX.Controllers;

/// <summary>
/// Consulta e administra Colecoes locais de discografias.
/// </summary>
[ApiController]
[Route("apiLocal/collections")]
[Authorize]
public sealed class CollectionsController(
    IMusicCollectionService collectionService,
    IMusicCollectionImportService importService) : ControllerBase
{
    /// <summary>
    /// Lista Colecoes locais de forma paginada.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogReadPolicy)]
    [ProducesResponseType(typeof(PagedResponse<MusicCollectionSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<MusicCollectionSummaryDto>>> List(
        [FromQuery] MusicCollectionQuery query,
        CancellationToken cancellationToken)
        => Ok(await collectionService.ListCollectionsAsync(query, cancellationToken));

    /// <summary>
    /// Obtem uma Colecao local completa, com releases e faixas.
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogReadPolicy)]
    [ProducesResponseType(typeof(MusicCollectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MusicCollectionDto>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var collection = await collectionService.GetCollectionAsync(id, cancellationToken);
        return collection is null ? NotFound() : Ok(collection);
    }

    /// <summary>
    /// Lista os releases de uma Colecao com suas faixas.
    /// </summary>
    [HttpGet("{id:long}/releases")]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogReadPolicy)]
    [ProducesResponseType(typeof(PagedResponse<MusicReleaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<MusicReleaseDto>>> ListReleases(
        long id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await collectionService.ListCollectionReleasesAsync(id, page, pageSize, cancellationToken));

    /// <summary>
    /// Cria uma Colecao local vinculada a artistas existentes.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogWritePolicy)]
    [ProducesResponseType(typeof(MusicCollectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MusicCollectionDto>> Create(
        [FromBody] CreateMusicCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var collection = await collectionService.CreateCollectionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = collection.MusicCollectionId }, collection);
    }

    /// <summary>
    /// Atualiza os metadados de uma Colecao local.
    /// </summary>
    [HttpPut("{id:long}")]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogWritePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateMusicCollectionRequest request,
        CancellationToken cancellationToken)
    {
        await collectionService.UpdateCollectionAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Importa um conjunto normalizado de forma idempotente e nao destrutiva.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogWritePolicy)]
    [ProducesResponseType(typeof(ImportMusicCollectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ImportMusicCollectionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ImportMusicCollectionResponse>> Import(
        [FromBody] ImportMusicCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await importService.ImportAsync(request, cancellationToken);
        return result.Created
            ? StatusCode(StatusCodes.Status201Created, result)
            : Ok(result);
    }

    /// <summary>
    /// Remove uma Colecao sem remover releases compartilhados.
    /// </summary>
    [HttpDelete("{id:long}")]
    [Authorize(Policy = ApiAuthorizationPolicies.CatalogDeletePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await collectionService.DeleteCollectionAsync(id, cancellationToken);
        return NoContent();
    }
}
