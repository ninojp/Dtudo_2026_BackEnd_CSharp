using LibDtudo.Shared.Dtos;

namespace WinAppDtudo.Services;

public interface IAnimeCoverDownloader
{
    Task<byte[]?> DownloadJpegAsync(
        string? primaryUrl,
        int malId,
        CancellationToken cancellationToken = default);
}

public sealed class AnimeCoverDownloader : IAnimeCoverDownloader
{
    public Task<byte[]?> DownloadJpegAsync(
        string? primaryUrl,
        int malId,
        CancellationToken cancellationToken = default)
        => ImageLoaderService.DownloadAnimeCoverJpegAsync(primaryUrl, malId, cancellationToken);
}

public class CriadorDeEstruturas
{
    private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private readonly IAnimeCoverDownloader _coverDownloader;

    public CriadorDeEstruturas(IAnimeCoverDownloader? coverDownloader = null)
    {
        _coverDownloader = coverDownloader ?? new AnimeCoverDownloader();
    }

    /// <summary>
    /// Monta o caminho completo da pasta raiz da coleção (MyAnime) dentro da pasta de destino
    /// escolhida, aplicando a mesma sanitização de nomes usada pela ApiFileStorage.
    /// </summary>
    public static string ObterCaminhoColecao(string pastaDestino, string? tituloMyAnime)
    {
        if (string.IsNullOrWhiteSpace(pastaDestino))
            throw new ArgumentException("A pasta de destino deve ser informada.", nameof(pastaDestino));
        if (string.IsNullOrWhiteSpace(tituloMyAnime))
            throw new ArgumentException("O título do MyAnime deve ser informado.", nameof(tituloMyAnime));

        return Path.Combine(pastaDestino, SanitizeName(tituloMyAnime));
    }

    /// <summary>Indica se já existe uma estrutura com o mesmo nome da coleção na pasta de destino.</summary>
    public static bool EstruturaJaExiste(string pastaDestino, string? tituloMyAnime)
        => Directory.Exists(ObterCaminhoColecao(pastaDestino, tituloMyAnime));

