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
    /// Representa a tabela de animes importados da ApiMyAnimeList no banco de dados.
    /// </summary>
    public DbSet<Anime> Animes { get; set; }

    /// <summary>
    /// Representa a trilha de auditoria de segurança separada dos logs tecnicos.
    /// </summary>
    public DbSet<SecurityAuditEvent> SecurityAuditEvents { get; set; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAuditEventsAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureAuditEventsAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

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

        modelBuilder.Entity<SecurityAuditEvent>(entity =>
        {
            entity.HasKey(auditEvent => auditEvent.Id);

            entity.Property(auditEvent => auditEvent.Actor)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(auditEvent => auditEvent.Action)
                .IsRequired()
                .HasMaxLength(128);
            entity.Property(auditEvent => auditEvent.Target)
                .IsRequired()
                .HasMaxLength(512);
            entity.Property(auditEvent => auditEvent.Result)
                .IsRequired()
                .HasMaxLength(64);
            entity.Property(auditEvent => auditEvent.OccurredAtUtc)
                .IsRequired()
                .HasPrecision(7);
            entity.Property(auditEvent => auditEvent.DeviceId)
                .IsRequired()
                .HasMaxLength(256);
            entity.Property(auditEvent => auditEvent.CorrelationId)
                .IsRequired()
                .HasMaxLength(128);
            entity.Property(auditEvent => auditEvent.Reason)
                .IsRequired()
                .HasMaxLength(1000);
            entity.Property(auditEvent => auditEvent.RetentionUntilUtc)
                .IsRequired()
                .HasPrecision(7);

            entity.HasIndex(auditEvent => new
            {
                auditEvent.OccurredAtUtc,
                auditEvent.Id
            });
            entity.HasIndex(auditEvent => auditEvent.RetentionUntilUtc);
        });
    }

    private void EnsureAuditEventsAppendOnly()
    {
        if (ChangeTracker.Entries<SecurityAuditEvent>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Security audit events are append-only and cannot be modified or deleted by the application.");
        }
    }
}
