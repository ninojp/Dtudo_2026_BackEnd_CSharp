using ApiMyAnimes.Data;
using ApiMyAnimes.Services;
using LibDtudo.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Controllers;

/// <summary>
/// Status por anime para o usuario autenticado, em lote, usado pelos icones dos cards do site.
/// O olho (<c>watch</c>) reflete os episodios vistos do usuario; a pasta (<c>folder</c>) e o ultimo resultado persistido da
/// atualizacao sob demanda (nunca calculado a cada exibicao); sem verificacao registrada, e <c>none</c> com <c>checkedAtUtc</c> nulo.
/// </summary>
/// <param name="context">Contexto do banco local.</param>
/// <param name="watchService">Servico de progresso de episodios (contagem agregada por anime).</param>
/// <param name="logger">Logger do controlador.</param>
[ApiController]
[Route("apiLocal/Anime/status")]
[Authorize]
public class AnimeStatusController(
    MyAnimesContext context,
    AnimeWatchService watchService,
    ILogger<AnimeStatusController> logger) : ControllerBase
{
    private const string SubjectClaimType = "sub";
    private const long MaxRequestBodyBytes = 16 * 1024;

    /// <summary>
    /// Retorna o status dos animes informados para o usuario do token.
    /// A identidade vem exclusivamente do claim <c>sub</c>; o corpo nunca carrega identidade.
    /// Ids invalidos (menores ou iguais a zero) e repetidos sao descartados; ids inexistentes no banco local sao omitidos da resposta.
    /// </summary>
    /// <param name="request">Corpo com a lista <c>malIds</c> (maximo de 200 itens).</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <returns>
    /// <c>200 OK</c> com <c>items</c>, <c>400 BadRequest</c> para corpo ausente, lista ausente ou acima do limite,
    /// <c>401</c> sem token valido e <c>403</c> sem permissao/escopo <c>catalog.read</c> ou sem claim <c>sub</c>.
    /// </returns>
    [HttpPost]
    [Authorize(Policy = "permission:catalog.read")]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    [ProducesResponseType(typeof(AnimeStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ObterStatus(
        [FromBody] AnimeStatusRequestDto? request,
        CancellationToken cancellationToken)
    {
        var accountId = User.FindFirst(SubjectClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(accountId)) return Forbid();

        if (request?.MalIds is null) return BadRequest("O corpo deve informar a lista malIds.");
        if (request.MalIds.Length > AnimeStatusRules.MaxMalIdsPerRequest)
            return BadRequest($"malIds aceita no maximo {AnimeStatusRules.MaxMalIdsPerRequest} itens.");

        // Resposta por usuario: nao pode ser armazenada por caches compartilhados.
        Response.Headers.CacheControl = "no-store";

        var malIds = AnimeStatusRules.NormalizeMalIds(request.MalIds);
        if (malIds.Count == 0) return Ok(new AnimeStatusResponseDto([]));

        var animes = await context.Animes
            .AsNoTracking()
            .Where(anime => malIds.Contains(anime.MalId))
            .Select(anime => new { anime.MalId, anime.Episodes, anime.Episodios })
            .ToDictionaryAsync(anime => anime.MalId, cancellationToken);

        // Uma consulta agregada (GROUP BY MalId) para todos os animes do lote: sem N+1.
        var watchedCounts = await watchService.GetWatchedCountsAsync(accountId, malIds, cancellationToken);

        // Uma consulta (PK) para todos os animes do lote: sem N+1. O resultado e o ultimo persistido pela atualizacao sob demanda.
        var folders = await context.AnimeFolderStatuses
            .AsNoTracking()
            .Where(status => malIds.Contains(status.MalId))
            .Select(status => new { status.MalId, status.State, status.VideoCount, status.ExpectedEpisodes, status.CheckedAtUtc })
            .ToDictionaryAsync(status => status.MalId, cancellationToken);

        var items = new List<AnimeStatusItemDto>(animes.Count);
        foreach (var malId in malIds)
        {
            if (!animes.TryGetValue(malId, out var anime)) continue;

            var total = AnimeStatusRules.ResolveTotalEpisodes(anime.Episodes, anime.Episodios);
            watchedCounts.TryGetValue(malId, out var watched);
            folders.TryGetValue(malId, out var stored);
            items.Add(new AnimeStatusItemDto(
                malId,
                new AnimeWatchStatusDto(AnimeStatusRules.ResolveWatchState(watched, total), watched, total),
                AnimeStatusRules.ToFolderStatus(stored?.State, stored?.VideoCount ?? 0, stored?.ExpectedEpisodes, stored?.CheckedAtUtc, total),
                null));
        }

        logger.LogDebug("Status em lote: {Requested} ids solicitados, {Returned} encontrados.", malIds.Count, items.Count);
        return Ok(new AnimeStatusResponseDto(items));
    }
}
