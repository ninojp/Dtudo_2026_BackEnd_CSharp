using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.MyAnimeList;
using System.Net;

namespace WinAppDtudo.Services;

public class ImportadorAnimesMyAnimeService
{
    private const int MaxTentativasApiMyAnimeList = 3;
    private static readonly TimeSpan DelayTentativaApiMyAnimeList = TimeSpan.FromSeconds(2);

    private readonly ApiMyAnimesService _apiMyAnimesService = new();
    private readonly MyAnimeListApiService _myAnimeListApiService = new();

    public async Task<ResultadoImportacaoAnimes> ImportarAsync(
        int myAnimeId,
        string tituloMyAnime,
        IReadOnlyCollection<int> malIds,
        IProgress<ProgressoImportacaoAnimes>? progresso = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = new ResultadoImportacaoAnimes
        {
            MyAnimeId = myAnimeId,
            TituloMyAnime = tituloMyAnime
        };

        var ids = malIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (ids.Count == 0)
        {
            progresso?.Report(new ProgressoImportacaoAnimes
            {
                Percentual = 100,
                Mensagem = "Nenhum anime para importar."
            });
            return resultado;
        }

        for (var indice = 0; indice < ids.Count; indice++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var malId = ids[indice];
            var detalhes = await BuscarAnimeComRetryAsync(tituloMyAnime, malId, resultado.ErrosDetalhados, cancellationToken);
            if (detalhes is null)
            {
                var salvouFallback = await TentarSalvarModoDegradacaoAsync(myAnimeId, tituloMyAnime, malId, resultado);
                if (!salvouFallback)
                    resultado.AnimesComFalha++;

                ReportarProgresso();
                continue;
            }

            try
            {
                var dtoAnime = ConversorAnimeDtoService.CriarAdicionaAnimeDto(detalhes, myAnimeId);
                await _apiMyAnimesService.AdicionarAnimeAsync(dtoAnime);
                resultado.AnimesSalvos++;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                try
                {
                    await _apiMyAnimesService.AssociarAnimeAoMyAnimeAsync(malId, myAnimeId);
                    resultado.AnimesSalvos++;
                }
                catch (Exception exAssociacao)
                {
                    resultado.AnimesIgnorados++;
                    resultado.ErrosDetalhados.Add(
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Falha ao associar MalId {malId} à coleção '{tituloMyAnime}': {exAssociacao.Message}");
                }
            }
            catch (Exception ex)
            {
                resultado.AnimesComFalha++;
                resultado.ErrosDetalhados.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Falha ao salvar MalId {malId} da coleção '{tituloMyAnime}' no DB local: {ex.Message}");
            }

            ReportarProgresso();

            void ReportarProgresso()
            {
                var percentual = (int)Math.Round(((indice + 1) / (double)ids.Count) * 100, MidpointRounding.AwayFromZero);
                progresso?.Report(new ProgressoImportacaoAnimes
                {
                    Percentual = Math.Clamp(percentual, 0, 100),
                    Mensagem = $"Salvando animes {indice + 1}/{ids.Count} da coleção '{tituloMyAnime}'"
                });
            }
        }

        return resultado;
    }

    private async Task<bool> TentarSalvarModoDegradacaoAsync(
        int myAnimeId,
        string tituloMyAnime,
        int malId,
        ResultadoImportacaoAnimes resultado)
    {
        try
        {
            var dtoFallback = new AdicionaAnimeDto
            {
                MalId = malId,
                Titulo = $"Anime_{malId}_Fallback",
                Episodios = 1,
                MyAnimeID = myAnimeId,
                Source = "Fallback",
                Synopsis = $"Registro em modo de degradação. Falha ao consultar detalhes na ApiMyAnimeList para a coleção '{tituloMyAnime}'."
            };

            await _apiMyAnimesService.AdicionarAnimeAsync(dtoFallback);
            resultado.AnimesSalvosModoDegradacao++;

            resultado.ErrosDetalhados.Add(
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Modo de degradação aplicado para MalId {malId} da coleção '{tituloMyAnime}'. Anime salvo com dados mínimos.");
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            resultado.AnimesIgnorados++;
            return true;
        }
        catch (Exception ex)
        {
            resultado.ErrosDetalhados.Add(
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Falha no modo de degradação para MalId {malId} da coleção '{tituloMyAnime}': {ex.Message}");
            return false;
        }
    }

    private async Task<AnimeDetails?> BuscarAnimeComRetryAsync(
        string tituloMyAnime,
        int malId,
        List<string> errosDetalhados,
        CancellationToken cancellationToken)
    {
        var errosTentativas = new List<string>();

        for (var tentativa = 1; tentativa <= MaxTentativasApiMyAnimeList; tentativa++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var anime = await _myAnimeListApiService.BuscarPorIdAsync(malId, cancellationToken);
                if (anime is not null)
                    return anime;

                errosTentativas.Add($"Tentativa {tentativa}: resposta vazia.");
            }
            catch (Exception ex)
            {
                errosTentativas.Add($"Tentativa {tentativa}: {ex.Message}");
            }

            if (tentativa < MaxTentativasApiMyAnimeList)
                await Task.Delay(DelayTentativaApiMyAnimeList, cancellationToken);
        }

        errosDetalhados.Add(
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Falha ao consultar ApiMyAnimeList para MalId {malId} (coleção '{tituloMyAnime}') após {MaxTentativasApiMyAnimeList} tentativas. Detalhes: {string.Join(" | ", errosTentativas)}");
        return null;
    }

    public static string SalvarLogErros(string prefixoArquivo, IEnumerable<string> errosDetalhados)
    {
        var erros = errosDetalhados.ToList();
        if (erros.Count == 0)
            return string.Empty;

        var diretorioLogs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogsImportacao");
        Directory.CreateDirectory(diretorioLogs);

        var nomeSeguro = string.Join("-", prefixoArquivo.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var nomeArquivo = $"{nomeSeguro}-{DateTime.Now:yyyyMMdd-HHmmss}.log";
        var caminho = Path.Combine(diretorioLogs, nomeArquivo);

        File.WriteAllLines(caminho,
        [
            "=== Log de Erros ===",
            $"Data/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            string.Empty,
            .. erros
        ]);

        return caminho;
    }
}

public class ResultadoImportacaoAnimes
{
    public int MyAnimeId { get; init; }
    public string TituloMyAnime { get; init; } = string.Empty;
    public int AnimesSalvos { get; set; }
    public int AnimesSalvosModoDegradacao { get; set; }
    public int AnimesIgnorados { get; set; }
    public int AnimesComFalha { get; set; }
    public List<string> ErrosDetalhados { get; } = [];
}

public class ProgressoImportacaoAnimes
{
    public int Percentual { get; init; }
    public string Mensagem { get; init; } = string.Empty;
}
