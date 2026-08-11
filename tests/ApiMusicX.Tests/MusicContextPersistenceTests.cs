using ApiMusicX.Data;
using ApiMusicX.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ApiMusicX.Tests;

public sealed class MusicContextPersistenceTests
{
    [Fact]
    public void EnsureCreatedBuildsRelationalSchemaWithExpectedTables()
    {
        using var fixture = new SqliteFixture();
        using var context = fixture.CreateContext();

        Assert.True(context.Database.CanConnect());

        var tableNames = context.Database.GetDbConnection()
            .CreateCommand();
        tableNames.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        using var reader = tableNames.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("MusicArtists", tables);
        Assert.Contains("MusicCollections", tables);
        Assert.Contains("MusicReleases", tables);
        Assert.Contains("MusicTracks", tables);
        Assert.Contains("MusicLocalFileReferences", tables);
        Assert.Contains("ExternalSourceIdentifiers", tables);
    }

    [Fact]
    public async Task ArtistCollectionReleaseTrackRelationshipsPersistTogether()
    {
        using var fixture = new SqliteFixture();
        await using var context = fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();

        var artist = new MusicArtist("Banda de Teste", MusicArtistType.Band);
        var collection = new MusicCollection("Colecao Banda de Teste");
        var release = new MusicRelease("Primeiro Album", MusicReleaseType.Album, 2026);
        var track = new MusicTrack(release, "Faixa Um", positionLabel: "A1", sequence: 1);

        context.AddRange(
            artist,
            collection,
            release,
            new MusicCollectionArtist(collection, artist, MusicCollectionArtistRole.Primary),
            new MusicCollectionRelease(collection, release, "albums", 0),
            track,
            new MusicReleaseArtist(release, artist, MusicCreditRole.Primary),
            new MusicTrackArtist(track, artist, MusicCreditRole.Primary));

        await context.SaveChangesAsync();

        Assert.Equal(1, await context.MusicCollectionArtists.CountAsync());
        Assert.Equal(1, await context.MusicCollectionReleases.CountAsync());
        Assert.Equal(1, await context.MusicReleaseArtists.CountAsync());
        Assert.Equal(1, await context.MusicTrackArtists.CountAsync());
        Assert.Equal("Faixa Um", await context.MusicTracks.Select(item => item.Title).SingleAsync());
    }

    [Fact]
    public async Task ExternalIdentityIsUniqueAcrossProvidersAndResourceTypes()
    {
        using var fixture = new SqliteFixture();
        await using var context = fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();

        var artist = new MusicArtist("Artista Externo");
        var release = new MusicRelease("Release Externo");
        context.AddRange(artist, release);
        await context.SaveChangesAsync();

        context.Add(new ExternalSourceIdentifier("Discogs", "Release", "12345")
        {
            MusicRelease = release
        });
        await context.SaveChangesAsync();

        await using var duplicateContext = fixture.CreateContext();
        var duplicate = new ExternalSourceIdentifier("Discogs", "Release", "12345")
        {
            MusicArtist = artist
        };
        duplicateContext.Add(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
        Assert.Equal(1, await context.ExternalSourceIdentifiers.CountAsync());
    }

    [Fact]
    public async Task ExternalIdentityRequiresExactlyOneOwner()
    {
        using var fixture = new SqliteFixture();
        await using var context = fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();

        context.Add(new ExternalSourceIdentifier("Discogs", "Release", "ownerless"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ReimportedCollectionReleaseAndTrackDoNotCreateDuplicates()
    {
        using var fixture = new SqliteFixture();
        await using var context = fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();

        var collection = new MusicCollection("Colecao Idempotente");
        var release = new MusicRelease("Release Idempotente", MusicReleaseType.Album, 2026);
        var track = new MusicTrack(release, "Faixa Idempotente", positionLabel: "1", sequence: 1);
        context.AddRange(collection, release, new MusicCollectionRelease(collection, release), track);
        await context.SaveChangesAsync();

        await using var duplicateContext = fixture.CreateContext();
        var persistedCollection = await duplicateContext.MusicCollections.SingleAsync();
        var persistedRelease = await duplicateContext.MusicReleases.SingleAsync();
        var duplicateLink = new MusicCollectionRelease(persistedCollection, persistedRelease);
        var duplicateTrack = new MusicTrack(
            persistedRelease,
            "Faixa Idempotente",
            positionLabel: "1",
            sequence: 1);
        duplicateContext.AddRange(duplicateLink, duplicateTrack);

        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
        Assert.Equal(1, await context.MusicCollectionReleases.CountAsync());
        Assert.Equal(1, await context.MusicTracks.CountAsync());
    }

    [Theory]
    [InlineData("C:\\Music\\track.flac")]
    [InlineData("\\\\server\\share\\track.flac")]
    [InlineData("../track.flac")]
    [InlineData("/var/music/track.flac")]
    public void LocalFileReferenceRejectsAbsoluteOrTraversalPath(string path)
    {
        var release = new MusicRelease("Release com arquivo");

        Assert.Throws<ArgumentException>(() => new MusicLocalFileReference(
            release,
            path,
            MusicMediaKind.Audio,
            MusicLocalFileRole.TrackAudio));
    }

    [Fact]
    public async Task LocalFileReferencePersistsRelativePathWithoutTouchingDisk()
    {
        using var fixture = new SqliteFixture();
        await using var context = fixture.CreateContext();
        await context.Database.EnsureCreatedAsync();

        var relativePath = $"references/{Guid.NewGuid():N}.flac";
        var release = new MusicRelease("Release com referencia");
        context.Add(new MusicLocalFileReference(
            release,
            relativePath,
            MusicMediaKind.Audio,
            MusicLocalFileRole.TrackAudio));
        await context.SaveChangesAsync();

        var persisted = await context.MusicLocalFileReferences.SingleAsync();
        Assert.Equal(relativePath, persisted.RelativePath);
        Assert.Equal(relativePath, persisted.NormalizedPath);
        Assert.False(File.Exists(Path.Combine(Path.GetTempPath(), relativePath)));
    }

    [Fact]
    public void SqlServerMigrationIsPresentInDesignTimeContext()
    {
        using var context = new MusicContextFactory().CreateDbContext([]);

        Assert.Contains(
            context.Database.GetMigrations(),
            migration => migration.EndsWith("_InitialMusicCollection", StringComparison.Ordinal));
    }

    private sealed class SqliteFixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public SqliteFixture()
        {
            _connection.Open();
        }

        public MusicContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<MusicContext>()
                .UseSqlite(_connection)
                .Options;
            var context = new MusicContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
