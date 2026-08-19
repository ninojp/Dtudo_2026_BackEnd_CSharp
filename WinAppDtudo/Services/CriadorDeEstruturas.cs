using System.Security.Cryptography;
using System.Text;
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
    private readonly IFileStorageApiClient _fileStorageApiClient;
    private readonly IAnimeCoverDownloader _coverDownloader;

    public CriadorDeEstruturas(
        IFileStorageApiClient? fileStorageApiClient = null,
        IAnimeCoverDownloader? coverDownloader = null)
    {
        _fileStorageApiClient = fileStorageApiClient ?? new FileStorageApiClient();
        _coverDownloader = coverDownloader ?? new AnimeCoverDownloader();
    }

    public async Task<CriacaoEstruturaResultado> CriarEstruturaAsync(
        ObterMyAnimeDto myAnime,
        IReadOnlyCollection<ObterAnimeDto> animes,
        IProgress<ProgressoExportacao>? progresso = null,
        string? destinationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(myAnime);
        ArgumentNullException.ThrowIfNull(animes);

        var animesOrdenados = animes
            .Where(anime => anime.MalId > 0)
            .GroupBy(anime => anime.MalId)
            .Select(grupo => grupo.First())
            .OrderBy(anime => anime.Year ?? int.MaxValue)
            .ThenBy(anime => anime.Titulo)
            .ToList();

        if (myAnime.Id <= 0 || animesOrdenados.Count == 0)
            throw new ArgumentException("A coleção e os animes devem possuir IDs válidos.");

        progresso?.Report(new ProgressoExportacao
        {
            PercentualConcluido = 0,
            Mensagem = "Preparando destinos lógicos na ApiFileStorage..."
        });

        var plano = await _fileStorageApiClient.PrepareExportAsync(
            myAnime.Id,
            myAnime.Titulo,
            animesOrdenados
                .Select(anime => new WinAppStorageExportAnime(
                    anime.MalId,
                    anime.Year,
                    anime.Titulo,
                    anime.Type))
                .ToArray(),
            destinationId,
            cancellationToken);
        var objetosPorMalId = plano.Items.ToDictionary(item => item.MalId);
        var resultado = new CriacaoEstruturaResultado
        {
            TotalPastasCriadas = plano.Items.Count
        };

        for (var indice = 0; indice < animesOrdenados.Count; indice++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var anime = animesOrdenados[indice];
            var percentual = (int)Math.Round(
                ((indice + 1) / (double)animesOrdenados.Count) * 100,
                MidpointRounding.AwayFromZero);

            if (!objetosPorMalId.TryGetValue(anime.MalId, out var destino))
            {
                resultado.Erros.Add($"Destino lógico não preparado para o anime {anime.MalId} - {anime.Titulo}.");
                Reportar(progresso, percentual, $"Destino ausente para {anime.MalId}: {anime.Titulo}");
                continue;
            }

            Reportar(progresso, Math.Max(0, percentual - 1), $"Baixando capa {indice + 1}/{animesOrdenados.Count}: {anime.Titulo}");
            var imagem = await _coverDownloader.DownloadJpegAsync(
                anime.ImagensUrlMal.FirstOrDefault(),
                anime.MalId,
                cancellationToken);
            if (imagem is null)
            {
                resultado.Erros.Add($"Não foi possível baixar imagem para o anime {anime.MalId} - {anime.Titulo}.");
                Reportar(progresso, percentual, $"Capa indisponível para {anime.MalId}: {anime.Titulo}");
                continue;
            }

            try
            {
                Reportar(progresso, Math.Max(0, percentual - 1), $"Enviando capa {indice + 1}/{animesOrdenados.Count}: {anime.Titulo}");
                var importacao = await _fileStorageApiClient.ImportAsync(
                    destino.ObjectId,
                    $"{anime.MalId}.jpg",
                    "image/jpeg",
                    imagem,
                    BuildIdempotencyKey(myAnime.Id, anime.MalId, destino.ObjectId),
                    cancellationToken);
                resultado.TotalImagensSalvas++;
                if (importacao.Replayed)
                    resultado.TotalImagensRepetidas++;

                Reportar(
                    progresso,
                    percentual,
                    importacao.Replayed
                        ? $"Capa já existente, operação reconciliada: {anime.Titulo}"
                        : $"Capa salva com segurança: {anime.Titulo}");
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
            {
                resultado.Erros.Add($"Falha ao enviar imagem do anime {anime.MalId} - {anime.Titulo}: {exception.Message}");
                Reportar(progresso, percentual, $"Falha no envio de {anime.MalId}: {anime.Titulo}");
            }
        }

        progresso?.Report(new ProgressoExportacao
        {
            PercentualConcluido = 100,
            Mensagem = resultado.Erros.Count == 0
                ? "Exportação finalizada na ApiFileStorage."
                : $"Exportação finalizada com {resultado.Erros.Count} ocorrência(s)."
        });
        return resultado;
    }

    private static string BuildIdempotencyKey(int myAnimeId, int malId, string objectId)
    {
        var objectHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(objectId)))[..16];
        return $"export-{myAnimeId}-{malId}-{objectHash}";
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
