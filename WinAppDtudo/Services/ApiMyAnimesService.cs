using System.Text;
using System.Text.Json;
using LibDtudo.Shared.Dtos;
using System.Net;

namespace WinAppDtudo.Services;

public class ApiMyAnimesService
{
    private static readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public const string ApiBase = "https://localhost:63980";

    static ApiMyAnimesService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiBase + "/"),
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    public async Task<List<ObterMyAnimeDto>> ObterMyAnimesAsync(int skip = 0, int take = 100)
    {
        using var response = await _httpClient.GetAsync($"apiLocal/MyAnime?skip={skip}&take={take}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ObterMyAnimeDto>>(json, _jsonOptions) ?? [];
    }

    public async Task<ObterMyAnimeDto?> ObterMyAnimePorIdAsync(int id)
    {
        using var response = await _httpClient.GetAsync($"apiLocal/MyAnime/{id}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ObterMyAnimeDto>(json, _jsonOptions);
    }

    public async Task<int?> AdicionarMyAnimeAsync(AdicionaMyAnimeDto dto)
    {
        var tituloNormalizado = dto.Titulo?.Trim() ?? string.Empty;
        var myAnimeExistente = await ObterMyAnimePorTituloAsync(tituloNormalizado);
        if (myAnimeExistente is not null)
            throw new HttpRequestException($"MyAnime '{tituloNormalizado}' já existe.", null, HttpStatusCode.Conflict);

        var content = SerializarJson(dto);
        using var response = await _httpClient.PostAsync("apiLocal/MyAnime", content);
        response.EnsureSuccessStatusCode();

        return ExtrairIdMyAnime(response);
    }

    public async Task AtualizarMyAnimeAsync(int id, AtualizaMyAnimeDto dto)
    {
        var content = SerializarJson(dto);
        using var response = await _httpClient.PutAsync($"apiLocal/MyAnime/{id}", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task AdicionarAnimeAsync(AdicionaAnimeDto dto)
    {
        var animeExistente = await ObterAnimePorMalIdAsync(dto.MalId);
        if (animeExistente is not null)
            throw new HttpRequestException($"Anime com MalId {dto.MalId} já existe.", null, HttpStatusCode.Conflict);

        var content = SerializarJson(dto);
        using var response = await _httpClient.PostAsync("apiLocal/Anime", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ObterAnimeDto>> ObterAnimesAsync(int skip = 0, int take = 100)
    {
        using var response = await _httpClient.GetAsync($"apiLocal/Anime?skip={skip}&take={take}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ObterAnimeDto>>(json, _jsonOptions) ?? [];
    }

    public async Task<ObterAnimeDto?> ObterAnimePorMalIdAsync(int malId)
    {
        using var response = await _httpClient.GetAsync($"apiLocal/Anime/{malId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ObterAnimeDto>(json, _jsonOptions);
    }

    public async Task<List<ObterAnimeDto>> ObterAnimesPorMyAnimeIdAsync(int myAnimeId, int take = 200)
    {
        var skip = 0;
        var filtrados = new List<ObterAnimeDto>();

        while (true)
        {
            var pagina = await ObterAnimesAsync(skip, take);
            if (pagina.Count == 0)
                break;

            filtrados.AddRange(pagina.Where(a => a.MyAnimeID == myAnimeId));

            if (pagina.Count < take)
                break;

            skip += take;
        }

        return filtrados
            .OrderBy(a => a.Year ?? int.MaxValue)
            .ThenBy(a => a.Titulo)
            .ToList();
    }

    public async Task<ApiMyAnimesBuscaResult> BuscarAnimesPorNomeAsync(string query, int page = 1, int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new ApiMyAnimesBuscaResult
            {
                CurrentPage = 1,
                TotalPages = 1,
                HasNextPage = false,
                TotalResults = 0,
                Results = []
            };

        var termo = query.Trim();
        var todos = new List<ObterAnimeDto>();
        var skip = 0;
        const int take = 200;

        while (true)
        {
            var pagina = await ObterAnimesAsync(skip, take);
            if (pagina.Count == 0)
                break;

            todos.AddRange(pagina);

            if (pagina.Count < take)
                break;

            skip += take;
        }

        var filtrados = todos
            .Where(a => ContemTermo(a, termo))
            .OrderBy(a => a.Titulo)
            .ThenBy(a => a.MalId)
            .ToList();

        var totalResultados = filtrados.Count;
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalResultados / (double)Math.Max(1, pageSize)));
        var paginaAtual = Math.Min(Math.Max(1, page), totalPaginas);

        var paginaResultados = filtrados
            .Skip((paginaAtual - 1) * Math.Max(1, pageSize))
            .Take(Math.Max(1, pageSize))
            .ToList();

        return new ApiMyAnimesBuscaResult
        {
            Results = paginaResultados,
            CurrentPage = paginaAtual,
            TotalPages = totalPaginas,
            HasNextPage = paginaAtual < totalPaginas,
            TotalResults = totalResultados
        };
    }

    private static bool ContemTermo(ObterAnimeDto anime, string termo)
    {
        bool Em(string? valor) => !string.IsNullOrWhiteSpace(valor)
            && valor.Contains(termo, StringComparison.OrdinalIgnoreCase);

        return Em(anime.Titulo)
            || Em(anime.Title)
            || Em(anime.TitleEnglish)
            || Em(anime.TitleJapanese)
            || anime.SubTitulos.Any(Em);
    }

    private static StringContent SerializarJson<T>(T dto)
    {
        return new StringContent(
            JsonSerializer.Serialize(dto),
            Encoding.UTF8,
            "application/json");
    }

    private static int? ExtrairIdMyAnime(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        if (location is null)
            return null;

        var ultimoSegmento = location.Segments.LastOrDefault()?.Trim('/');
        return int.TryParse(ultimoSegmento, out var id) ? id : null;
    }

    public async Task<ObterMyAnimeDto?> ObterMyAnimePorTituloAsync(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            return null;

        const int take = 200;
        var skip = 0;

        while (true)
        {
            var pagina = await ObterMyAnimesAsync(skip, take);
            if (pagina.Count == 0)
                return null;

            var existente = pagina.FirstOrDefault(item =>
                string.Equals(item.Titulo?.Trim(), titulo, StringComparison.OrdinalIgnoreCase));
            if (existente is not null)
                return existente;

            if (pagina.Count < take)
                return null;

            skip += take;
        }
    }
}
