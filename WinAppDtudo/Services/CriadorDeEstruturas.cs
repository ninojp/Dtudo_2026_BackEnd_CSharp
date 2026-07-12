using LibDtudo.Shared.Dtos;

namespace WinAppDtudo.Services;

public class CriadorDeEstruturas
{
    private readonly MyAnimeListApiService _myAnimeListApiService = new();

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(120)
    };

    public async Task<CriacaoEstruturaResultado> CriarEstruturaAsync(
        ObterMyAnimeDto myAnime,
        IReadOnlyCollection<ObterAnimeDto> animes,
        string diretorioBase,
        CancellationToken cancellationToken = default)
    {
        if (myAnime is null) throw new ArgumentNullException(nameof(myAnime));
        if (animes is null) throw new ArgumentNullException(nameof(animes));
        if (string.IsNullOrWhiteSpace(diretorioBase)) throw new ArgumentException("Diretório inválido.", nameof(diretorioBase));

        var nomePastaRaiz = SanitizarNome(myAnime.Titulo);
        var pastaRaiz = Path.Combine(diretorioBase, nomePastaRaiz);
        Directory.CreateDirectory(pastaRaiz);

        var resultado = new CriacaoEstruturaResultado
        {
            PastaRaiz = pastaRaiz
        };

        foreach (var anime in animes.OrderBy(a => a.Year ?? int.MaxValue).ThenBy(a => a.Titulo))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nomePastaAnime = MontarNomePastaAnime(anime);
            var pastaAnime = Path.Combine(pastaRaiz, nomePastaAnime);
            Directory.CreateDirectory(pastaAnime);
            resultado.TotalPastasCriadas++;

            var caminhoImagem = Path.Combine(pastaAnime, $"{anime.MalId}.jpg");
            var imagemSalva = await TentarSalvarImagemAsync(anime, caminhoImagem, cancellationToken);
            if (imagemSalva)
            {
                resultado.TotalImagensSalvas++;
            }
            else
            {
                resultado.Erros.Add($"Não foi possível baixar imagem para o anime {anime.MalId} - {anime.Titulo}.");
            }
        }

        return resultado;
    }

    private async Task<bool> TentarSalvarImagemAsync(ObterAnimeDto anime, string caminhoImagem, CancellationToken cancellationToken)
    {
        var urls = new List<string>();

        try
        {
            var detalhesMyAnimeList = await _myAnimeListApiService.BuscarPorIdAsync(anime.MalId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(detalhesMyAnimeList?.Images?.Jpg?.LargeImageUrl)) urls.Add(detalhesMyAnimeList.Images.Jpg.LargeImageUrl);
            if (!string.IsNullOrWhiteSpace(detalhesMyAnimeList?.Images?.Jpg?.ImageUrl)) urls.Add(detalhesMyAnimeList.Images.Jpg.ImageUrl);
            if (!string.IsNullOrWhiteSpace(detalhesMyAnimeList?.Images?.Jpg?.SmallImageUrl)) urls.Add(detalhesMyAnimeList.Images.Jpg.SmallImageUrl);
        }
        catch
        {
            // Fallback para URLs já persistidas no anime local.
        }

        urls.AddRange(anime.ImagensUrlMal);

        foreach (var url in urls.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct())
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode) continue;

                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType is null || !mediaType.StartsWith("image", StringComparison.OrdinalIgnoreCase)) continue;

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (bytes.Length == 0) continue;

                await File.WriteAllBytesAsync(caminhoImagem, bytes, cancellationToken);
                return true;
            }
            catch
            {
                // tenta próxima URL
            }
        }

        return false;
    }

    private static string MontarNomePastaAnime(ObterAnimeDto anime)
    {
        var ano = anime.Year?.ToString() ?? "0000";
        var titulo = !string.IsNullOrWhiteSpace(anime.Titulo)
            ? anime.Titulo
            : !string.IsNullOrWhiteSpace(anime.Title)
                ? anime.Title
                : $"Anime_{anime.MalId}";
        var tipo = !string.IsNullOrWhiteSpace(anime.Type) ? anime.Type : "TipoDesconhecido";

        return SanitizarNome($"{ano} {titulo} - {tipo}");
    }

    private static string SanitizarNome(string nome)
    {
        var nomeLimpo = nome.Trim();

        foreach (var c in Path.GetInvalidFileNameChars())
            nomeLimpo = nomeLimpo.Replace(c, ' ');

        while (nomeLimpo.Contains("  "))
            nomeLimpo = nomeLimpo.Replace("  ", " ");

        return string.IsNullOrWhiteSpace(nomeLimpo) ? "SemNome" : nomeLimpo;
    }
}

public class CriacaoEstruturaResultado
{
    public string PastaRaiz { get; set; } = string.Empty;
    public int TotalPastasCriadas { get; set; }
    public int TotalImagensSalvas { get; set; }
    public List<string> Erros { get; set; } = [];
}
