using System.Net;
using System.Net.Sockets;
using LibDtudo.Shared.Dtos.MyAnimeList;
using ApiMyAnimeList.Dtos;
using ApiMyAnimeList.Mappers;
using ApiMyAnimeList.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ApiMyAnimeList.Controllers;
/// <summary>
/// Controller responsável por fornecer endpoints compatíveis com a API MyAnimeList.
/// </summary>
/// <param name="client">Cliente responsável por se comunicar com a API MyAnimeList.</param>
/// <param name="logger">Logger para registrar informações e erros.</param>
[ApiController]
[Route("ApiMyAnimeList")]
[Authorize]
public sealed class MyAnimeListController(MyAnimeListClient client, ILogger<MyAnimeListController> logger) : ControllerBase
{
    [HttpGet("health")]
    [Authorize(Policy = "permission:health.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new { status = "ok", service = "ApiMyAnimeList" });

    [HttpGet("search")]
    [Authorize(Policy = "permission:catalog.external.read")]
    [ProducesResponseType(typeof(AnimeSearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnimeSearchResult>> Search([FromQuery] string? q, [FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { message = "O termo de busca é obrigatório." });
        if (page < 1) return BadRequest(new { message = "O número da página deve ser maior que 0." });
        const int limit = 20;
        try { return Ok(MyAnimeListMapper.MapSearch(await client.SearchAsync(q.Trim(), (page - 1) * limit, limit, cancellationToken), page, limit)); }
        catch (BrokenCircuitException) { logger.LogWarning("Circuito da MAL aberto durante pesquisa"); return UpstreamUnavailable(StatusCodes.Status503ServiceUnavailable, "MyAnimeList indisponível", "A MyAnimeList está temporariamente indisponível. Tente novamente em instantes."); }
        catch (TimeoutRejectedException) { return UpstreamUnavailable(StatusCodes.Status504GatewayTimeout, "Tempo limite da MyAnimeList", "A MyAnimeList demorou para responder. Tente novamente em instantes."); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return UpstreamUnavailable(StatusCodes.Status504GatewayTimeout, "Tempo limite da MyAnimeList", "A MyAnimeList demorou para responder. Tente novamente em instantes."); }
        catch (SocketException ex) { logger.LogWarning(ex, "Falha de rede ao pesquisar anime na MAL"); return UpstreamUnavailable(StatusCodes.Status503ServiceUnavailable, "MyAnimeList indisponível", "Não foi possível conectar à MyAnimeList. Tente novamente em instantes."); }
        catch (HttpRequestException ex) { logger.LogWarning(ex, "Falha ao pesquisar anime na MAL"); return UpstreamUnavailable(GetGatewayStatusCode(ex.StatusCode), "MyAnimeList indisponível", "Não foi possível obter resposta da MyAnimeList. Tente novamente em instantes."); }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "permission:catalog.external.read")]
    [ProducesResponseType(typeof(AnimeDetails), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnimeDetails>> Get(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest(new { message = "ID inválido." });
        try
        {
            var anime = await client.GetAnimeAsync(id, cancellationToken);
            return anime is null ? NotFound(new { message = $"Anime com ID {id} não encontrado." }) : Ok(MyAnimeListMapper.MapDetails(anime));
        }
        catch (BrokenCircuitException) { logger.LogWarning("Circuito da MAL aberto durante consulta do anime {Id}", id); return UpstreamUnavailable(StatusCodes.Status503ServiceUnavailable, "MyAnimeList indisponível", "A MyAnimeList está temporariamente indisponível. Tente novamente em instantes."); }
        catch (TimeoutRejectedException) { return UpstreamUnavailable(StatusCodes.Status504GatewayTimeout, "Tempo limite da MyAnimeList", "A MyAnimeList demorou para responder. Tente novamente em instantes."); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return UpstreamUnavailable(StatusCodes.Status504GatewayTimeout, "Tempo limite da MyAnimeList", "A MyAnimeList demorou para responder. Tente novamente em instantes."); }
        catch (SocketException ex) { logger.LogWarning(ex, "Falha de rede ao obter anime {Id} na MAL", id); return UpstreamUnavailable(StatusCodes.Status503ServiceUnavailable, "MyAnimeList indisponível", "Não foi possível conectar à MyAnimeList. Tente novamente em instantes."); }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return NotFound(new { message = $"Anime com ID {id} não encontrado." }); }
        catch (HttpRequestException ex) { logger.LogWarning(ex, "Falha ao obter anime {Id} na MAL", id); return UpstreamUnavailable(GetGatewayStatusCode(ex.StatusCode), "MyAnimeList indisponível", "Não foi possível obter resposta da MyAnimeList. Tente novamente em instantes."); }
    }

    [HttpGet("{id:int}/relations")]
    [Authorize(Policy = "permission:catalog.external.read")]
    [ProducesResponseType(typeof(List<AnimeRelationGroup>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AnimeRelationGroup>>> Relations(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest(new { message = "ID inválido." });
        try
        {
            var anime = await client.GetAnimeAsync(id, cancellationToken);
            return anime is null ? NotFound(new { message = $"Anime com ID {id} não encontrado." }) : Ok(MyAnimeListMapper.MapRelations(anime));
        }
        catch (BrokenCircuitException) { logger.LogWarning("Circuito da MAL aberto durante relações do anime {Id}", id); return UpstreamUnavailable(StatusCodes.Status503ServiceUnavailable, "MyAnimeList indisponível", "A MyAnimeList está temporariamente indisponível. Tente novamente em instantes."); }
        catch (TimeoutRejectedException) { return UpstreamUnavailable(StatusCodes.Status504GatewayTimeout, "Tempo limite da MyAnimeList", "A MyAnimeList demorou para responder. Tente novamente em instantes."); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return UpstreamUnavailable(StatusCodes.Status504GatewayTimeout, "Tempo limite da MyAnimeList", "A MyAnimeList demorou para responder. Tente novamente em instantes."); }
        catch (SocketException ex) { logger.LogWarning(ex, "Falha de rede ao obter relações do anime {Id} na MAL", id); return UpstreamUnavailable(StatusCodes.Status503ServiceUnavailable, "MyAnimeList indisponível", "Não foi possível conectar à MyAnimeList. Tente novamente em instantes."); }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return NotFound(new { message = $"Anime com ID {id} não encontrado." }); }
        catch (HttpRequestException ex) { logger.LogWarning(ex, "Falha ao obter relações do anime {Id} na MAL", id); return UpstreamUnavailable(GetGatewayStatusCode(ex.StatusCode), "MyAnimeList indisponível", "Não foi possível obter resposta da MyAnimeList. Tente novamente em instantes."); }
    }

    private ObjectResult UpstreamUnavailable(int statusCode, string title, string detail)
    {
        Response.Headers["Retry-After"] = statusCode == StatusCodes.Status503ServiceUnavailable ? "30" : "5";
        return Problem(statusCode: statusCode, title: title, detail: detail);
    }

    private static int GetGatewayStatusCode(HttpStatusCode? upstreamStatusCode)
        => upstreamStatusCode is null
            || upstreamStatusCode == HttpStatusCode.TooManyRequests
            || (int)upstreamStatusCode.Value >= 500
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status502BadGateway;
}
