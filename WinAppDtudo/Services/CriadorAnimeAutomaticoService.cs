using LibDtudo.Shared.Dtos.MyAnimeList;

namespace WinAppDtudo.Services;

public class CriadorAnimeAutomaticoService
{
    private readonly ApiMyAnimesService _apiMyAnimesService;

    public CriadorAnimeAutomaticoService(ApiMyAnimesService? apiMyAnimesService = null)
    {
        _apiMyAnimesService = apiMyAnimesService ?? new ApiMyAnimesService();
    }

    public async Task CriarAnimeDoMyAnimeAsync(AnimeDetails anime, int myAnimeId)
    {
        ArgumentNullException.ThrowIfNull(anime);

        if (myAnimeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(myAnimeId), "MyAnimeId deve ser maior que zero.");

        var dto = ConversorAnimeDtoService.CriarAdicionaAnimeDto(anime, myAnimeId);
        await _apiMyAnimesService.AdicionarAnimeAsync(dto);
        await _apiMyAnimesService.AssociarAnimeAoMyAnimeAsync(anime.MalId, myAnimeId);
    }
}
