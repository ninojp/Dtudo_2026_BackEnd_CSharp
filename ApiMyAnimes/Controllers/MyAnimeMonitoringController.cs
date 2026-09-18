using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos.FileMonitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Controllers;

/// <summary>Associa a colecao a uma pasta existente, sem acessar ou alterar arquivos.</summary>
[ApiController]
[Route("apiLocal/MyAnime/{myAnimeId:int}/monitoring-location")]
[Authorize(Policy = "permission:catalog.write")]
public sealed class MyAnimeMonitoringController(MyAnimesContext context) : ControllerBase
{
    /// <summary>Consulta o vinculo de monitoramento da colecao.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MyAnimeMonitoringLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyAnimeMonitoringLocationDto>> Get(int myAnimeId, CancellationToken cancellationToken)
    {
        var location = await context.MonitoringLocations.AsNoTracking().FirstOrDefaultAsync(item => item.MyAnimeId == myAnimeId, cancellationToken);
        return location is null ? NotFound() : Ok(new MyAnimeMonitoringLocationDto(location.RootKey, location.RelativePath));
    }

    /// <summary>Define explicitamente a localizacao; altera somente o vinculo no banco.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(MyAnimeMonitoringLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyAnimeMonitoringLocationDto>> Put(int myAnimeId, MyAnimeMonitoringLocationDto request, CancellationToken cancellationToken)
    {
        if (!MonitoringPathPolicy.CollectionRoots().Any(root => root.Key == request.RootKey)
            || !MonitoringPathPolicy.IsValidRelativePath(request.RelativePath))
            return BadRequest("Selecione uma pasta de colecao dentro de uma raiz autorizada.");
        if (!await context.MyAnimes.AnyAsync(item => item.Id == myAnimeId, cancellationToken)) return NotFound();
        var location = await context.MonitoringLocations.FindAsync([myAnimeId], cancellationToken);
        if (location is null)
        {
            location = new MyAnimeMonitoringLocation { MyAnimeId = myAnimeId };
            context.MonitoringLocations.Add(location);
        }
        location.RootKey = request.RootKey;
        location.RelativePath = request.RelativePath;
        await context.SaveChangesAsync(cancellationToken);
        return Ok(request);
    }
}
