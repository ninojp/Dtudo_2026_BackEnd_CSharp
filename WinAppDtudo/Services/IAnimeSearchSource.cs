using LibDtudo.Shared.Dtos.MyAnimeList;

namespace WinAppDtudo.Services;

internal enum AnimeSearchSourceKind
{
    DBLocal,
    ApiMyAnimeList
}

internal interface IAnimeSearchSource
{
    string Name { get; }
    bool UsarFallbackMyAnimeList { get; }
    string? ValidarConsulta(string consulta);
    string ObterStatusCarregando(string consulta);
    string ObterStatusResultado(AnimeSearchResult resultado, string consulta);
    Task<AnimeSearchResult> BuscarAsync(string consulta, int pagina, CancellationToken cancellationToken);
    AnimeSearchFeedback DescreverErro(Exception exception);
}

internal sealed record AnimeSearchFeedback(
    string Status,
    string Mensagem,
    string Titulo,
    MessageBoxIcon Icone);
