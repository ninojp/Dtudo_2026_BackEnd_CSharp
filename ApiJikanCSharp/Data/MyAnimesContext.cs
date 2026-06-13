using ApiJikanCSharp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiJikanCSharp.Data;

public class MyAnimesContext: DbContext
{
    public MyAnimesContext(DbContextOptions<MyAnimesContext> options) : base(options)
    {        
    }
    public DbSet<MyAnime> MyAnimes { get; set; }
    public DbSet<Anime> Animes { get; set; }
}
