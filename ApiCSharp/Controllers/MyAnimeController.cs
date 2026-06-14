using ApiCSharp.Data;
using ApiCSharp.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiCSharp.Controllers;

[ApiController]
[Route("apiLocal/[controller]")]
public class MyAnimeController : ControllerBase
{
    private readonly MyAnimesContext _context;
    //=============================================
    public MyAnimeController(MyAnimesContext context)
    {
        _context = context;
    }
    //=============================================
    [HttpPost]
    public IActionResult AdicionarMyAnime([FromBody] MyAnime myAnime)
    {
        _context.MyAnimes.Add(myAnime);
        _context.SaveChanges();
        Console.WriteLine($"Anime adicionado: {myAnime.Titulo}");
        return CreatedAtAction(nameof(ObterMyAnimePorId), new { id = myAnime.Id }, myAnime);
    }
    //======================================================================================
    [HttpGet]
    public ActionResult<List<MyAnime>> ObterMyAnimes([FromQuery] int skip = 0, [FromQuery] int take = 5)
    {
        return Ok(_context.MyAnimes.OrderBy(a => a.Id).Skip(skip).Take(take).ToList());
    }
    //===========================================================
    [HttpGet("{id}")]
    public ActionResult<MyAnime> ObterMyAnimePorId(int id)
    {
        if (id <= 0) return BadRequest("ID deve ser um número positivo.");

        var myAnime = _context.MyAnimes.FirstOrDefault(a => a.Id == id);

        if (myAnime is null) return NotFound($"Anime com ID {id} não encontrado.");

        Console.WriteLine($"Anime encontrado: {myAnime.Titulo}");
        return Ok(myAnime);
    }
    //===========================================================
}
