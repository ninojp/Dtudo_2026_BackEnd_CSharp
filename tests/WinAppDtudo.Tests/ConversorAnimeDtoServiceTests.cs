using LibDtudo.Shared.Dtos.MyAnimeList;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class ConversorAnimeDtoServiceTests
{
    [Fact]
    public void AnimeDto_StoresOnlyDistinctPositiveRelatedIds()
    {
        var anime = new AnimeDetails
        {
            MalId = 42,
            Title = "Anime atual"
        };

        var adiciona = ConversorAnimeDtoService.CriarAdicionaAnimeDto(
            anime,
            myAnimeId: 7,
            animesRelacionadosIds: [12, -1, 12, 0, 24]);
        var atualiza = ConversorAnimeDtoService.CriarAtualizaAnimeDto(
            anime,
            myAnimeId: 7,
            animesRelacionadosIds: [12, -1, 12, 0, 24]);

        Assert.Equal([12, 24], adiciona.AnimesRelacionadosIds);
        Assert.Equal([12, 24], atualiza.AnimesRelacionadosIds);
    }
}
