using System.Net;
using LibDtudo.Shared.Dtos.MyAnimeList;
using ApiMyAnimeList.Dtos;
using ApiMyAnimeList.Mappers;
using ApiMyAnimeList.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiMyAnimeList.Controllers;
/// <summary>
/// Controller responsável por fornecer endpoints compatíveis com a API MyAnimeList.
/// </summary>
/// <param name="client">Cliente responsável por se comunicar com a API MyAnimeList.</param>
/// <param name="logger">Logger para registrar informações e erros.</param>
[ApiController]
[Route("ApiMyAnimeList")]
public sealed class MyAnimeListController(MyAnimeListClient client, ILogger<MyAnimeListController> logger) : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new { status = "ok", service = "ApiMyAnimeList" });

    [HttpGet("search")]
    [ProducesResponseType(typeof(AnimeSearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnimeSearchResult>> Search([FromQuery] string? q, [FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { message = "O termo de busca é obrigatório." });
        if (page < 1) return BadRequest(new { message = "O número da página deve ser maior que 0." });
        const int limit = 20;
        try { return Ok(MyAnimeListMapper.MapSearch(await client.SearchAsync(q.Trim(), (page - 1) * limit, limit, cancellationToken), page, limit)); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return StatusCode(504, new { message = "A API MyAnimeList demorou para responder." }); }
        catch (HttpRequestException ex) { logger.LogError(ex, "Erro ao pesquisar anime na MAL"); return StatusCode((int?)ex.StatusCode is >= 400 and <= 599 ? (int)ex.StatusCode.Value : 502, new { message = "Falha ao comunicar com a API MyAnimeList." }); }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AnimeDetails), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnimeDetails>> Get(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest(new { message = "ID inválido." });
        try
        {
            var anime = await client.GetAnimeAsync(id, cancellationToken);
            return anime is null ? NotFound(new { message = $"Anime com ID {id} não encontrado." }) : Ok(MyAnimeListMapper.MapDetails(anime));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return StatusCode(504, new { message = "A API MyAnimeList demorou para responder." }); }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return NotFound(new { message = $"Anime com ID {id} não encontrado." }); }
        catch (HttpRequestException ex) { logger.LogError(ex, "Erro ao obter anime {Id} na MAL", id); return StatusCode(502, new { message = "Falha ao comunicar com a API MyAnimeList." }); }
    }

    [HttpGet("{id:int}/relations")]
    [ProducesResponseType(typeof(List<AnimeRelationGroup>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AnimeRelationGroup>>> Relations(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return BadRequest(new { message = "ID inválido." });
        try
        {
            var anime = await client.GetAnimeAsync(id, cancellationToken);
            return anime is null ? NotFound(new { message = $"Anime com ID {id} não encontrado." }) : Ok(MyAnimeListMapper.MapRelations(anime));
        }
        catch (HttpRequestException ex) { logger.LogError(ex, "Erro ao obter relações do anime {Id} na MAL", id); return StatusCode(502, new { message = "Falha ao comunicar com a API MyAnimeList." }); }
    }
}
