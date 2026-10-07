using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.MyAnimeList;

namespace WinAppDtudo.Services;

internal sealed class DBLocalAnimeSearchSource(ApiMyAnimesService apiService) : IAnimeSearchSource
{
    public string Name => "DB_Local";
    public bool UsarFallbackMyAnimeList => false;

    public string? ValidarConsulta(string consulta) => string.IsNullOrWhiteSpace(consulta)
        ? "Digite o nome para buscar no DB_Local."
        : null;

    public string ObterStatusCarregando(string consulta) => "⏳ Buscando animes no DB_Local...";

    public string ObterStatusResultado(AnimeSearchResult resultado, string consulta) =>
        resultado.TotalResults == 0 || resultado.Results.Count == 0
            ? "Nenhum anime encontrado no DB_Local."
            : $"✅ Busca no DB_Local concluída. {resultado.TotalResults:N0} resultado(s).";

    public async Task<AnimeSearchResult> BuscarAsync(
        string consulta,
        int pagina,
        CancellationToken cancellationToken)
    {
        if (ValidarConsulta(consulta) is { } aviso)
            throw new ArgumentException(aviso, nameof(consulta));

        var resultado = await apiService.BuscarAnimesPorTituloAsync(
            consulta, pagina, 20, cancellationToken);
        return new AnimeSearchResult
        {
            Results = resultado.Results.Select(MapearParaCard).ToList(),
            CurrentPage = resultado.CurrentPage,
            TotalPages = resultado.TotalPages,
            HasNextPage = resultado.HasNextPage,
            TotalResults = resultado.TotalResults
        };
    }

    public AnimeSearchFeedback DescreverErro(Exception exception) => exception switch
    {
        HttpRequestException => new(
            "❌ Erro de conexão com ApiMyAnimes (DB_Local).",
            $"Não foi possível conectar à ApiMyAnimes em:\n{ApiMyAnimesService.ApiBase}\n\nDetalhes: {exception.Message}",
            "Erro de Conexão",
            MessageBoxIcon.Error),
        OperationCanceledException => new(
            "⚠️ A busca no DB_Local excedeu o tempo limite.",
            "A ApiMyAnimes demorou para responder. Tente novamente em instantes.",
            "DB_Local indisponível",
            MessageBoxIcon.Warning),
        _ => new(
            "❌ Erro ao buscar no DB_Local.",
            $"Erro ao buscar animes locais:\n\n{exception.Message}",
            "Erro",
            MessageBoxIcon.Error)
    };

    private static AnimeSearchCard MapearParaCard(ObterAnimeDto anime) => new()
    {
        MalId = anime.MalId,
        Title = !string.IsNullOrWhiteSpace(anime.Titulo) ? anime.Titulo : anime.Title,
        TitleEnglish = anime.TitleEnglish,
        TitleJapanese = anime.TitleJapanese,
        TitleSynonyms = anime.TitleSynonyms,
        Type = anime.Type ?? "Anime local",
        Score = anime.Score,
        Year = anime.Year,
        ImageUrl = anime.ImagensUrlMal?.FirstOrDefault()
    };
}
