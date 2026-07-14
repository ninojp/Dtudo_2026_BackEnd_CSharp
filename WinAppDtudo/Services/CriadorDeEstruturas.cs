using LibDtudo.Shared.Dtos;

namespace WinAppDtudo.Services;

public class CriadorDeEstruturas
{
    public async Task<CriacaoEstruturaResultado> CriarEstruturaAsync(
        ObterMyAnimeDto myAnime,
        IReadOnlyCollection<ObterAnimeDto> animes,
        string diretorioBase,
        CancellationToken cancellationToken = default)
    {
        if (myAnime is null) throw new ArgumentNullException(nameof(myAnime));
        if (animes is null) throw new ArgumentNullException(nameof(animes));
        if (string.IsNullOrWhiteSpace(diretorioBase)) throw new ArgumentException("Diretório inválido.", nameof(diretorioBase));

        var pastaRaiz = ObterCaminhoPastaRaiz(myAnime, diretorioBase);
        if (Directory.Exists(pastaRaiz))
            throw new InvalidOperationException($"A pasta já existe e não pode ser sobrescrita: {pastaRaiz}");

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

    public static string ObterCaminhoPastaRaiz(ObterMyAnimeDto myAnime, string diretorioBase)
    {
        if (myAnime is null) throw new ArgumentNullException(nameof(myAnime));
        if (string.IsNullOrWhiteSpace(diretorioBase)) throw new ArgumentException("Diretório inválido.", nameof(diretorioBase));

        return Path.Combine(diretorioBase, SanitizarNome(myAnime.Titulo));
    }

    private async Task<bool> TentarSalvarImagemAsync(ObterAnimeDto anime, string caminhoImagem, CancellationToken cancellationToken)
    {
        var imagem = await ImageLoaderService.DownloadAnimeCoverAsync(
            anime.ImagensUrlMal.FirstOrDefault(),
            anime.MalId,
            cancellationToken);

        if (imagem is null)
            return false;

        using (imagem)
        {
            imagem.Save(caminhoImagem, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        return true;
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
