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

    public DbSet<MyAnimeMonitoringLocation> MonitoringLocations => Set<MyAnimeMonitoringLocation>();

    public DbSet<MyAnimeMonitoringSyncRun> MonitoringSyncRuns => Set<MyAnimeMonitoringSyncRun>();

    public DbSet<MyAnimeMonitoringSyncEvent> MonitoringSyncEvents => Set<MyAnimeMonitoringSyncEvent>();

    /// <summary>
    /// Representa a tabela de animes importados da ApiMyAnimeList no banco de dados.
    /// </summary>
    public DbSet<Anime> Animes { get; set; }

    /// <summary>
    /// Representa a trilha de auditoria de segurança separada dos logs tecnicos.
    /// </summary>
    public DbSet<SecurityAuditEvent> SecurityAuditEvents { get; set; }

    /// <summary>
    /// Episodios vistos por usuario. Dado pessoal isolado por <see cref="AnimeEpisodeWatch.AccountId"/>.
    /// </summary>
    public DbSet<AnimeEpisodeWatch> AnimeEpisodeWatches => Set<AnimeEpisodeWatch>();

    /// <summary>Ultimo status persistido da pasta de cada anime (calculado sob demanda).</summary>
    public DbSet<AnimeFolderStatus> AnimeFolderStatuses => Set<AnimeFolderStatus>();

    public DbSet<AnimeFolderRefreshRun> AnimeFolderRefreshRuns => Set<AnimeFolderRefreshRun>();

    public DbSet<AnimeFolderRefreshItem> AnimeFolderRefreshItems => Set<AnimeFolderRefreshItem>();

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
        modelBuilder.Entity<MyAnimeMonitoringLocation>(entity =>
        {
            entity.ToTable("MyAnimeMonitoringLocations");
            entity.HasKey(location => location.MyAnimeId);
            entity.Property(location => location.RootKey).HasMaxLength(100).IsRequired();
            entity.Property(location => location.RelativePath).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasOne<MyAnime>().WithOne().HasForeignKey<MyAnimeMonitoringLocation>(location => location.MyAnimeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MyAnimeMonitoringSyncRun>(entity =>
        {
            entity.ToTable("MyAnimeMonitoringSyncRuns");
            entity.HasKey(run => run.Id);
            entity.Property(run => run.Status).HasMaxLength(30).IsRequired();
            entity.Property(run => run.Error).HasMaxLength(2000);
            entity.HasIndex(run => new { run.StartedAtUtc, run.Id });
        });
        modelBuilder.Entity<MyAnimeMonitoringSyncEvent>(entity =>
        {
            entity.ToTable("MyAnimeMonitoringSyncEvents");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Severity).HasMaxLength(20).IsRequired();
            entity.Property(item => item.Code).HasMaxLength(100).IsRequired();
            entity.Property(item => item.RootKey).HasMaxLength(100).IsRequired();
            entity.Property(item => item.RelativePath).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(item => item.PreviousRootKey).HasMaxLength(100);
            entity.Property(item => item.NewRootKey).HasMaxLength(100);
            entity.Property(item => item.Detail).HasMaxLength(2000).IsRequired();
            entity.HasOne<MyAnimeMonitoringSyncRun>().WithMany().HasForeignKey(item => item.RunId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.RunId, item.Id });
        });
        // MalId é a chave primária mas NÃO é auto-incremento (é um ID externo do MyAnimeList)
        modelBuilder.Entity<Anime>()
            .HasKey(a => a.MalId);

        modelBuilder.Entity<Anime>()
            .Property(a => a.MalId)
            .ValueGeneratedNever(); // Desabilita auto-incremento

        modelBuilder.Entity<AnimeEpisodeWatch>(entity =>
        {
            entity.ToTable("AnimeEpisodeWatches", table =>
            {
                table.HasCheckConstraint("CK_AnimeEpisodeWatches_MalId", "[MalId] > 0");
                table.HasCheckConstraint("CK_AnimeEpisodeWatches_EpisodeNumber", "[EpisodeNumber] >= 1");
            });
            // A chave composta tambem serve as consultas por (AccountId, MalId): o indice clusterizado cobre esse prefixo.
            entity.HasKey(watch => new { watch.AccountId, watch.MalId, watch.EpisodeNumber });
            entity.Property(watch => watch.AccountId)
                .HasMaxLength(AnimeEpisodeWatch.AccountIdMaxLength)
                .IsRequired();
            entity.Property(watch => watch.WatchedAtUtc)
                .HasPrecision(7)
                .IsRequired();
        });

        modelBuilder.Entity<AnimeFolderStatus>(entity =>
        {
            entity.ToTable("AnimeFolderStatus", table =>
            {
                table.HasCheckConstraint("CK_AnimeFolderStatus_MalId", "[MalId] > 0");
                table.HasCheckConstraint("CK_AnimeFolderStatus_VideoCount", "[VideoCount] >= 0");
                table.HasCheckConstraint("CK_AnimeFolderStatus_State", "[State] IN ('none', 'partial', 'complete')");
            });
            entity.HasKey(status => status.MalId);
            entity.Property(status => status.MalId).ValueGeneratedNever();
            entity.Property(status => status.RootKey).HasMaxLength(AnimeFolderStatus.RootKeyMaxLength).IsRequired();
            entity.Property(status => status.RelativePath).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(status => status.State).HasMaxLength(AnimeFolderStatus.StateMaxLength).IsRequired();
            entity.Property(status => status.CheckedAtUtc).HasPrecision(7).IsRequired();
            entity.HasIndex(status => status.MyAnimeId);
        });
        modelBuilder.Entity<AnimeFolderRefreshRun>(entity =>
        {
            entity.ToTable("AnimeFolderRefreshRuns");
            entity.HasKey(run => run.Id);
            entity.Property(run => run.Status).HasMaxLength(30).IsRequired();
            entity.Property(run => run.RequestedBy).HasMaxLength(AnimeFolderRefreshRun.RequestedByMaxLength).IsRequired();
            entity.Property(run => run.Error).HasMaxLength(AnimeFolderRefreshRun.ErrorMaxLength);
            entity.HasIndex(run => new { run.StartedAtUtc, run.Id });
        });
        modelBuilder.Entity<AnimeFolderRefreshItem>(entity =>
        {
            entity.ToTable("AnimeFolderRefreshItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Outcome).HasMaxLength(30).IsRequired();
            entity.Property(item => item.State).HasMaxLength(AnimeFolderStatus.StateMaxLength);
            entity.Property(item => item.Detail).HasMaxLength(AnimeFolderRefreshItem.DetailMaxLength).IsRequired();
            entity.HasOne<AnimeFolderRefreshRun>().WithMany().HasForeignKey(item => item.RunId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(item => new { item.RunId, item.Id });
        });

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
        if (ChangeTracker.Entries<MyAnimeMonitoringSyncEvent>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Collection synchronization events are append-only and cannot be modified or deleted by the application.");
        }
    }
}
