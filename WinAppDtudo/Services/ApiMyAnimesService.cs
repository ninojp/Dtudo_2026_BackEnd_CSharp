using System.Text;
using System.Text.Json;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Search;
using System.Net;

namespace WinAppDtudo.Services;

public class ApiMyAnimesService
{
    private static readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static string ApiBase => AppConfigurationService.ApiMyAnimesBaseUrl;

    static ApiMyAnimesService()
    {
        var handler = AppConfigurationService.CreateHttpClientHandler();

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiBase.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    public async Task<ApiAnimesBuscaResult> BuscarAnimesPorTituloAsync(string query, int page = 1, int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new ApiAnimesBuscaResult
            {
                CurrentPage = 1,
                TotalPages = 1,
                HasNextPage = false,
                TotalResults = 0,
                Results = []
            };

        var termo = AnimeSearchTextNormalizer.Normalize(query);
        if (termo.IsEmpty)
            return new ApiAnimesBuscaResult
            {
                CurrentPage = 1,
                TotalPages = 1,
                HasNextPage = false,
                TotalResults = 0,
                Results = []
            };

        var todos = new List<ObterAnimeDto>();
        var skip = 0;
        const int take = 200;

        while (true)
        {
            var paginaAnimes = await ObterAnimesAsync(skip, take);
            if (paginaAnimes.Count == 0)
                break;

            todos.AddRange(paginaAnimes);

            if (paginaAnimes.Count < take)
                break;

            skip += take;
        }

        var filtrados = todos
            .Select(a => new
            {
                Anime = a,
                Score = CalcularScoreAnime(a, termo)
            })
            .Where(resultado => resultado.Score > 0)
            .OrderByDescending(resultado => resultado.Score)
            .ThenBy(resultado => resultado.Anime.Titulo)
            .ThenBy(resultado => resultado.Anime.MalId)
            .Select(resultado => resultado.Anime)
            .ToList();

        var totalResultados = filtrados.Count;
        var tamanhoPagina = Math.Max(1, pageSize);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalResultados / (double)tamanhoPagina));
        var paginaAtual = Math.Min(Math.Max(1, page), totalPaginas);

        return new ApiAnimesBuscaResult
        {
            Results = filtrados
                .Skip((paginaAtual - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .ToList(),
            CurrentPage = paginaAtual,
            TotalPages = totalPaginas,
            HasNextPage = paginaAtual < totalPaginas,
            TotalResults = totalResultados
        };
    }

    public async Task<ApiMyColecoesBuscaResult> BuscarMyAnimesPorTituloAsync(string query, int page = 1, int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new ApiMyColecoesBuscaResult
            {
                CurrentPage = 1,
                TotalPages = 1,
                HasNextPage = false,
                TotalResults = 0,
                Results = []
            };

        var termo = AnimeSearchTextNormalizer.Normalize(query);
        if (termo.IsEmpty)
            return new ApiMyColecoesBuscaResult
            {
                CurrentPage = 1,
                TotalPages = 1,
                HasNextPage = false,
                TotalResults = 0,
                Results = []
            };

        var todos = new List<ObterMyAnimeDto>();
        var skip = 0;
        const int take = 200;

        while (true)
        {
            var paginaMyAnimes = await ObterMyAnimesAsync(skip, take);
            if (paginaMyAnimes.Count == 0)
                break;

            todos.AddRange(paginaMyAnimes);

            if (paginaMyAnimes.Count < take)
                break;

            skip += take;
        }

        var filtrados = todos
            .Where(m => AnimeSearchTextNormalizer.Normalize(m.Titulo).Matches(termo))
            .OrderBy(m => m.Titulo)
            .ThenBy(m => m.Id)
            .ToList();

        var totalResultados = filtrados.Count;
        var tamanhoPagina = Math.Max(1, pageSize);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalResultados / (double)tamanhoPagina));
        var paginaAtual = Math.Min(Math.Max(1, page), totalPaginas);

        var paginaResultados = filtrados
            .Skip((paginaAtual - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToList();

        return new ApiMyColecoesBuscaResult
        {
            Results = paginaResultados,
            CurrentPage = paginaAtual,
            TotalPages = totalPaginas,
            HasNextPage = paginaAtual < totalPaginas,
            TotalResults = totalResultados
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

    public async Task AssociarAnimeAoMyAnimeAsync(int malId, int myAnimeId)
    {
        var anime = await ObterAnimePorMalIdAsync(malId);
        if (anime is null)
            return;

        anime.MyAnimeID = myAnimeId;
        var content = SerializarJson(anime);
        using var response = await _httpClient.PutAsync($"apiLocal/Anime/{malId}", content);
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

    public async Task<ConflitoTituloAnimeDto?> BuscarConflitoDeTituloAsync(AdicionaAnimeDto dto)
    {
        var content = SerializarJson(dto);
        using var response = await _httpClient.PostAsync("apiLocal/Anime/conflito-titulo", content);

        if (response.StatusCode == HttpStatusCode.NoContent)
            return null;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ConflitoTituloAnimeDto>(json, _jsonOptions);
    }

    public async Task AtualizarAnimeAsync(int malId, AdicionaAnimeDto dto)
    {
        var content = SerializarJson(dto);
        using var response = await _httpClient.PutAsync($"apiLocal/Anime/{malId}", content);
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

    private static int CalcularScoreAnime(ObterAnimeDto anime, AnimeSearchText termo)
    {
        var melhorScore = 0;

        foreach (var campo in ObterCamposBuscaAnime(anime))
        {
            var textoNormalizado = AnimeSearchTextNormalizer.Normalize(campo.Texto);
            if (!textoNormalizado.Matches(termo)) continue;

            var score = campo.Peso;
            if (textoNormalizado.Value == termo.Value) score += 50;
            if (textoNormalizado.Value.StartsWith(termo.Value, StringComparison.Ordinal)) score += 25;
            if (textoNormalizado.CompactValue == termo.CompactValue) score += 20;

            melhorScore = Math.Max(melhorScore, score);
        }

        return melhorScore;
    }

    private static IEnumerable<(string? Texto, int Peso)> ObterCamposBuscaAnime(ObterAnimeDto anime)
    {
        yield return (anime.Titulo, 100);
        yield return (anime.Title, 95);
        yield return (anime.TitleEnglish, 90);
        yield return (anime.TitleJapanese, 90);

        foreach (var sinonimo in anime.TitleSynonyms ?? [])
            yield return (sinonimo, 80);

        foreach (var subTitulo in anime.SubTitulos ?? [])
            yield return (subTitulo, 70);
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
