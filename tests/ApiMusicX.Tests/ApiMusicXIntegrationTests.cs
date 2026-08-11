using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ApiMusicX.Data;
using ApiMusicX.Dtos;
using ApiMusicX.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiMusicX.Tests;

public sealed class ApiMusicXIntegrationTests
{
    [Fact]
    public async Task CollectionReadRequiresAuthenticationAndCatalogPermission()
    {
        using var app = CreateApp();

        using var anonymousResponse = await app.CreateClient()
            .GetAsync("/apiLocal/collections");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var client = CreateClient(app, "scope=catalog.read");
        using var missingPermissionResponse = await client
            .GetAsync("/apiLocal/collections");

        Assert.Equal(HttpStatusCode.Forbidden, missingPermissionResponse.StatusCode);
    }

    [Fact]
    public async Task CollectionAndHealthEndpointsReturnLocalContracts()
    {
        using var app = CreateApp();
        var collectionId = SeedCollection(app);
        using var client = CreateClient(app, "scope=catalog.read health.read;permission=catalog.read");

        using var listResponse = await client.GetAsync("/apiLocal/collections?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResponse<MusicCollectionSummaryDto>>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.MusicCollectionId == collectionId);

        using var detailResponse = await client.GetAsync($"/apiLocal/collections/{collectionId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var collection = await detailResponse.Content.ReadFromJsonAsync<MusicCollectionDto>();
        Assert.NotNull(collection);
        Assert.Single(collection.Releases);
        Assert.Single(collection.Releases[0].Tracks);

        using var artistSearchResponse = await client.GetAsync(
            "/apiLocal/artists?search=Integracao&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, artistSearchResponse.StatusCode);
        var artistPage = await artistSearchResponse.Content
            .ReadFromJsonAsync<PagedResponse<MusicArtistSummaryDto>>();
        Assert.NotNull(artistPage);
        Assert.Contains(artistPage.Items, item => item.DisplayName == "Artista de Integracao");

        using var releaseListResponse = await client.GetAsync(
            $"/apiLocal/collections/{collectionId}/releases?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, releaseListResponse.StatusCode);
        var releasePage = await releaseListResponse.Content
            .ReadFromJsonAsync<PagedResponse<MusicReleaseDto>>();
        Assert.NotNull(releasePage);
        Assert.Single(releasePage.Items);

        using var releaseResponse = await client.GetAsync(
            $"/apiLocal/releases/{collection.Releases[0].MusicReleaseId}");
        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);

        using var healthResponse = await client.GetAsync("/apiLocal/Health");
        Assert.Equal(HttpStatusCode.Forbidden, healthResponse.StatusCode);

        using var healthClient = CreateClient(app, "scope=health.read;permission=health.read");
        using var authorizedHealthResponse = await healthClient.GetAsync("/apiLocal/Health");
        Assert.Equal(HttpStatusCode.OK, authorizedHealthResponse.StatusCode);
        var health = await authorizedHealthResponse.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(health);
        Assert.Equal("ok", health.Status);
        Assert.Equal("ok", health.Database);
    }

    [Fact]
    public async Task ImportReplayIsIdempotentAndConflictingNameIsNotOverwritten()
    {
        using var app = CreateApp();
        using var client = CreateClient(app, "scope=catalog.write;permission=catalog.write");
        var request = CreateImportRequest();

        using var firstResponse = await client.PostAsJsonAsync(
            "/apiLocal/collections/import",
            request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<ImportMusicCollectionResponse>();
        Assert.NotNull(first);
        Assert.True(first.Created);
        Assert.Equal(1, first.ArtistsAdded);
        Assert.Equal(1, first.ReleasesAdded);
        Assert.Equal(1, first.TracksAdded);
        Assert.Equal(1, first.LocalFileReferencesAdded);

        using var replayResponse = await client.PostAsJsonAsync(
            "/apiLocal/collections/import",
            request);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        var replay = await replayResponse.Content.ReadFromJsonAsync<ImportMusicCollectionResponse>();
        Assert.NotNull(replay);
        Assert.False(replay.Created);
        Assert.False(replay.Changed);
        Assert.Equal(0, replay.ArtistsAdded);
        Assert.Equal(0, replay.ReleasesAdded);
        Assert.Equal(0, replay.TracksAdded);
        Assert.Equal(0, replay.LocalFileReferencesAdded);

        var conflictingRequest = CreateImportRequest("Nome divergente");
        using var conflictResponse = await client.PostAsJsonAsync(
            "/apiLocal/collections/import",
            conflictingRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal("application/problem+json", conflictResponse.Content.Headers.ContentType?.MediaType);

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MusicContext>();
        Assert.Equal(1, await context.MusicCollections.CountAsync());
        Assert.Equal("Colecao importada", await context.MusicCollections.Select(item => item.DisplayName).SingleAsync());
        Assert.Equal(1, await context.MusicReleases.CountAsync());
        Assert.Equal(1, await context.MusicTracks.CountAsync());
        Assert.Equal(1, await context.MusicLocalFileReferences.CountAsync());
    }

    [Fact]
    public async Task CrudOperationsUseWriteAndDeletePolicies()
    {
        using var app = CreateApp();
        var artistId = SeedArtist(app);
        using var readClient = CreateClient(app, "scope=catalog.read;permission=catalog.read");
        using var writeClient = CreateClient(app, "scope=catalog.write;permission=catalog.write");
        using var deleteClient = CreateClient(app, "scope=catalog.delete;permission=catalog.delete");

        using var unauthorizedCreate = await readClient.PostAsJsonAsync(
            "/apiLocal/collections",
            new CreateMusicCollectionRequest
            {
                DisplayName = "Colecao CRUD",
                Artists = [new MusicCollectionArtistRequest { MusicArtistId = artistId }]
            });
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedCreate.StatusCode);

        using var createResponse = await writeClient.PostAsJsonAsync(
            "/apiLocal/collections",
            new CreateMusicCollectionRequest
            {
                DisplayName = "Colecao CRUD",
                Artists = [new MusicCollectionArtistRequest { MusicArtistId = artistId }]
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MusicCollectionDto>();
        Assert.NotNull(created);

        using var updateResponse = await writeClient.PutAsJsonAsync(
            $"/apiLocal/collections/{created.MusicCollectionId}",
            new UpdateMusicCollectionRequest
            {
                DisplayName = "Colecao CRUD Atualizada",
                Description = "Descricao local"
            });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var updatedResponse = await readClient.GetAsync(
            $"/apiLocal/collections/{created.MusicCollectionId}");
        var updated = await updatedResponse.Content.ReadFromJsonAsync<MusicCollectionDto>();
        Assert.NotNull(updated);
        Assert.Equal("Colecao CRUD Atualizada", updated.DisplayName);

        using var unauthorizedDelete = await writeClient.DeleteAsync(
            $"/apiLocal/collections/{created.MusicCollectionId}");
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedDelete.StatusCode);

        using var deleteResponse = await deleteClient.DeleteAsync(
            $"/apiLocal/collections/{created.MusicCollectionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var deletedResponse = await readClient.GetAsync(
            $"/apiLocal/collections/{created.MusicCollectionId}");
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }

    private static ApiMusicXFactory CreateApp()
    {
        var app = new ApiMusicXFactory();
        app.EnsureDatabase();
        return app;
    }

    private static HttpClient CreateClient(ApiMusicXFactory app, string claims)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", claims);
        return client;
    }

    private static long SeedCollection(ApiMusicXFactory app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MusicContext>();
        var artist = new MusicArtist("Artista de Integracao", MusicArtistType.Band);
        var collection = new MusicCollection("Colecao de Integracao");
        var release = new MusicRelease("Release de Integracao", MusicReleaseType.Album, 2026);
        var track = new MusicTrack(release, "Faixa de Integracao", sequence: 1);
        context.AddRange(
            artist,
            collection,
            release,
            new MusicCollectionArtist(collection, artist, MusicCollectionArtistRole.Primary),
            new MusicCollectionRelease(collection, release, "albums", 0),
            track,
            new MusicReleaseArtist(release, artist, MusicCreditRole.Primary));
        context.SaveChanges();
        return collection.MusicCollectionId;
    }

    private static long SeedArtist(ApiMusicXFactory app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MusicContext>();
        var artist = new MusicArtist("Artista CRUD", MusicArtistType.Solo);
        context.MusicArtists.Add(artist);
        context.SaveChanges();
        return artist.MusicArtistId;
    }

    private static ImportMusicCollectionRequest CreateImportRequest(string displayName = "Colecao importada")
        => new()
        {
            DisplayName = displayName,
            ExternalIdentifiers =
            [
                new ExternalSourceIdentifierRequest
                {
                    Provider = "ApiNode.MyMusicX",
                    ResourceType = "Collection",
                    ExternalId = "collection-import-1"
                }
            ],
            Artists =
            [
                new MusicArtistImportRequest
                {
                    DisplayName = "Artista importado",
                    ArtistType = MusicArtistType.Solo,
                    ExternalIdentifiers =
                    [
                        new ExternalSourceIdentifierRequest
                        {
                            Provider = "Discogs",
                            ResourceType = "Artist",
                            ExternalId = "artist-import-1"
                        }
                    ],
                    CollectionRole = MusicCollectionArtistRole.Primary
                }
            ],
            Releases =
            [
                new MusicReleaseImportRequest
                {
                    Title = "Release importado",
                    ReleaseType = MusicReleaseType.Album,
                    ReleaseYear = 2026,
                    ExternalIdentifiers =
                    [
                        new ExternalSourceIdentifierRequest
                        {
                            Provider = "Discogs",
                            ResourceType = "Release",
                            ExternalId = "release-import-1"
                        }
                    ],
                    Tracks =
                    [
                        new MusicTrackImportRequest
                        {
                            Title = "Faixa importada",
                            PositionLabel = "1",
                            Sequence = 1,
                            ExternalIdentifiers =
                            [
                                new ExternalSourceIdentifierRequest
                                {
                                    Provider = "Discogs",
                                    ResourceType = "Track",
                                    ExternalId = "track-import-1"
                                }
                            ],
                            LocalFileReferences =
                            [
                                new MusicLocalFileReferenceImportRequest
                                {
                                    RelativePath = "imports/release/faixa.flac",
                                    MediaKind = MusicMediaKind.Audio,
                                    Role = MusicLocalFileRole.TrackAudio
                                }
                            ]
                        }
                    ]
                }
            ]
        };

    private sealed class ApiMusicXFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            connection.Open();
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Issuer"] = "https://identity.test",
                    ["Authentication:Audience"] = "api-musicx-test",
                    ["ConnectionStrings:LocalDbConnection"] = "Data Source=ApiMusicXTests",
                    ["Seq:Url"] = string.Empty,
                    ["Cors:AllowedOrigins:0"] = "https://localhost:5173",
                    ["Cors:AllowedOrigins:1"] = "https://localhost:5178"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<MusicContext>>();
                services.RemoveAll<DbContextOptions<MusicContext>>();
                services.RemoveAll<MusicContext>();
                services.AddDbContext<MusicContext>(options => options.UseSqlite(connection));
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
            });
        }

        public void EnsureDatabase()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MusicContext>();
            context.Database.EnsureCreated();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                connection.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var rawClaims = Request.Headers["X-Test-Claims"].ToString();
            if (string.IsNullOrWhiteSpace(rawClaims))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = rawClaims
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(parts => parts.Length == 2)
                .Select(parts => new Claim(parts[0], parts[1]))
                .ToList();
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
