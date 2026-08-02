using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace ApiMyAnimes.Controllers;

/// <summary>
/// Controlador responsável por gerenciar as coleções <see cref="MyAnime"/>.
/// Disponibiliza operações de criação, consulta, atualização e remoção.
/// </summary>
/// <param name="context">Contexto do banco para operações CRUD na tabela MyAnimes.</param>
[ApiController]
[Route("apiLocal/[controller]")]
public class MyAnimeController(MyAnimesContext context) : ControllerBase
{
    /// <summary>
    /// Adiciona uma nova coleção MyAnime.
    /// </summary>
    /// <param name="adicionaMyAnimeDto">Dados necessários para criação da coleção.</param>
    /// <returns>
    /// Retorna <c>201 Created</c> quando a coleção é criada com sucesso
    /// ou <c>400 BadRequest</c> quando o corpo da requisição é inválido.
    /// </returns>
    [HttpPost]
    [ProducesResponseType(typeof(ObterMyAnimeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult AdicionarMyAnime([FromBody] AdicionaMyAnimeDto adicionaMyAnimeDto)
    {
        if (adicionaMyAnimeDto is null) return BadRequest("Corpo da requisição inválido.");

        var tituloNormalizado = adicionaMyAnimeDto.Titulo?.Trim() ?? string.Empty;
        var myAnimeExistente = context.MyAnimes.FirstOrDefault(a =>
            a.Titulo.Trim().ToLower() == tituloNormalizado.ToLower());

        if (myAnimeExistente is not null)
            return Conflict($"MyAnime '{tituloNormalizado}' já existe.");

        var myAnime = new MyAnime
        {
            Titulo = tituloNormalizado,
            AnimesMalId = adicionaMyAnimeDto.AnimesMalId
        };
        context.MyAnimes.Add(myAnime);
        context.SaveChanges();
        Console.WriteLine($"Coleção 'MyAnimes' adicionada: {myAnime.Titulo}");
        return CreatedAtAction(nameof(ObterMyAnimePorId), new { id = myAnime.Id }, ParaObterMyAnimeDto(myAnime));
    }
    //======================================================================================
    /// <summary>
    /// Obtém uma lista paginada de coleções MyAnime.
    /// </summary>
    /// <param name="skip">Quantidade de registros a ignorar.</param>
    /// <param name="take">Quantidade máxima de registros retornados.</param>
    /// <returns>
    /// Retorna <c>200 OK</c> com a lista paginada
    /// ou <c>400 BadRequest</c> para parâmetros inválidos.
    /// </returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ObterMyAnimeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<ObterMyAnimeDto>> ObterMyAnimes([FromQuery] int skip = 0, [FromQuery] int take = 5)
    {
        if (skip < 0 || take <= 0) return BadRequest("Parâmetros de paginação inválidos.");

        var myAnimesDto = context.MyAnimes
            .OrderBy(a => a.Id)
            .Skip(skip)
            .Take(take)
            .Select(ParaObterMyAnimeDto)
            .ToList();

        return Ok(myAnimesDto);
    }
    //====================================================================
    /// <summary>
    /// Obtém uma coleção MyAnime pelo ID.
    /// </summary>
    /// <param name="id">ID da coleção.</param>
    /// <returns>
    /// Retorna <c>200 OK</c> com os dados da coleção,
    /// <c>400 BadRequest</c> para ID inválido,
    /// ou <c>404 NotFound</c> quando não encontrada.
    /// </returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ObterMyAnimeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ObterMyAnimeDto> ObterMyAnimePorId(int id)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");
        var myAnime = context.MyAnimes.FirstOrDefault(a => a.Id == id);
        if (myAnime is null) return NotFound($"Coleção com ID {id} não encontrada.");

        Console.WriteLine($"Coleção encontrada: {myAnime.Titulo}");
        return Ok(ParaObterMyAnimeDto(myAnime));
    }
    //===========================================================
    /// <summary>
    /// Atualiza completamente uma coleção MyAnime existente.
    /// </summary>
    /// <param name="id">ID da coleção a ser atualizada.</param>
    /// <param name="atualizaMyAnimeDto">Novos valores da coleção.</param>
    /// <returns>
    /// Retorna <c>204 NoContent</c> quando atualizada com sucesso,
    /// <c>400 BadRequest</c> para entrada inválida,
    /// ou <c>404 NotFound</c> quando não encontrada.
    /// </returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult AtualizarMyAnime(int id, [FromBody] AtualizaMyAnimeDto atualizaMyAnimeDto)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");
        if (atualizaMyAnimeDto is null) return BadRequest("Corpo da requisição inválido.");
        var myAnime = context.MyAnimes.FirstOrDefault(a => a.Id == id);
        if (myAnime is null) return NotFound($"Coleção com ID {id} não encontrada.");
        var tituloNormalizado = atualizaMyAnimeDto.Titulo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tituloNormalizado))
            return BadRequest("O titulo e obrigatorio.");

