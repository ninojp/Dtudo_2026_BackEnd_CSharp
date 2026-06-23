using ApiCSharp.Shared.Dtos;
using ApiCSharp.Shared.Models;
using ApiMyAnimes.Data;
using ApiMyAnimes.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ApiCSharp.Controllers;

/// <summary>
/// Controlador responsável por gerenciar os registros da entidade <see cref="Anime"/> em banco local.
/// Disponibiliza endpoints de criação, leitura, atualização (total e parcial) e exclusão.
/// </summary>
/// <param name="context">Contexto do banco de dados utilizado para operações CRUD da tabela Animes.</param>
[ApiController]
[Route("apiLocal/[controller]")]
public class AnimeController(
    MyAnimesContext context,
    ApiJikanClient apiJikanClient,
    ILogger<AnimeController> logger) : ControllerBase
{
    private readonly ApiJikanClient _apiJikanClient = apiJikanClient;
    private readonly ILogger<AnimeController> _logger = logger;
    /// <summary>
    /// Adiciona um novo anime na tabela local de animes.
    /// Pode criar diretamente via corpo da requisição ou importar da ApiJikan via <c>jikanId</c> na query string.
    /// O <c>MalId</c> é utilizado como chave primária e deve ser único.
    /// </summary>
    /// <param name="adicionaAnimeDto">Dados necessários para criação direta no banco local.</param>
    /// <param name="jikanId">ID opcional para importar dados da ApiJikan e persistir localmente.</param>
    /// <returns>
    /// Retorna <c>201 Created</c> quando criado com sucesso,
    /// <c>400 BadRequest</c> para entrada inválida,
    /// <c>404 NotFound</c> quando o anime não existe na ApiJikan,
    /// <c>409 Conflict</c> quando já existe anime com o mesmo <c>MalId</c>,
    /// ou <c>502 BadGateway</c> quando a ApiJikan está indisponível.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(ObterAnimeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> AdicionarAnime([FromBody] AdicionaAnimeDto? adicionaAnimeDto, [FromQuery] int? jikanId = null)
    {
        if (jikanId.HasValue)
        {
            if (jikanId.Value <= 0) return BadRequest("jikanId deve ser um número positivo.");

            var animeExistentePorImportacao = context.Animes.FirstOrDefault(a => a.MalId == jikanId.Value);
            if (animeExistentePorImportacao is not null) return Conflict($"Anime com MalId {jikanId.Value} já existe.");

            AnimeImportData? animeImportado;
            try
            {
                animeImportado = await _apiJikanClient.ObterAnimePorIdAsync(jikanId.Value, HttpContext.RequestAborted);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound($"Anime com MalId {jikanId.Value} não encontrado na ApiJikan.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha ao consultar ApiJikan para o MalId {MalId}", jikanId.Value);
                return StatusCode(StatusCodes.Status502BadGateway, "Falha ao consultar ApiJikan.");
            }

            if (animeImportado is null) return NotFound($"Anime com MalId {jikanId.Value} não encontrado na ApiJikan.");

            var animeImportacao = new Anime
            {
                MalId = animeImportado.MalId,
                Titulo = animeImportado.Titulo,
                Episodios = animeImportado.Episodios,
                MyAnimeID = adicionaAnimeDto?.MyAnimeID ?? 0,
                MalUrl = animeImportado.MalUrl,
                ImagensUrlMal = animeImportado.ImagensUrlMal,
                SubTitulos = animeImportado.SubTitulos,
                Trailer = animeImportado.Trailer,
                Approved = animeImportado.Approved,
                Title = animeImportado.Title,
                TitleEnglish = animeImportado.TitleEnglish,
                TitleJapanese = animeImportado.TitleJapanese,
                TitleSynonyms = animeImportado.TitleSynonyms,
                Type = animeImportado.Type,
                Source = animeImportado.Source,
                Episodes = animeImportado.Episodes,
                Status = animeImportado.Status,
                Airing = animeImportado.Airing,
                Aired = animeImportado.Aired,
                Duration = animeImportado.Duration,
                Rating = animeImportado.Rating,
                Score = animeImportado.Score,
                ScoredBy = animeImportado.ScoredBy,
                Rank = animeImportado.Rank,
                Popularity = animeImportado.Popularity,
                Members = animeImportado.Members,
                Favorites = animeImportado.Favorites,
                Synopsis = animeImportado.Synopsis,
                Background = animeImportado.Background,
                Season = animeImportado.Season,
                Year = animeImportado.Year,
                Producers = animeImportado.Producers,
                Licensors = animeImportado.Licensors,
                Studios = animeImportado.Studios,
                Genres = animeImportado.Genres,
                ExplicitGenres = animeImportado.ExplicitGenres,
                Themes = animeImportado.Themes,
                Demographics = animeImportado.Demographics
            };

            context.Animes.Add(animeImportacao);
            context.SaveChanges();

            return CreatedAtAction(nameof(ObterAnimePorId), new { id = animeImportacao.MalId }, ParaObterAnimeDto(animeImportacao));
        }

        if (adicionaAnimeDto is null) return BadRequest("Corpo da requisição inválido.");

        var animeExistente = context.Animes.FirstOrDefault(a => a.MalId == adicionaAnimeDto.MalId);
        if (animeExistente is not null) return Conflict($"Anime com MalId {adicionaAnimeDto.MalId} já existe.");

        var anime = new Anime
        {
            MalId = adicionaAnimeDto.MalId,
            Titulo = adicionaAnimeDto.Titulo,
            Episodios = adicionaAnimeDto.Episodios,
            MyAnimeID = adicionaAnimeDto.MyAnimeID,
            MalUrl = adicionaAnimeDto.MalUrl,
            ImagensUrlMal = adicionaAnimeDto.ImagensUrlMal,
            SubTitulos = adicionaAnimeDto.SubTitulos,
            Trailer = adicionaAnimeDto.Trailer,
            Approved = adicionaAnimeDto.Approved,
            Title = adicionaAnimeDto.Title,
            TitleEnglish = adicionaAnimeDto.TitleEnglish,
            TitleJapanese = adicionaAnimeDto.TitleJapanese,
            TitleSynonyms = adicionaAnimeDto.TitleSynonyms,
            Type = adicionaAnimeDto.Type,
            Source = adicionaAnimeDto.Source,
            Episodes = adicionaAnimeDto.Episodes,
            Status = adicionaAnimeDto.Status,
            Airing = adicionaAnimeDto.Airing,
            Aired = adicionaAnimeDto.Aired,
            Duration = adicionaAnimeDto.Duration,
            Rating = adicionaAnimeDto.Rating,
            Score = adicionaAnimeDto.Score,
            ScoredBy = adicionaAnimeDto.ScoredBy,
            Rank = adicionaAnimeDto.Rank,
            Popularity = adicionaAnimeDto.Popularity,
            Members = adicionaAnimeDto.Members,
            Favorites = adicionaAnimeDto.Favorites,
            Synopsis = adicionaAnimeDto.Synopsis,
            Background = adicionaAnimeDto.Background,
            Season = adicionaAnimeDto.Season,
            Year = adicionaAnimeDto.Year,
            Producers = adicionaAnimeDto.Producers,
            Licensors = adicionaAnimeDto.Licensors,
            Studios = adicionaAnimeDto.Studios,
            Genres = adicionaAnimeDto.Genres,
            ExplicitGenres = adicionaAnimeDto.ExplicitGenres,
            Themes = adicionaAnimeDto.Themes,
            Demographics = adicionaAnimeDto.Demographics
        };
        context.Animes.Add(anime);
        context.SaveChanges();
        return CreatedAtAction(nameof(ObterAnimePorId), new { id = anime.MalId }, ParaObterAnimeDto(anime));
    }
    //================================================================
    /// <summary>
    /// Obtém uma lista paginada de animes cadastrados no banco local.
    /// </summary>
    /// <param name="skip">Quantidade de registros a ignorar (offset).</param>
    /// <param name="take">Quantidade máxima de registros retornados.</param>
    /// <returns>
    /// Retorna <c>200 OK</c> com a lista paginada,
    /// ou <c>400 BadRequest</c> quando parâmetros de paginação são inválidos.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ObterAnimeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<ObterAnimeDto>> ObterAnimes([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        if (skip < 0 || take <= 0) return BadRequest("Parâmetros de paginação inválidos.");

        var animesDto = context.Animes
            .OrderBy(a => a.MalId)
            .Skip(skip)
            .Take(take)
            .Select(a => ParaObterAnimeDto(a))
            .ToList();

        return Ok(animesDto);
    }
    //==============================================
    /// <summary>
    /// Obtém um anime específico pelo <c>MalId</c>.
    /// </summary>
    /// <param name="id">Identificador do anime no MyAnimeList (MalId).</param>
    /// <returns>
    /// Retorna <c>200 OK</c> com o anime,
    /// <c>400 BadRequest</c> para ID inválido,
    /// ou <c>404 NotFound</c> quando o registro não existe.
    /// </returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ObterAnimeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ObterAnimeDto> ObterAnimePorId(int id)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");

        var anime = context.Animes.FirstOrDefault(a => a.MalId == id);
        if (anime is null) return NotFound($"Anime com MalId {id} não encontrado.");

        return Ok(ParaObterAnimeDto(anime));
    }
    //=====================================================================
    /// <summary>
    /// Atualiza completamente um anime existente com base no <c>MalId</c>.
    /// </summary>
    /// <param name="id">MalId do anime que será atualizado.</param>
    /// <param name="atualizaAnimeDto">Objeto com os novos dados do anime.</param>
    /// <returns>
    /// Retorna <c>204 NoContent</c> quando atualizado com sucesso,
    /// <c>400 BadRequest</c> para entrada inválida,
    /// ou <c>404 NotFound</c> quando o anime não existe.
    /// </returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult AtualizarAnime(int id, [FromBody] AtualizaAnimeDto atualizaAnimeDto)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");
        if (atualizaAnimeDto is null) return BadRequest("Corpo da requisição inválido.");

        var anime = context.Animes.FirstOrDefault(a => a.MalId == id);
        if (anime is null) return NotFound($"Anime com MalId {id} não encontrado.");

        anime.Titulo = atualizaAnimeDto.Titulo;
        anime.Episodios = atualizaAnimeDto.Episodios;
        anime.MyAnimeID = atualizaAnimeDto.MyAnimeID;
        anime.MalUrl = atualizaAnimeDto.MalUrl;
        anime.ImagensUrlMal = atualizaAnimeDto.ImagensUrlMal;
        anime.SubTitulos = atualizaAnimeDto.SubTitulos;
        anime.Trailer = atualizaAnimeDto.Trailer;
        anime.Approved = atualizaAnimeDto.Approved;
        anime.Title = atualizaAnimeDto.Title;
        anime.TitleEnglish = atualizaAnimeDto.TitleEnglish;
        anime.TitleJapanese = atualizaAnimeDto.TitleJapanese;
        anime.TitleSynonyms = atualizaAnimeDto.TitleSynonyms;
        anime.Type = atualizaAnimeDto.Type;
        anime.Source = atualizaAnimeDto.Source;
        anime.Episodes = atualizaAnimeDto.Episodes;
        anime.Status = atualizaAnimeDto.Status;
        anime.Airing = atualizaAnimeDto.Airing;
        anime.Aired = atualizaAnimeDto.Aired;
        anime.Duration = atualizaAnimeDto.Duration;
        anime.Rating = atualizaAnimeDto.Rating;
        anime.Score = atualizaAnimeDto.Score;
        anime.ScoredBy = atualizaAnimeDto.ScoredBy;
        anime.Rank = atualizaAnimeDto.Rank;
        anime.Popularity = atualizaAnimeDto.Popularity;
        anime.Members = atualizaAnimeDto.Members;
        anime.Favorites = atualizaAnimeDto.Favorites;
        anime.Synopsis = atualizaAnimeDto.Synopsis;
        anime.Background = atualizaAnimeDto.Background;
        anime.Season = atualizaAnimeDto.Season;
        anime.Year = atualizaAnimeDto.Year;
        anime.Producers = atualizaAnimeDto.Producers;
        anime.Licensors = atualizaAnimeDto.Licensors;
        anime.Studios = atualizaAnimeDto.Studios;
        anime.Genres = atualizaAnimeDto.Genres;
        anime.ExplicitGenres = atualizaAnimeDto.ExplicitGenres;
        anime.Themes = atualizaAnimeDto.Themes;
        anime.Demographics = atualizaAnimeDto.Demographics;

        context.SaveChanges();
        return NoContent();
    }
    //=============================================================
    /// <summary>
    /// Atualiza parcialmente um anime existente usando JSON Patch.
    /// O <c>MalId</c> é preservado e não é alterado pela operação.
    /// </summary>
    /// <param name="id">MalId do anime que será atualizado parcialmente.</param>
    /// <param name="animePatch">Documento JSON Patch contendo as operações de alteração.</param>
    /// <returns>
    /// Retorna <c>204 NoContent</c> quando atualizado com sucesso,
    /// <c>400 BadRequest</c> para entrada inválida,
    /// ou <c>404 NotFound</c> quando o anime não existe.
    /// </returns>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult AtualizarAnimeParcial(int id, [FromBody] JsonPatchDocument<AtualizaAnimeDto> animePatch)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");
        if (animePatch is null) return BadRequest("Corpo da requisição PATCH inválido.");

        var anime = context.Animes.FirstOrDefault(a => a.MalId == id);
        if (anime is null) return NotFound($"Anime com MalId {id} não encontrado.");

        var animeParaAtualizar = new AtualizaAnimeDto
        {
            Titulo = anime.Titulo,
            Episodios = anime.Episodios,
            MyAnimeID = anime.MyAnimeID,
            MalUrl = anime.MalUrl,
            ImagensUrlMal = anime.ImagensUrlMal,
            SubTitulos = anime.SubTitulos,
            Trailer = anime.Trailer,
            Approved = anime.Approved,
            Title = anime.Title,
            TitleEnglish = anime.TitleEnglish,
            TitleJapanese = anime.TitleJapanese,
            TitleSynonyms = anime.TitleSynonyms,
            Type = anime.Type,
            Source = anime.Source,
            Episodes = anime.Episodes,
            Status = anime.Status,
            Airing = anime.Airing,
            Aired = anime.Aired,
            Duration = anime.Duration,
            Rating = anime.Rating,
            Score = anime.Score,
            ScoredBy = anime.ScoredBy,
            Rank = anime.Rank,
            Popularity = anime.Popularity,
            Members = anime.Members,
            Favorites = anime.Favorites,
            Synopsis = anime.Synopsis,
            Background = anime.Background,
            Season = anime.Season,
            Year = anime.Year,
            Producers = anime.Producers,
            Licensors = anime.Licensors,
            Studios = anime.Studios,
            Genres = anime.Genres,
            ExplicitGenres = anime.ExplicitGenres,
            Themes = anime.Themes,
            Demographics = anime.Demographics
        };

        animePatch.ApplyTo(animeParaAtualizar, ModelState);
        if (!ModelState.IsValid) return BadRequest(ModelState);

        anime.Titulo = animeParaAtualizar.Titulo;
        anime.Episodios = animeParaAtualizar.Episodios;
        anime.MyAnimeID = animeParaAtualizar.MyAnimeID;
        anime.MalUrl = animeParaAtualizar.MalUrl;
        anime.ImagensUrlMal = animeParaAtualizar.ImagensUrlMal;
        anime.SubTitulos = animeParaAtualizar.SubTitulos;
        anime.Trailer = animeParaAtualizar.Trailer;
        anime.Approved = animeParaAtualizar.Approved;
        anime.Title = animeParaAtualizar.Title;
        anime.TitleEnglish = animeParaAtualizar.TitleEnglish;
        anime.TitleJapanese = animeParaAtualizar.TitleJapanese;
        anime.TitleSynonyms = animeParaAtualizar.TitleSynonyms;
        anime.Type = animeParaAtualizar.Type;
        anime.Source = animeParaAtualizar.Source;
        anime.Episodes = animeParaAtualizar.Episodes;
        anime.Status = animeParaAtualizar.Status;
        anime.Airing = animeParaAtualizar.Airing;
        anime.Aired = animeParaAtualizar.Aired;
        anime.Duration = animeParaAtualizar.Duration;
        anime.Rating = animeParaAtualizar.Rating;
        anime.Score = animeParaAtualizar.Score;
        anime.ScoredBy = animeParaAtualizar.ScoredBy;
        anime.Rank = animeParaAtualizar.Rank;
        anime.Popularity = animeParaAtualizar.Popularity;
        anime.Members = animeParaAtualizar.Members;
        anime.Favorites = animeParaAtualizar.Favorites;
        anime.Synopsis = animeParaAtualizar.Synopsis;
        anime.Background = animeParaAtualizar.Background;
        anime.Season = animeParaAtualizar.Season;
        anime.Year = animeParaAtualizar.Year;
        anime.Producers = animeParaAtualizar.Producers;
        anime.Licensors = animeParaAtualizar.Licensors;
        anime.Studios = animeParaAtualizar.Studios;
        anime.Genres = animeParaAtualizar.Genres;
        anime.ExplicitGenres = animeParaAtualizar.ExplicitGenres;
        anime.Themes = animeParaAtualizar.Themes;
        anime.Demographics = animeParaAtualizar.Demographics;

        context.SaveChanges();
        return NoContent();
    }

    /*
     * Exemplo de PATCH para atualizar apenas o título:
     * [
     *   {
     *     "op": "replace",
     *     "path": "/Titulo",
     *     "value": "Novo Título"
     *   }
     * ]
     */

    /// <summary>
    /// Remove um anime da base local com base no <c>MalId</c> informado.
    /// </summary>
    /// <param name="id">MalId do anime a ser removido.</param>
    /// <returns>
    /// Retorna <c>204 NoContent</c> quando removido com sucesso,
    /// <c>400 BadRequest</c> para ID inválido,
    /// ou <c>404 NotFound</c> quando o anime não existe.
    /// </returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeletarAnime(int id)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");

        var anime = context.Animes.FirstOrDefault(a => a.MalId == id);
        if (anime is null) return NotFound($"Anime com MalId {id} não encontrado.");

        context.Animes.Remove(anime);
        context.SaveChanges();

        return NoContent();
    }

    private static ObterAnimeDto ParaObterAnimeDto(Anime anime)
    {
        return new ObterAnimeDto
        {
            MalId = anime.MalId,
            Titulo = anime.Titulo,
            Episodios = anime.Episodios,
            MyAnimeID = anime.MyAnimeID,
            MalUrl = anime.MalUrl,
            ImagensUrlMal = anime.ImagensUrlMal,
            SubTitulos = anime.SubTitulos,
            Trailer = anime.Trailer,
            Approved = anime.Approved,
            Title = anime.Title,
            TitleEnglish = anime.TitleEnglish,
            TitleJapanese = anime.TitleJapanese,
            TitleSynonyms = anime.TitleSynonyms,
            Type = anime.Type,
            Source = anime.Source,
            Episodes = anime.Episodes,
            Status = anime.Status,
            Airing = anime.Airing,
            Aired = anime.Aired,
            Duration = anime.Duration,
            Rating = anime.Rating,
            Score = anime.Score,
            ScoredBy = anime.ScoredBy,
            Rank = anime.Rank,
            Popularity = anime.Popularity,
            Members = anime.Members,
            Favorites = anime.Favorites,
            Synopsis = anime.Synopsis,
            Background = anime.Background,
            Season = anime.Season,
            Year = anime.Year,
            Producers = anime.Producers,
            Licensors = anime.Licensors,
            Studios = anime.Studios,
            Genres = anime.Genres,
            ExplicitGenres = anime.ExplicitGenres,
            Themes = anime.Themes,
            Demographics = anime.Demographics,
            HoraDaConsulta = DateTime.Now
        };
    }
}
