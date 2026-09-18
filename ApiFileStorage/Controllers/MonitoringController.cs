using System.Data.Common;
using ApiFileStorage.Monitoring;
using ApiFileStorage.Monitoring.Data;
using LibDtudo.Shared.Dtos.FileMonitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiFileStorage.Controllers;

/// <summary>Monitoramento passivo; nenhum endpoint modifica arquivos das colecoes.</summary>
[ApiController]
[Route("api/file-storage/monitoring")]
[Authorize(Policy = "permission:filesystem.command")]
public sealed class MonitoringController(IServiceProvider services, ILogger<MonitoringController> logger) : ControllerBase
{
    private string Owner => User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException();
    private MonitoringSessions Sessions => services.GetService<MonitoringSessions>()
        ?? throw new MonitoringDisabledException();

    /// <summary>Lista as raizes permitidas sem enumerar os discos.</summary>
    [HttpGet("roots")]
    [ProducesResponseType(typeof(MonitoringRootDto[]), StatusCodes.Status200OK)]
    public ActionResult<MonitoringRootDto[]> Roots() => Ok(MonitoringRoots.Collections().All.Select(root => new MonitoringRootDto(root.Key, root.Path)).ToArray());

    /// <summary>Inicia coleta somente para o escopo solicitado.</summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(MonitoringSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<ActionResult<MonitoringSessionDto>> Start(StartMonitoringRequest request, CancellationToken cancellationToken) =>
        Execute(() => Sessions.StartAsync(Owner, request, cancellationToken));

    /// <summary>Renova a sessao e consulta seu progresso, sem varrer os discos.</summary>
    [HttpPost("sessions/{id:guid}/heartbeat")]
    [ProducesResponseType(typeof(MonitoringSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<MonitoringSessionDto>> Heartbeat(Guid id) => Execute(() => Task.FromResult(Sessions.Get(id, Owner, renew: true)));

    /// <summary>Solicita nova leitura do escopo ja aberto pelo usuario.</summary>
    [HttpPost("sessions/{id:guid}/refresh")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<ActionResult<bool>> Refresh(Guid id) => Execute(() => { Sessions.Refresh(id, Owner); return Task.FromResult(true); });

    /// <summary>Encerra a coleta da sessao sem apagar seu historico.</summary>
    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public Task<ActionResult<bool>> Stop(Guid id) => Execute(async () => { await Sessions.StopAsync(id, Owner); return true; });

    /// <summary>Consulta notificacoes e historico do escopo da sessao com cursor por ID.</summary>
    [HttpGet("sessions/{id:guid}/events")]
    [ProducesResponseType(typeof(MonitoringEventDto[]), StatusCodes.Status200OK)]
    public Task<ActionResult<MonitoringEventDto[]>> Events(Guid id, long afterId = 0, int take = 200, bool latest = false, CancellationToken cancellationToken = default) => Execute(async () =>
    {
        if (afterId < 0 || take is < 1 or > 1000) throw new ArgumentException("Paginacao invalida.");
        var locations = Sessions.Get(id, Owner).Locations.Select(location => location.Id).ToArray();
        var context = services.GetRequiredService<MonitoringDbContext>();
        var query = context.Observations.AsNoTracking().Where(item => locations.Contains(item.LocationId) && item.Id > afterId);
        var ordered = latest ? query.OrderByDescending(item => item.Id) : query.OrderBy(item => item.Id);
        var result = await ordered.Take(take).Select(item => new MonitoringEventDto(item.Id, item.LocationId,
                item.ObservedAtUtc, item.Source.ToString(), item.Code, item.RelativePath, item.PreviousRelativePath, item.Detail)).ToArrayAsync(cancellationToken);
        return latest ? result.Reverse().ToArray() : result;
    });

    /// <summary>Consulta entradas paginadas do inventario de uma localizacao da sessao.</summary>
    [HttpGet("sessions/{id:guid}/entries/{locationId:guid}")]
    [ProducesResponseType(typeof(MonitoringEntryDto[]), StatusCodes.Status200OK)]
    public Task<ActionResult<MonitoringEntryDto[]>> Entries(Guid id, Guid locationId, Guid snapshotId,
        long afterId = 0, int take = 500, CancellationToken cancellationToken = default) => Execute(async () =>
    {
        if (!Sessions.Get(id, Owner).Locations.Any(location => location.Id == locationId)) throw new KeyNotFoundException();
        var context = services.GetRequiredService<MonitoringDbContext>();
        if (!await context.Snapshots.AnyAsync(snapshot => snapshot.Id == snapshotId && snapshot.LocationId == locationId, cancellationToken))
            throw new KeyNotFoundException();
        var entries = await services.GetRequiredService<MonitoringInventoryStore>().ReadEntriesAsync(snapshotId, afterId, take, cancellationToken);
        return entries.Select(entry => new MonitoringEntryDto(entry.Id, entry.RelativePath, entry.IsDirectory, entry.LengthBytes, entry.LastWriteTimeUtc)).ToArray();
    });

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (MonitoringDisabledException) { return Problem(statusCode: 503, title: "Monitoramento desativado na API."); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException exception) { return Problem(statusCode: 400, title: exception.Message); }
        catch (InvalidOperationException exception) { return Problem(statusCode: 409, title: exception.Message); }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            logger.LogError(exception, "Falha na persistencia do monitoramento");
            return Problem(statusCode: 503, title: "Banco de monitoramento indisponivel. Nenhuma outra funcionalidade foi bloqueada.");
        }
    }

    private sealed class MonitoringDisabledException : Exception;
}
