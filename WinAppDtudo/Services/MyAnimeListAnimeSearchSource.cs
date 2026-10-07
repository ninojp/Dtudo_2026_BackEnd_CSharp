using System.Globalization;
using System.Net;
using System.Net.Sockets;
using LibDtudo.Shared.Dtos.MyAnimeList;

namespace WinAppDtudo.Services;

internal sealed class MyAnimeListAnimeSearchSource(MyAnimeListApiService apiService) : IAnimeSearchSource
{
    public string Name => "ApiMyAnimeList";
    public bool UsarFallbackMyAnimeList => true;

    public string? ValidarConsulta(string consulta)
    {
        var termo = consulta.Trim();
        if (string.IsNullOrWhiteSpace(termo))
            return "Digite o nome ou ID para buscar na ApiMyAnimeList.";

        if (EhBuscaPorId(termo) && (!int.TryParse(termo, out var malId) || malId is < 1 or > 100000))
            return "Informe um ID de anime entre 1 e 100000.";

        return null;
    }

    public string ObterStatusCarregando(string consulta) => EhBuscaPorId(consulta.Trim())
        ? "⏳ Buscando ID na ApiMyAnimeList..."
        : "⏳ Buscando na ApiMyAnimeList...";

    public string ObterStatusResultado(AnimeSearchResult resultado, string consulta)
    {
        if (resultado.TotalResults == 0 || resultado.Results.Count == 0)
            return "Nenhum anime encontrado na ApiMyAnimeList.";

        return EhBuscaPorId(consulta.Trim())
            ? "✅ Busca por ID na ApiMyAnimeList concluída."
            : $"✅ Busca na ApiMyAnimeList concluída. {resultado.TotalResults:N0} resultado(s).";
    }

    public async Task<AnimeSearchResult> BuscarAsync(
        string consulta,
        int pagina,
        CancellationToken cancellationToken)
    {
        if (ValidarConsulta(consulta) is { } aviso)
            throw new ArgumentException(aviso, nameof(consulta));

        var termo = consulta.Trim();
        if (!EhBuscaPorId(termo))
            return await apiService.BuscarPorNomeAsync(termo, pagina, cancellationToken);

        AnimeDetails? anime;
        try
        {
            anime = await apiService.BuscarPorIdAsync(
                int.Parse(termo, CultureInfo.InvariantCulture), cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            anime = null;
        }

        return new AnimeSearchResult
        {
            Results = anime is null ? [] : [MapearParaCard(anime)],
            CurrentPage = 1,
            TotalPages = 1,
            HasNextPage = false,
            TotalResults = anime is null ? 0 : 1
        };
    }

    public AnimeSearchFeedback DescreverErro(Exception exception) => exception switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.GatewayTimeout } => Indisponivel(
            "A MyAnimeList demorou para responder. Tente novamente em instantes."),
        HttpRequestException { StatusCode: null or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable } => Indisponivel(
            "A MyAnimeList está temporariamente indisponível. Sua aplicação continua funcionando; tente novamente em instantes."),
        HttpRequestException => Indisponivel(
            $"Não foi possível concluir a consulta à MyAnimeList. Tente novamente em instantes.\n\nDetalhes: {exception.Message}"),
        SocketException => Indisponivel(
            "A conexão com a MyAnimeList foi interrompida. Tente novamente em instantes."),
        OperationCanceledException => Indisponivel(
            "A consulta à MyAnimeList excedeu o tempo limite. Tente novamente em instantes."),
        _ => new(
            "❌ Erro ao buscar na ApiMyAnimeList.",
            $"Erro ao buscar animes na ApiMyAnimeList:\n\n{exception.Message}",
            "Erro",
            MessageBoxIcon.Error)
    };

    private static bool EhBuscaPorId(string consulta) =>
        consulta.Length > 0 && consulta.All(char.IsAsciiDigit);

    private static AnimeSearchFeedback Indisponivel(string mensagem) => new(
        "⚠️ ApiMyAnimeList temporariamente indisponível.",
        mensagem,
        "MyAnimeList indisponível",
        MessageBoxIcon.Warning);

    private static AnimeSearchCard MapearParaCard(AnimeDetails anime) => new()
    {
        MalId = anime.MalId,
        Url = anime.Url,
        Title = anime.Title,
        TitleEnglish = anime.TitleEnglish,
        TitleJapanese = anime.TitleJapanese,
        TitleSynonyms = anime.TitleSynonyms,
        ImageUrl = anime.Images?.Jpg?.LargeImageUrl ?? anime.Images?.Jpg?.ImageUrl,
        Type = anime.Type,
        Episodes = anime.Episodes,
        Status = anime.Status,
        Score = anime.Score,
        Year = anime.Year,
        Genres = anime.Genres
    };
}
