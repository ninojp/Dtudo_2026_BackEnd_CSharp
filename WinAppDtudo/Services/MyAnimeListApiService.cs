using LibDtudo.Shared.Dtos.MyAnimeList;
using System.Net.Sockets;
using System.Text.Json;

namespace WinAppDtudo.Services;

/// <summary>
/// Serviço HTTP que consome exclusivamente a API local ApiMyAnimeList.
/// A API local mantém o contrato de resposta compatível com os modelos usados pelos cards.
/// </summary>
public sealed class MyAnimeListApiService
{
    private static readonly HttpClient HttpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly WinAppAuthenticationService _authenticationService;
    private readonly ApiMyAnimeListStartupService _startupService;

    public MyAnimeListApiService(
        WinAppAuthenticationService? authenticationService = null,
        ApiMyAnimeListStartupService? startupService = null)
    {
        _authenticationService = authenticationService ?? new WinAppAuthenticationService();
        _startupService = startupService ?? new ApiMyAnimeListStartupService();
    }

    public static string ApiBase => AppConfigurationService.ApiMyAnimeListBaseUrl;

    static MyAnimeListApiService()
    {
        var handler = AppConfigurationService.CreateHttpClientHandler();

        HttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiBase.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    public async Task<AnimeSearchResult> BuscarPorNomeAsync(string query, int page = 1, CancellationToken cancellationToken = default)
    {
        var url = $"ApiMyAnimeList/search?q={Uri.EscapeDataString(query)}&page={page}";
        HttpResponseMessage response;
        try
        {
            response = await SendAuthenticatedGetAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex) when (EhConexaoRecusada(ex))
        {
            await _startupService.EnsureReadyAsync(cancellationToken);
            response = await SendAuthenticatedGetAsync(url, cancellationToken);
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<AnimeSearchResult>(json, JsonOptions) ?? new AnimeSearchResult();
        }
    }

    public Task<AnimeDetails?> BuscarPorIdAsync(int malId, CancellationToken cancellationToken = default)
        => GetAsync<AnimeDetails>($"ApiMyAnimeList/{malId}", cancellationToken);

    public async Task<List<AnimeRelationGroup>> BuscarRelacoesAsync(int malId, CancellationToken cancellationToken = default)
        => await GetAsync<List<AnimeRelationGroup>>($"ApiMyAnimeList/{malId}/relations", cancellationToken) ?? [];

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await SendAuthenticatedGetAsync(url, cancellationToken);
        }
        catch (HttpRequestException ex) when (EhConexaoRecusada(ex))
        {
            await _startupService.EnsureReadyAsync(cancellationToken);
            response = await SendAuthenticatedGetAsync(url, cancellationToken);
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
    }

    private Task<HttpResponseMessage> SendAuthenticatedGetAsync(
        string url,
        CancellationToken cancellationToken) =>
        _authenticationService.SendAuthenticatedAsync(
            HttpClient,
            _ => new HttpRequestMessage(HttpMethod.Get, url),
            cancellationToken);

    private static bool EhConexaoRecusada(HttpRequestException exception)
        => exception.InnerException is SocketException socketException
           && socketException.SocketErrorCode == SocketError.ConnectionRefused;
}