        var myAnimeComMesmoTitulo = context.MyAnimes.FirstOrDefault(a =>
            a.Id != id && a.Titulo.Trim().ToLower() == tituloNormalizado.ToLower());
        if (myAnimeComMesmoTitulo is not null)
            return Conflict($"MyAnime '{tituloNormalizado}' ja existe.");

        myAnime.Titulo = tituloNormalizado;
        myAnime.AnimesMalId = atualizaMyAnimeDto.AnimesMalId
            .Distinct()
            .Where(malId => malId > 0)
            .ToList();
        context.SaveChanges();
        Console.WriteLine($"Coleção atualizada: {myAnime.Titulo}");
        return NoContent();
    }
    //===========================================================
    /// <summary>
    /// Atualiza parcialmente uma coleção MyAnime usando JSON Patch.
    /// </summary>
    /// <param name="id">ID da coleção a ser atualizada parcialmente.</param>
    /// <param name="myAnimeToAtualiza">Documento de operações JSON Patch aplicado sobre <see cref="AtualizaMyAnimeDto"/>.</param>
    /// <returns>
    /// Retorna <c>204 NoContent</c> quando atualizada com sucesso,
    /// <c>400 BadRequest</c> para entrada inválida,
    /// ou <c>404 NotFound</c> quando não encontrada.
    /// </returns>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult AtualizarMyAnimeParcial(int id, [FromBody] JsonPatchDocument<AtualizaMyAnimeDto> myAnimeToAtualiza)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");
        if (myAnimeToAtualiza is null) return BadRequest("Corpo da requisição PATCH inválido.");
        var myAnime = context.MyAnimes.FirstOrDefault(a => a.Id == id);
        if (myAnime is null) return NotFound($"Coleção com ID {id} não encontrada.");
        var myAnimeParaAtualizar = new AtualizaMyAnimeDto
        {
            Titulo = myAnime.Titulo,
            AnimesMalId = myAnime.AnimesMalId
        };
        myAnimeToAtualiza.ApplyTo(myAnimeParaAtualizar, ModelState);
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var tituloNormalizado = myAnimeParaAtualizar.Titulo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tituloNormalizado))
            return BadRequest("O titulo e obrigatorio.");

        var myAnimeComMesmoTitulo = context.MyAnimes.FirstOrDefault(a =>
            a.Id != id && a.Titulo.Trim().ToLower() == tituloNormalizado.ToLower());
        if (myAnimeComMesmoTitulo is not null)
            return Conflict($"MyAnime '{tituloNormalizado}' ja existe.");

        myAnime.Titulo = tituloNormalizado;
        myAnime.AnimesMalId = myAnimeParaAtualizar.AnimesMalId
            .Distinct()
            .Where(malId => malId > 0)
            .ToList();
        context.SaveChanges();
        Console.WriteLine($"Coleção MyAnime atualizada: {myAnime.Titulo}");
        return NoContent();
    }
    /*
     * Exemplo de PATCH para atualizar apenas o título:
     * [
     *   {
     *     "op": "replace",
     *     "path": "/Titulo",
     *     "value": "NovoTitulo"
     *   }
     * ]
     */
    //===========================================================
    /// <summary>
    /// Remove uma coleção MyAnime pelo ID.
    /// </summary>
    /// <param name="id">ID da coleção a ser removida.</param>
    /// <returns>
    /// Retorna <c>204 NoContent</c> quando removida com sucesso,
    /// <c>400 BadRequest</c> para ID inválido,
    /// ou <c>404 NotFound</c> quando não encontrada.
    /// </returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult DeletarMyAnime(int id)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");

        var myAnime = context.MyAnimes.FirstOrDefault(a => a.Id == id);
        if (myAnime is null) return NotFound($"Coleção MyAnimes com ID {id} não encontrada.");

        context.MyAnimes.Remove(myAnime);
        context.SaveChanges();
        Console.WriteLine($"Coleção MyAnimes deletada: {myAnime.Titulo}");
        return NoContent();
    }

    private static ObterMyAnimeDto ParaObterMyAnimeDto(MyAnime myAnime)
    {
        return new ObterMyAnimeDto
        {
            Id = myAnime.Id,
            Titulo = myAnime.Titulo,
            AnimesMalId = myAnime.AnimesMalId,
            HoraDaConsulta = DateTime.Now
        };
    }
}
