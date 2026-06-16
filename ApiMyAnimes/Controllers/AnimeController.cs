using ApiCSharp.Shared.Dtos;
using ApiCSharp.Shared.Models;
using ApiMyAnimes.Data;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace ApiCSharp.Controllers;

/// <summary>
/// Controlador responsável por gerenciar os registros da entidade <see cref="Anime"/> em banco local.
/// Disponibiliza endpoints de criação, leitura, atualização (total e parcial) e exclusão.
/// </summary>
/// <param name="context">Contexto do banco de dados utilizado para operações CRUD da tabela Animes.</param>
[ApiController]
[Route("apiLocal/[controller]")]
public class AnimeController(MyAnimesContext context) : ControllerBase
{
    /// <summary>
    /// Adiciona um novo anime na tabela local de animes.
    /// O <c>MalId</c> é utilizado como chave primária e deve ser único.
    /// </summary>
    /// <param name="adicionaAnimeDto">Dados necessários para criar um novo anime.</param>
    /// <returns>
    /// Retorna <c>201 Created</c> quando criado com sucesso,
    /// <c>400 BadRequest</c> para corpo inválido,
    /// ou <c>409 Conflict</c> quando já existe anime com o mesmo <c>MalId</c>.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(ObterAnimeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult AdicionarAnime([FromBody] AdicionaAnimeDto adicionaAnimeDto)
    {
        if (adicionaAnimeDto is null) return BadRequest("Corpo da requisição inválido.");

        var animeExistente = context.Animes.FirstOrDefault(a => a.MalId == adicionaAnimeDto.MalId);
        if (animeExistente is not null) return Conflict($"Anime com MalId {adicionaAnimeDto.MalId} já existe.");

        var anime = new Anime
        {
            MalId = adicionaAnimeDto.MalId,
            Titulo = adicionaAnimeDto.Titulo,
            Episodios = adicionaAnimeDto.Episodios,
            MalUrl = adicionaAnimeDto.MalUrl,
            ImagensUrlMal = adicionaAnimeDto.ImagensUrlMal,
            SubTitulos = adicionaAnimeDto.SubTitulos
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
        anime.MalUrl = atualizaAnimeDto.MalUrl;
        anime.ImagensUrlMal = atualizaAnimeDto.ImagensUrlMal;
        anime.SubTitulos = atualizaAnimeDto.SubTitulos;

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
            MalUrl = anime.MalUrl,
            ImagensUrlMal = anime.ImagensUrlMal,
            SubTitulos = anime.SubTitulos
        };

        animePatch.ApplyTo(animeParaAtualizar, ModelState);
        if (!ModelState.IsValid) return BadRequest(ModelState);

        anime.Titulo = animeParaAtualizar.Titulo;
        anime.Episodios = animeParaAtualizar.Episodios;
        anime.MalUrl = animeParaAtualizar.MalUrl;
        anime.ImagensUrlMal = animeParaAtualizar.ImagensUrlMal;
        anime.SubTitulos = animeParaAtualizar.SubTitulos;

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
            MalUrl = anime.MalUrl,
            ImagensUrlMal = anime.ImagensUrlMal,
            SubTitulos = anime.SubTitulos,
            HoraDaConsulta = DateTime.Now
        };
    }
}
