using ApiMyAnimes.Data;
using ApiMyAnimes.Services;
using LibDtudo.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Controllers;

/// <summary>
/// Atualiza e consulta o status persistido das pastas dos animes (indicador de pasta dos cards). Somente administracao
/// (<c>catalog.write</c>): o usuario do site so le o resultado pelo endpoint de status em lote. Nenhuma resposta contem
/// caminho fisico. Estes caminhos nao sao expostos pelo gateway publico.
/// </summary>
[ApiController]
[Route("apiLocal/MyAnime")]
[Authorize(Policy = "permission:catalog.write")]
public sealed class AnimeFolderStatusController(
    MyAnimesContext context,
    AnimeFolderRefreshService refreshService,
    AnimeFolderRefreshRunner runner,
    IAnimeFolderScanClient scanClient) : ControllerBase
{
    private const string SubjectClaimType = "sub";

    /// <summary>
    /// Inicia, em segundo plano, a atualizacao do status de pasta dos animes de uma colecao. A execucao e consultavel por
    /// <c>GET folder-status/runs/{id}</c>.
    /// </summary>
    /// <param name="myAnimeId">Id da colecao (MyAnime).</param>
    /// <param name="cancellationToken">Cancelamento da requisicao (nao cancela a execucao ja iniciada).</param>
    [HttpPost("{myAnimeId:int}/folder-status/refresh")]
    [ProducesResponseType(typeof(AnimeFolderRefreshRunDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AnimeFolderRefreshRunDto>> RefreshCollection(int myAnimeId, CancellationToken cancellationToken)
    {
        if (myAnimeId <= 0 || !await context.MyAnimes.AnyAsync(item => item.Id == myAnimeId, cancellationToken))
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Colecao nao encontrada.");
        return await StartAsync(myAnimeId, cancellationToken);
    }

    /// <summary>Inicia, em segundo plano, a atualizacao do status de pasta de todas as colecoes.</summary>
    /// <param name="cancellationToken">Cancelamento da requisicao (nao cancela a execucao ja iniciada).</param>
    [HttpPost("folder-status/refresh")]
    [ProducesResponseType(typeof(AnimeFolderRefreshRunDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<ActionResult<AnimeFolderRefreshRunDto>> RefreshAll(CancellationToken cancellationToken) =>
        StartAsync(null, cancellationToken);

    /// <summary>Lista as execucoes mais recentes (sem o relatorio por anime).</summary>
    /// <param name="take">Quantidade (1 a 50).</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    [HttpGet("folder-status/runs")]
    [ProducesResponseType(typeof(IReadOnlyList<AnimeFolderRefreshRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnimeFolderRefreshRunDto>>> ListRuns(
        [FromQuery] int take = 20, CancellationToken cancellationToken = default) =>
        Ok(await refreshService.ListRunsAsync(take, cancellationToken));

    /// <summary>Consulta o progresso de uma execucao e, opcionalmente, o relatorio por anime (paginado por <c>afterId</c>).</summary>
    /// <param name="runId">Identificador devolvido ao iniciar.</param>
    /// <param name="includeItems">Inclui o relatorio por anime.</param>
    /// <param name="afterId">Cursor: devolve itens com id maior que este.</param>
    /// <param name="take">Itens por pagina (1 a 500).</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    [HttpGet("folder-status/runs/{runId:guid}")]
    [ProducesResponseType(typeof(AnimeFolderRefreshRunDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnimeFolderRefreshRunDto>> GetRun(
        Guid runId,
        [FromQuery] bool includeItems = false,
        [FromQuery] long afterId = 0,
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
    {
        var run = await refreshService.GetRunAsync(runId, runner.IsActive(runId), includeItems, afterId, take, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    /// <summary>Pede o cancelamento da execucao em andamento; os animes ja resolvidos permanecem gravados.</summary>
    /// <param name="runId">Identificador da execucao.</param>
    [HttpPost("folder-status/runs/{runId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult CancelRun(Guid runId) =>
        runner.Cancel(runId)
            ? Accepted()
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Nenhuma execucao em andamento com este identificador.");

    private async Task<ActionResult<AnimeFolderRefreshRunDto>> StartAsync(int? myAnimeId, CancellationToken cancellationToken)
    {
        if (!scanClient.IsConfigured)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "ApiFileStorage nao configurada para a atualizacao de pastas.");
        var requestedBy = User.FindFirst(SubjectClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(requestedBy)) return Forbid();
        try
        {
            var run = await runner.StartAsync(myAnimeId, requestedBy, cancellationToken);
            return Accepted(Url.Action(nameof(GetRun), new { runId = run.Id }), run);
        }
        catch (AnimeFolderRefreshBusyException exception)
        {
            var problem = Problem(statusCode: StatusCodes.Status409Conflict, title: exception.Message);
            if (exception.ActiveRunId != Guid.Empty) Response.Headers.Location = Url.Action(nameof(GetRun), new { runId = exception.ActiveRunId });
            return problem;
        }
    }
}
