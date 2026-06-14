using ApiCSharp.Data;
using ApiCSharp.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiCSharp.Controllers;

[ApiController]
[Route("apiLocal/[controller]")]
public class AnimeController : ControllerBase
{
    private readonly MyAnimesContext _context;
    //===========================================
    public AnimeController(MyAnimesContext context)
    {
        _context = context;
    }
    //========================================================
    [HttpPost]
    public IActionResult AdicionarAnime([FromBody] Anime anime)
    {
        _context.Animes.Add(anime);
        _context.SaveChanges();
        Console.WriteLine($"Anime adicionado: {anime.Titulo}");
        return CreatedAtAction(nameof(ObterAnimePorId), new { id = anime.MalId }, anime);
    }
    //===================================================================================
    [HttpGet]
    public ActionResult<List<Anime>> ObterAnimes([FromQuery] int skip=0, [FromQuery] int take=5)
    {
        return Ok(_context.Animes.OrderBy(a => a.MalId).Skip(skip).Take(take).ToList());
    }
    //===========================================================
    [HttpGet("{id}")]
    public ActionResult<Anime> ObterAnimePorId(int id)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");

        var anime = _context.Animes.FirstOrDefault(a => a.MalId == id);

        if (anime is null) return NotFound($"Anime com ID {id} não encontrado.");

        Console.WriteLine($"Anime encontrado: {anime.Titulo}");
        return Ok(anime);
    }
}
