using ApiJikan.Dtos.Responses;
using ApiJikan.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiJikan.Controllers;

/// <summary>
/// Controlador responsável por expor os endpoints da API local que interage com a API externa Jikan para buscar informações sobre animes.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ApiJikanController(
    ServiceBuscarPorID ServiceBuscarPorID,
    ServiceBuscarPorNome ServiceBuscarPorNome,
    ServiceBuscarAnimeRelacionadoPorID ServiceBuscarAnimeRelacionadoPorID,
    ILogger<ApiJikanController> logger) : ControllerBase
{
    private readonly ServiceBuscarPorID _serviceBuscarPorID = ServiceBuscarPorID;
    private readonly ServiceBuscarPorNome _serviceBuscarPorNome = ServiceBuscarPorNome;
    private readonly ServiceBuscarAnimeRelacionadoPorID _serviceBuscarAnimeRelacionadoPorID = ServiceBuscarAnimeRelacionadoPorID;
    private readonly ILogger<ApiJikanController> _logger = logger;
    //========================================================================================
    /// <summary>
    /// End-Point da minha Api Local que faz uma busca na Api externa Jikan, por nome do anime.
    /// </summary>
    /// <param name="q">Termo de busca</param>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <returns>Lista de animes encontrados</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(BuscarAnimePorNomeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<BuscarAnimePorNomeResponseDto>> BuscarAnimePorNome(
        [FromQuery] string? q,
        [FromQuery] int page = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { message = "O termo de busca é obrigatório." });
            if (page < 1) return BadRequest(new { message = "O número da página deve ser maior que 0." });
            _logger.LogInformation("Recebida requisição de busca: Query='{Query}', Page={Page}", q, page);
            var result = await _serviceBuscarPorNome.JikanBuscarPorNomeAsync(q, page);
            return Ok(result);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout ao processar busca de anime na API externa");
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                new { message = "A API externa demorou para responder. Tente novamente em instantes." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar busca de anime");
            return StatusCode(500, new { message = "Erro interno ao processar a requisição." });
        }
    }
    //=================================================
    /// <summary>
    /// Busca um anime específico por ID do MyAnimeList
    /// </summary>
    /// <param name="id">ID do anime no MyAnimeList</param>
    /// <returns>Detalhes completos do anime</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BuscarAnimePorIdResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BuscarAnimePorIdResponseDto>> BuscarAnimePorId(int id)
    {
        try
        {
            if (id <= 0) return BadRequest(new { message = "ID inválido." });
            _logger.LogInformation("Buscando anime por ID: {Id}", id);
            var anime = await _serviceBuscarPorID.JikanBuscarPorIDAsync(id);
            if (anime == null) return NotFound(new { message = $"Anime com ID {id} não encontrado." });
            return Ok(anime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar anime por ID: {Id}", id);
            return StatusCode(500, new { message = "Erro interno ao processar a requisição." });
        }
    }
    //=================================================
    /// <summary>
    /// Busca os animes relacionados a um anime específico pelo ID do MyAnimeList.
    /// Utiliza o endpoint dedicado /anime/{id}/relations da Jikan e retorna as imagens hidratadas de cada entrada.
    /// </summary>
    /// <param name="id">ID do anime no MyAnimeList</param>
    /// <returns>Lista de relações do anime com imagens hidratadas</returns>
    [HttpGet("{id:int}/relations")]
    [ProducesResponseType(typeof(List<AnimeRelationGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<AnimeRelationGroupDto>>> BuscarAnimeRelacionadoPorId(int id)
    {
        try
        {
            if (id <= 0) return BadRequest(new { message = "ID inválido." });
            _logger.LogInformation("Buscando relações do anime por ID: {Id}", id);
            var relations = await _serviceBuscarAnimeRelacionadoPorID.JikanBuscarRelacoesPorIDAsync(id);
            return Ok(relations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar relações do anime por ID: {Id}", id);
            return StatusCode(500, new { message = "Erro interno ao processar a requisição." });
        }
    }
}
