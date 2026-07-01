using LibDtudo.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Data;

/// <summary>
/// Contexto do banco de dados para a aplicação MyAnimes, utilizando Entity Framework Core.
/// </summary>
public class MyAnimesContext: DbContext
{
    /// <summary>
    /// Construtor do contexto do banco de dados, recebendo as opções de configuração.
    /// </summary>
    /// <param name="options"></param>
    public MyAnimesContext(DbContextOptions<MyAnimesContext> options) : base(options)
    {        
    }
    /// <summary>
    /// Representa a tabela de animes do usuário no banco de dados.
    /// </summary>
    public DbSet<MyAnime> MyAnimes { get; set; }

    /// <summary>
    /// Representa a tabela de animes importados da ApiJikan no banco de dados.
    /// </summary>
    public DbSet<Anime> Animes { get; set; }

    /// <summary>
    /// Configura o modelo do banco de dados, definindo chaves primárias e propriedades específicas para as entidades.
    /// </summary>
    /// <param name="modelBuilder"></param>
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
