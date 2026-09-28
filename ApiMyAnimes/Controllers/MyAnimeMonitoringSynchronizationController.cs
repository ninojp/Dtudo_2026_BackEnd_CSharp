using System.Data.Common;
using ApiMyAnimes.Services;
using LibDtudo.Shared.Dtos.FileMonitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Controllers;

/// <summary>Coordena a aplicacao segura das descobertas de colecoes locais.</summary>
[ApiController]
[Route("apiLocal/MyAnime/monitoring-synchronizations")]
[Authorize(Policy = "permission:catalog.write")]
public sealed class MyAnimeMonitoringSynchronizationController(
    MyAnimeMonitoringSynchronizationService synchronization) : ControllerBase
{
    /// <summary>Valida e aplica uma descoberta; arquivos e pastas nunca sao alterados.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CollectionSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CollectionSyncResultDto>> Synchronize(
        CollectionSyncRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await synchronization.SynchronizeAsync(request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: exception.Message);
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: exception.Message);
        }
    }

    /// <summary>Consulta uma sincronizacao persistida e todas as suas ocorrencias.</summary>
    [HttpGet("{runId:guid}")]
    [ProducesResponseType(typeof(CollectionSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionSyncResultDto>> Get(Guid runId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await synchronization.GetAsync(runId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
