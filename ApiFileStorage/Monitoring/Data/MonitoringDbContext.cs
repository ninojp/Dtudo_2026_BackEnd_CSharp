using Microsoft.EntityFrameworkCore;

namespace ApiFileStorage.Monitoring.Data;

public sealed class MonitoringDbContext(DbContextOptions<MonitoringDbContext> options) : DbContext(options)
{
    public DbSet<MonitoredLocation> Locations => Set<MonitoredLocation>();
    public DbSet<InventorySnapshot> Snapshots => Set<InventorySnapshot>();
    public DbSet<InventoryEntry> Entries => Set<InventoryEntry>();
    public DbSet<MonitoringObservation> Observations => Set<MonitoringObservation>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnsureAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("monitoring");

        modelBuilder.Entity<MonitoredLocation>(entity =>
        {
            entity.ToTable("Locations");
            entity.HasKey(location => location.Id);
            entity.Property(location => location.RootKey).HasMaxLength(100).IsRequired();
            entity.Property(location => location.RelativePath).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasIndex(location => location.RootKey);
        });

        modelBuilder.Entity<InventorySnapshot>(entity =>
        {
            entity.ToTable("Snapshots", table =>
            {
                table.HasCheckConstraint("CK_Snapshots_Counts", "[FileCount] >= 0 AND [DirectoryCount] >= 0 AND [TotalBytes] >= 0");
                table.HasCheckConstraint("CK_Snapshots_Times", "[FinishedAtUtc] >= [StartedAtUtc]");
                table.HasCheckConstraint("CK_Snapshots_Completion", "[Completion] IN ('Complete', 'Partial', 'Unavailable', 'Cancelled')");
            });
            entity.HasKey(snapshot => snapshot.Id);
            entity.Property(snapshot => snapshot.Completion).HasConversion<string>().HasMaxLength(20);
            entity.HasOne<MonitoredLocation>().WithMany().HasForeignKey(snapshot => snapshot.LocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(snapshot => new { snapshot.LocationId, snapshot.Completion, snapshot.FinishedAtUtc, snapshot.Id });
        });

        modelBuilder.Entity<InventoryEntry>(entity =>
        {
            entity.ToTable("Entries", table => table.HasCheckConstraint("CK_Entries_Length", "[LengthBytes] >= 0 AND ([IsDirectory] = 0 OR [LengthBytes] = 0)"));
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.RelativePath).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasOne<InventorySnapshot>().WithMany(snapshot => snapshot.Entries).HasForeignKey(entry => entry.SnapshotId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(entry => new { entry.SnapshotId, entry.Id });
        });

        modelBuilder.Entity<MonitoringObservation>(entity =>
        {
            entity.ToTable("Observations", table => table.HasCheckConstraint("CK_Observations_Source", "[Source] IN ('LiveNotification', 'InventoryComparison', 'Monitor')"));
            entity.HasKey(observation => observation.Id);
            entity.Property(observation => observation.Source).HasConversion<string>().HasMaxLength(30);
            entity.Property(observation => observation.Code).HasMaxLength(100).IsRequired();
            entity.Property(observation => observation.RelativePath).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(observation => observation.PreviousRelativePath).HasColumnType("nvarchar(max)");
            entity.Property(observation => observation.Detail).HasMaxLength(2000).IsRequired();
            entity.HasOne<MonitoredLocation>().WithMany().HasForeignKey(observation => observation.LocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(observation => new { observation.LocationId, observation.ObservedAtUtc, observation.Id });
        });
    }

    private void EnsureAppendOnly()
    {
        if (ChangeTracker.Entries().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Monitoring records are append-only; updates and deletions are not permitted.");
        }
    }
}
