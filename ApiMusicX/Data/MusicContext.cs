using ApiMusicX.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiMusicX.Data;

public sealed class MusicContext(DbContextOptions<MusicContext> options) : DbContext(options)
{
    public DbSet<MusicArtist> MusicArtists => Set<MusicArtist>();

    public DbSet<MusicArtistAlias> MusicArtistAliases => Set<MusicArtistAlias>();

    public DbSet<MusicCollection> MusicCollections => Set<MusicCollection>();

    public DbSet<MusicCollectionArtist> MusicCollectionArtists => Set<MusicCollectionArtist>();

    public DbSet<MusicRelease> MusicReleases => Set<MusicRelease>();

    public DbSet<MusicCollectionRelease> MusicCollectionReleases => Set<MusicCollectionRelease>();

    public DbSet<MusicReleaseArtist> MusicReleaseArtists => Set<MusicReleaseArtist>();

    public DbSet<MusicTrack> MusicTracks => Set<MusicTrack>();

    public DbSet<MusicTrackArtist> MusicTrackArtists => Set<MusicTrackArtist>();

    public DbSet<MusicLocalFileReference> MusicLocalFileReferences => Set<MusicLocalFileReference>();

    public DbSet<ExternalSourceIdentifier> ExternalSourceIdentifiers => Set<ExternalSourceIdentifier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MusicContext).Assembly);
    }
}
