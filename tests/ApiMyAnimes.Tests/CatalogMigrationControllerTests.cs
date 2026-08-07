using System.Net;
using System.Net.Http.Json;
using ApiMyAnimes.Controllers;
using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ApiMyAnimes.Tests;

public sealed class CatalogMigrationControllerTests
{
    [Fact]
    public void EnsureCollection_ReplayDoesNotCreateDuplicate()
    {
        using var context = CreateContext();
        var controller = new CatalogMigrationController(context);
        var request = new EnsureMyAnimeCollectionRequest
        {
            Titulo = "  Colecao A  ",
            AnimesMalId = [2, 1, 2]
        };

        var first = controller.EnsureMyAnimeCollection(request);
        var firstResult = Assert.IsType<ObjectResult>(first.Result);
        Assert.Equal(StatusCodes.Status201Created, firstResult.StatusCode);

        var second = controller.EnsureMyAnimeCollection(request);
        var secondResult = Assert.IsType<OkObjectResult>(second.Result);
        var response = Assert.IsType<EnsureMyAnimeCollectionResponse>(secondResult.Value);

        Assert.Equal(1, context.MyAnimes.Count());
        Assert.Equal([1, 2], context.MyAnimes.Single().AnimesMalId);
        Assert.False(response.Created);
        Assert.False(response.Changed);
    }

    [Fact]
    public void EnsureAssociation_ReplayPreservesCatalogConsistency()
    {
        using var context = CreateContext();
        var collection = new MyAnime
        {
            Titulo = "Colecao A",
            AnimesMalId = []
        };
        var anime = new Anime
        {
            MalId = 42,
            Titulo = "Anime A",
            Episodios = 1,
            MyAnimeID = 0
        };
        context.MyAnimes.Add(collection);
        context.Animes.Add(anime);
        context.SaveChanges();

        var controller = new CatalogMigrationController(context);
        var request = new EnsureAnimeAssociationRequest { MyAnimeId = collection.Id };

        var first = controller.EnsureAnimeAssociation(42, request);
        var firstResponse = Assert.IsType<OkObjectResult>(first.Result);
        Assert.True(Assert.IsType<EnsureAnimeAssociationResponse>(firstResponse.Value).Changed);

        var second = controller.EnsureAnimeAssociation(42, request);
        var secondResponse = Assert.IsType<OkObjectResult>(second.Result);
        Assert.False(Assert.IsType<EnsureAnimeAssociationResponse>(secondResponse.Value).Changed);

        Assert.Equal(collection.Id, context.Animes.Single().MyAnimeID);
        Assert.Equal([42], context.MyAnimes.Single().AnimesMalId);
    }

    [Fact]
    public void EnsureAssociation_ReassignmentRemovesAnimeFromPreviousCollection()
    {
        using var context = CreateContext();
        var previousCollection = new MyAnime
        {
            Titulo = "Colecao A",
            AnimesMalId = [42]
        };
        var targetCollection = new MyAnime
        {
            Titulo = "Colecao B",
            AnimesMalId = []
        };
        var anime = new Anime
        {
            MalId = 42,
            Titulo = "Anime A",
            Episodios = 1,
            MyAnimeID = 0
        };
        context.MyAnimes.AddRange(previousCollection, targetCollection);
        context.Animes.Add(anime);
        context.SaveChanges();
        anime.MyAnimeID = previousCollection.Id;
        context.SaveChanges();

        var controller = new CatalogMigrationController(context);
        var request = new EnsureAnimeAssociationRequest { MyAnimeId = targetCollection.Id };

        var result = controller.EnsureAnimeAssociation(42, request);
        var response = Assert.IsType<OkObjectResult>(result.Result);

        Assert.True(Assert.IsType<EnsureAnimeAssociationResponse>(response.Value).Changed);
        Assert.Equal(targetCollection.Id, context.Animes.Single().MyAnimeID);
        Assert.Empty(context.MyAnimes.Single(item => item.Id == previousCollection.Id).AnimesMalId);
        Assert.Equal([42], context.MyAnimes.Single(item => item.Id == targetCollection.Id).AnimesMalId);
    }

    private static MyAnimesContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyAnimesContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new MyAnimesContext(options);
    }
}

public sealed class CatalogMigrationAuthorizationTests
{
    [Fact]
    public async Task AnonymousMigrationCommand_IsRejected()
    {
        await using var app = CreateApp();

        using var response = await app.CreateClient().PutAsJsonAsync(
            "/apiLocal/catalog-migration/my-animes/by-title",
            new EnsureMyAnimeCollectionRequest
            {
                Titulo = "Colecao A",
                AnimesMalId = [1]
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MissingCatalogPermission_IsRejected()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=catalog.write");

        using var response = await client.PutAsJsonAsync(
            "/apiLocal/catalog-migration/my-animes/by-title",
            new EnsureMyAnimeCollectionRequest
            {
                Titulo = "Colecao A",
                AnimesMalId = [1]
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MatchingCatalogPermission_ReachesControllerValidation()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=catalog.write;permission=catalog.write");

        using var response = await client.PutAsync(
            "/apiLocal/catalog-migration/my-animes/by-title",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateApp()
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = "api-my-animes",
                        ["ConnectionStrings:LocalDbConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Dtudo2026Tests;Trusted_Connection=True;TrustServerCertificate=True",
                        ["Seq:Url"] = string.Empty
                    });
                });
                builder.ConfigureTestServices(services => TestAuthentication.Add(services));
            });
}