    /// <summary>
    /// Salva a estrutura de pastas e capas diretamente no disco local, sem passar pela ApiFileStorage.
    /// NUNCA apaga nem substitui conteúdo existente: pastas são apenas criadas quando ausentes e
    /// arquivos já presentes são sempre preservados (escrita com FileMode.CreateNew).
    /// </summary>
    public async Task<CriacaoEstruturaResultado> CriarEstruturaLocalAsync(
        ObterMyAnimeDto myAnime,
        IReadOnlyCollection<ObterAnimeDto> animes,
        string pastaDestino,
        ModoSalvamentoLocal modo,
        IProgress<ProgressoExportacao>? progresso = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(myAnime);
        ArgumentNullException.ThrowIfNull(animes);
        if (string.IsNullOrWhiteSpace(pastaDestino))
            throw new ArgumentException("A pasta de destino deve ser informada.", nameof(pastaDestino));
        if (!Path.IsPathFullyQualified(pastaDestino))
            throw new ArgumentException("A pasta de destino deve ser um caminho local completo.", nameof(pastaDestino));

        var animesOrdenados = animes
            .Where(anime => anime.MalId > 0)
            .GroupBy(anime => anime.MalId)
            .Select(grupo => grupo.First())
            .OrderBy(anime => anime.Year ?? int.MaxValue)
            .ThenBy(anime => anime.Titulo)
            .ToList();

        if (myAnime.Id <= 0 || animesOrdenados.Count == 0)
            throw new ArgumentException("A coleção e os animes devem possuir IDs válidos.");

        var caminhoColecao = ObterCaminhoColecao(pastaDestino, myAnime.Titulo);
        var resultado = new CriacaoEstruturaResultado();

        Reportar(progresso, 0, modo == ModoSalvamentoLocal.AdicionarNovos
            ? $"Analisando a estrutura já existente em: {caminhoColecao}"
            : $"Preparando a nova estrutura em: {caminhoColecao}");

        var nomesUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pastasPorMalId = animesOrdenados.ToDictionary(
            anime => anime.MalId,
            anime => BuildUniqueAnimeFolder(anime, nomesUsados));

        Directory.CreateDirectory(caminhoColecao);

        for (var indice = 0; indice < animesOrdenados.Count; indice++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var anime = animesOrdenados[indice];
            var percentual = (int)Math.Round(
                ((indice + 1) / (double)animesOrdenados.Count) * 100,
                MidpointRounding.AwayFromZero);
            var pastaAnime = Path.Combine(caminhoColecao, pastasPorMalId[anime.MalId]);
            var caminhoArquivo = Path.Combine(pastaAnime, $"{anime.MalId}.jpg");

            if (File.Exists(caminhoArquivo))
            {
                resultado.TotalImagensRepetidas++;
                Reportar(progresso, percentual,
                    $"Capa {indice + 1} de {animesOrdenados.Count} já existente, preservada sem alterações: {anime.Titulo}");
                continue;
            }

            if (!Directory.Exists(pastaAnime))
            {
                Directory.CreateDirectory(pastaAnime);
                resultado.TotalPastasCriadas++;
                Reportar(progresso, Math.Max(0, percentual - 1),
                    $"Nova subpasta criada: {pastasPorMalId[anime.MalId]}");
            }

            byte[]? imagem;
            try
            {
                Reportar(progresso, Math.Max(0, percentual - 1),
                    $"Baixando capa {indice + 1} de {animesOrdenados.Count}: {anime.Titulo}");
                imagem = await _coverDownloader.DownloadJpegAsync(
                    anime.ImagensUrlMal.FirstOrDefault(),
                    anime.MalId,
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                resultado.Erros.Add($"Falha ao baixar a capa do anime {anime.MalId} - {anime.Titulo}: {exception.Message}");
                Reportar(progresso, percentual, $"Falha no download de {anime.MalId}: {anime.Titulo}");
                continue;
            }

            if (imagem is null)
            {
                resultado.Erros.Add($"Não foi possível baixar imagem para o anime {anime.MalId} - {anime.Titulo}.");
                Reportar(progresso, percentual, $"Capa indisponível para {anime.MalId}: {anime.Titulo}");
                continue;
            }

            try
            {
                Reportar(progresso, Math.Max(0, percentual - 1),
                    $"Salvando no disco a capa {indice + 1} de {animesOrdenados.Count}: {anime.Titulo}");
                await EscreverArquivoSemSubstituirAsync(caminhoArquivo, imagem, cancellationToken);
                resultado.TotalImagensSalvas++;
                Reportar(progresso, percentual, $"Capa salva com segurança: {anime.Titulo}");
            }
            catch (IOException) when (File.Exists(caminhoArquivo))
            {
                // O arquivo surgiu entre a verificação e a escrita: preservar sempre o conteúdo existente.
                resultado.TotalImagensRepetidas++;
                Reportar(progresso, percentual,
                    $"Capa {indice + 1} de {animesOrdenados.Count} já existente, preservada sem alterações: {anime.Titulo}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                resultado.Erros.Add($"Falha ao gravar a capa do anime {anime.MalId} - {anime.Titulo}: {exception.Message}");
                Reportar(progresso, percentual, $"Falha ao gravar {anime.MalId}: {anime.Titulo}");
            }
        }

        Reportar(progresso, 100, resultado.Erros.Count == 0
            ? "Salvamento da estrutura concluído com segurança."
            : $"Salvamento da estrutura concluído com {resultado.Erros.Count} ocorrência(s).");
        return resultado;
    }

    private static void Reportar(
        IProgress<ProgressoExportacao>? progresso,
        int percentual,
        string mensagem)
        => progresso?.Report(new ProgressoExportacao
        {
            PercentualConcluido = Math.Clamp(percentual, 0, 100),
            Mensagem = mensagem
        });

    /// <summary>
    /// Grava o arquivo com FileMode.CreateNew, garantindo que um arquivo já existente
    /// NUNCA seja sobrescrito (lança IOException caso ele exista).
    /// </summary>
    private static async Task EscreverArquivoSemSubstituirAsync(
        string caminhoArquivo,
        byte[] conteudo,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            caminhoArquivo,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        await stream.WriteAsync(conteudo, cancellationToken);
    }

    private static string BuildUniqueAnimeFolder(
        ObterAnimeDto anime,
        HashSet<string> usedNames)
    {
        var year = anime.Year?.ToString() ?? "0000";
        var title = string.IsNullOrWhiteSpace(anime.Titulo)
            ? $"Anime_{anime.MalId}"
            : anime.Titulo;
        var type = string.IsNullOrWhiteSpace(anime.Type)
            ? "TipoDesconhecido"
            : anime.Type;
        var baseName = SanitizeName($"{year} {title} - {type}");
        var name = baseName;
        var suffix = 2;

        while (!usedNames.Add(name))
        {
            var suffixText = $" ({suffix++})";
            name = SanitizeNameWithLimit(baseName, 255 - suffixText.Length) + suffixText;
        }

        return name;
    }

    private static string SanitizeName(string name)
    {
        var sanitizedName = SanitizeNameWithLimit(name, 255);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedName);
        if (ReservedWindowsNames.Contains(nameWithoutExtension))
        {
            sanitizedName = $"_{sanitizedName}";
        }

        return string.IsNullOrWhiteSpace(sanitizedName) ? "SemNome" : sanitizedName;
    }

    private static string SanitizeNameWithLimit(string name, int limit)
    {
        var sanitizedName = name.Trim();
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            sanitizedName = sanitizedName.Replace(character, ' ');
        }

        while (sanitizedName.Contains("  ", StringComparison.Ordinal))
        {
            sanitizedName = sanitizedName.Replace("  ", " ", StringComparison.Ordinal);
        }

        sanitizedName = sanitizedName.Trim().TrimEnd('.', ' ');
        return sanitizedName.Length <= limit
            ? sanitizedName
            : sanitizedName[..limit].TrimEnd('.', ' ');
    }
}

/// <summary>Modo de salvamento local da estrutura quando o destino é escolhido pelo usuário.</summary>
public enum ModoSalvamentoLocal
{
    /// <summary>A estrutura ainda não existe no destino: tudo será criado.</summary>
    CriarNova,

    /// <summary>
    /// A estrutura já existe no destino: apenas subpastas e capas ausentes são adicionadas,
    /// sem apagar nem substituir nenhum conteúdo existente.
    /// </summary>
    AdicionarNovos
}

public class CriacaoEstruturaResultado
{
    public int TotalPastasCriadas { get; set; }

    public int TotalImagensSalvas { get; set; }

    public int TotalImagensRepetidas { get; set; }

    public List<string> Erros { get; set; } = [];
}

public class ProgressoExportacao
{
    public int PercentualConcluido { get; init; }

    public string Mensagem { get; init; } = string.Empty;
}
