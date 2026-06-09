using ApiCSharp.Models;
using ApiCSharp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiCSharp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnimeController : ControllerBase
{
    private readonly IJikanService _jikanService;
    private readonly ILogger<AnimeController> _logger;

    public AnimeController(IJikanService jikanService, ILogger<AnimeController> logger)
    {
        _jikanService = jikanService;
        _logger = logger;
    }

    /// <summary>
    /// Busca animes por nome
    /// </summary>
    /// <param name="q">Termo de busca</param>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <returns>Lista de animes encontrados</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(AnimeSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AnimeSearchResponse>> SearchAnime(
        [FromQuery] string? q,
        [FromQuery] int page = 1)
    {
        try
        {
            if (page < 1)
            {
                return BadRequest(new { message = "O número da página deve ser maior que 0." });
            }

            _logger.LogInformation("Recebida requisição de busca: Query='{Query}', Page={Page}", q, page);

            var result = await _jikanService.SearchAnimeAsync(q ?? string.Empty, page);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar busca de anime");
            return StatusCode(500, new { message = "Erro interno ao processar a requisição." });
        }
    }

    /// <summary>
    /// Busca um anime específico por ID do MyAnimeList
    /// </summary>
    /// <param name="id">ID do anime no MyAnimeList</param>
    /// <returns>Detalhes completos do anime</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AnimeData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AnimeData>> GetAnimeById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "ID inválido." });
            }

            _logger.LogInformation("Buscando anime por ID: {Id}", id);

            var anime = await _jikanService.GetAnimeByIdAsync(id);

            if (anime == null)
            {
                return NotFound(new { message = $"Anime com ID {id} não encontrado." });
            }

            return Ok(anime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar anime por ID: {Id}", id);
            return StatusCode(500, new { message = "Erro interno ao processar a requisição." });
        }
    }
}
