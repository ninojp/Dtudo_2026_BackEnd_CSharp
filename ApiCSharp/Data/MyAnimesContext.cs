using ApiCSharp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiCSharp.Data;

public class MyAnimesContext: DbContext
{
    public MyAnimesContext(DbContextOptions<MyAnimesContext> options) : base(options)
    {        
    }
    public DbSet<MyAnime> MyAnimes { get; set; }

    public DbSet<Anime> Animes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MalId é a chave primária mas NÃO é auto-incremento (é um ID externo do MyAnimeList)
        modelBuilder.Entity<Anime>()
            .HasKey(a => a.MalId);

        modelBuilder.Entity<Anime>()
            .Property(a => a.MalId)
            .ValueGeneratedNever(); // Desabilita auto-incremento
    }
}
