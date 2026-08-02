using ApiMyAnimes.Data;
using ApiMyAnimes.Services;
using LibDtudo.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Tests;

public sealed class AnimeBuscaLocalServiceTests
{
    [Fact]
    public async Task BuscarAsync_EncontraSleazyFamilySemFalsosPositivosDeTokenCurto()
    {
        await using var context = CriarContexto();
        context.Animes.AddRange(
            CriarAnime(1, "Sleazy Family"),
            CriarAnime(2, "A Kite"),
            CriarAnime(3, "A-Channel"),
            CriarAnime(4, "A.I.C.O. Incarnation"),
            CriarAnime(5, "A-Channel: +A-Channel"));
        await context.SaveChangesAsync();

        var service = new AnimeBuscaLocalService(context);
        var resultados = await service.BuscarAsync("Sleazy Family", 100, CancellationToken.None);

        var resultado = Assert.Single(resultados);
        Assert.Equal("Sleazy Family", resultado.Titulo);
    }

    [Fact]
    public async Task BuscarAsync_LimitaEmCemEPriorizaTituloExato()
    {
        await using var context = CriarContexto();
        context.Animes.Add(CriarAnime(1, "Family"));

        for (var index = 2; index <= 122; index++)
            context.Animes.Add(CriarAnime(index, $"Family {index:000}"));

        await context.SaveChangesAsync();

        var service = new AnimeBuscaLocalService(context);
        var resultados = await service.BuscarAsync("Family", 500, CancellationToken.None);

        Assert.Equal(100, resultados.Count);
        Assert.Equal("Family", resultados[0].Titulo);
    }

    private static MyAnimesContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<MyAnimesContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MyAnimesContext(options);
    }

    private static Anime CriarAnime(int malId, string titulo) => new()
    {
        MalId = malId,
        Titulo = titulo
    };
}
