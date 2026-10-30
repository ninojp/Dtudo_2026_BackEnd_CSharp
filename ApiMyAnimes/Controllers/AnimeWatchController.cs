using ApiMyAnimes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiMyAnimes.Controllers;

/// <summary>
/// Progresso de episodios vistos do usuario autenticado em um anime. Dado pessoal: a conta vem exclusivamente
/// do claim <c>sub</c> do token (nunca do corpo, da rota ou da query) e cada usuario so enxerga e altera o proprio progresso.
/// </summary>
/// <param name="watchService">Servico de progresso de episodios.</param>
/// <param name="logger">Logger do controlador.</param>
[ApiController]
[Route("apiLocal/Anime/{malId:int:min(1)}/watch")]
[Authorize]
public class AnimeWatchController(AnimeWatchService watchService, ILogger<AnimeWatchController> logger) : ControllerBase
{
    private const string SubjectClaimType = "sub";
    private const long MaxRequestBodyBytes = 64 * 1024;

    /// <summary>Lista os episodios vistos pelo usuario.</summary>
    /// <param name="malId">Id do anime no MyAnimeList.</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <returns><c>200</c> com o progresso, <c>404</c> se o anime nao existe, <c>401</c>/<c>403</c> sem token valido ou sem permissao/escopo <c>personal.read</c>.</returns>
    [HttpGet]
    [Authorize(Policy = "permission:personal.read")]
    [ProducesResponseType(typeof(AnimeWatchDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Listar(int malId, CancellationToken cancellationToken) =>
        ExecutarAsync(accountId => watchService.GetAsync(accountId, malId, cancellationToken));

    /// <summary>
    /// Marca episodios como vistos (idempotente: repetir a chamada nao duplica nem altera episodios ja vistos).
    /// Todos os numeros devem ser maiores ou iguais a 1 e, se o total for conhecido, menores ou iguais ao total;
    /// um numero invalido recusa a requisicao inteira.
    /// </summary>
    /// <param name="malId">Id do anime no MyAnimeList.</param>
    /// <param name="request">Corpo com a lista <c>episodes</c>.</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <returns><c>200</c> com o progresso atualizado, <c>400</c> para corpo/episodios invalidos, <c>404</c> se o anime nao existe.</returns>
    [HttpPut("episodes")]
    [Authorize(Policy = "permission:personal.write")]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    [ProducesResponseType(typeof(AnimeWatchDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarcarEpisodios(
        int malId,
        [FromBody] AnimeWatchEpisodesRequestDto? request,
        CancellationToken cancellationToken) =>
        ExecutarAsync(accountId => watchService.MarkEpisodesAsync(accountId, malId, request?.Episodes, cancellationToken));

    /// <summary>
    /// Desmarca episodios. Sem corpo ou com lista vazia nao apaga nada (retorna o progresso atual).
    /// </summary>
    /// <param name="malId">Id do anime no MyAnimeList.</param>
    /// <param name="request">Corpo opcional com a lista <c>episodes</c>.</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <returns><c>200</c> com o progresso atualizado, <c>400</c> para numeros invalidos, <c>404</c> se o anime nao existe.</returns>
    [HttpDelete("episodes")]
    [Authorize(Policy = "permission:personal.write")]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    [ProducesResponseType(typeof(AnimeWatchDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> DesmarcarEpisodios(
        int malId,
        [FromBody] AnimeWatchEpisodesRequestDto? request,
        CancellationToken cancellationToken) =>
        ExecutarAsync(accountId => watchService.UnmarkEpisodesAsync(accountId, malId, request?.Episodes ?? [], cancellationToken));

    /// <summary>Marca todos os episodios (1..total) como vistos. Exige total de episodios conhecido.</summary>
    /// <param name="malId">Id do anime no MyAnimeList.</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <returns><c>200</c> com o progresso, <c>404</c> se o anime nao existe, <c>409</c> se o total e desconhecido.</returns>
    [HttpPut("all")]
    [Authorize(Policy = "permission:personal.write")]
    [ProducesResponseType(typeof(AnimeWatchDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> MarcarTodos(int malId, CancellationToken cancellationToken) =>
        ExecutarAsync(accountId => watchService.MarkAllAsync(accountId, malId, cancellationToken));

    /// <summary>Limpa todo o progresso do usuario neste anime.</summary>
    /// <param name="malId">Id do anime no MyAnimeList.</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <returns><c>200</c> com o progresso (vazio), <c>404</c> se o anime nao existe.</returns>
    [HttpDelete]
    [Authorize(Policy = "permission:personal.write")]
    [ProducesResponseType(typeof(AnimeWatchDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Limpar(int malId, CancellationToken cancellationToken) =>
        ExecutarAsync(accountId => watchService.ClearAsync(accountId, malId, cancellationToken));

    private async Task<IActionResult> ExecutarAsync(Func<string, Task<AnimeWatchResult>> operacao)
    {
        var accountId = User.FindFirst(SubjectClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(accountId) || accountId.Length > Data.AnimeEpisodeWatch.AccountIdMaxLength)
            return Forbid();

        // Resposta por usuario: nao pode ser armazenada por caches compartilhados.
        Response.Headers.CacheControl = "no-store";

        var result = await operacao(accountId);
        switch (result.Outcome)
        {
            case AnimeWatchOutcome.Success:
                return Ok(result.Watch);
            case AnimeWatchOutcome.AnimeNotFound:
                return NotFound();
            case AnimeWatchOutcome.TotalUnknown:
                return Conflict(result.Error);
            default:
                logger.LogDebug("Progresso recusado: {Error}", result.Error);
                return BadRequest(result.Error);
        }
    }
}
