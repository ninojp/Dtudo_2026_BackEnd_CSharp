using ApiMyAnimes.Controllers;
using ApiMyAnimes.Data;
using ApiMyAnimes.Services;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApiMyAnimes.Tests;

public sealed class AnimeControllerTests
{
    [Fact]
    public async Task AddAnime_PersistsAndReturnsRelatedIds()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.AdicionarAnime(new AdicionaAnimeDto
        {
            MalId = 42,
            Titulo = "Anime atual",
            AnimesRelacionadosIds = [12, -1, 12, 24]
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ObterAnimeDto>(created.Value);
        var anime = await context.Animes.SingleAsync();

        Assert.Equal([12, 24], anime.AnimesRelacionadosIds);
        Assert.Equal([12, 24], response.AnimesRelacionadosIds);
    }

    [Fact]
    public void UpdateAnime_ReplacesRelatedIdsWithNormalizedValues()
    {
        using var context = CreateContext();
        context.Animes.Add(new Anime
        {
            MalId = 42,
            Titulo = "Anime atual",
            AnimesRelacionadosIds = [1]
        });
        context.SaveChanges();

        var controller = CreateController(context);
        var result = controller.AtualizarAnime(42, new AtualizaAnimeDto
        {
            Titulo = "Anime atualizado",
            AnimesRelacionadosIds = [31, 0, 31, 47]
        });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal([31, 47], context.Animes.Single().AnimesRelacionadosIds);
    }

    private static AnimeController CreateController(MyAnimesContext context)
        => new(
            context,
            new MyAnimeListImportClient(new HttpClient
            {
                BaseAddress = new Uri("https://api-my-anime-list.test/")
            }),
            new AnimeBuscaLocalService(context),
            new AnimeTitleConflictService(context),
            NullLogger<AnimeController>.Instance);

    private static MyAnimesContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyAnimesContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MyAnimesContext(options);
    }
}
