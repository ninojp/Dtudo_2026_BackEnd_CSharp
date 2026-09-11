using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.MyAnimeList;
using System.Net;

namespace WinAppDtudo.Services;

public class ImportadorAnimesMyAnimeService
{
    private const int MaxTentativasApiMyAnimeList = 3;
    private static readonly TimeSpan DelayTentativaApiMyAnimeList = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Profundidade máxima de expansão do grafo de relações a partir do anime atual:
    /// nível 1 = relações diretas, nível 2 = relações dos itens do nível 1 ainda não conhecidos,
    /// nível 3 = relações dos itens do nível 2 ainda não conhecidos. Itens do nível 3 têm seu
    /// próprio campo de relações preenchido, mas novos ids descobertos a partir deles não são importados.
    /// </summary>
    private const int ProfundidadeMaximaRelacionados = 3;

    private readonly ApiMyAnimesService _apiMyAnimesService;
    private readonly MyAnimeListApiService _myAnimeListApiService;
    private readonly Dictionary<int, IReadOnlyCollection<int>?> _relacoesCache = [];

    public ImportadorAnimesMyAnimeService(
        ApiMyAnimesService? apiMyAnimesService = null,
        WinAppAuthenticationService? authenticationService = null,
        MyAnimeListApiService? myAnimeListApiService = null)
    {
        var resolvedAuthenticationService = authenticationService ?? new WinAppAuthenticationService();
        _apiMyAnimesService = apiMyAnimesService ?? new ApiMyAnimesService(resolvedAuthenticationService);
        _myAnimeListApiService = myAnimeListApiService
            ?? new MyAnimeListApiService(resolvedAuthenticationService);
    }

    public async Task<ResultadoImportacaoAnimes> ImportarAsync(
        int myAnimeId,
        string tituloMyAnime,
        IReadOnlyCollection<int> malIds,
        IProgress<ProgressoImportacaoAnimes>? progresso = null,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>>? animesRelacionadosPorMalId = null)
    {
        var resultado = new ResultadoImportacaoAnimes
        {
            MyAnimeId = myAnimeId,
            TituloMyAnime = tituloMyAnime
        };

        var idsIniciais = malIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (idsIniciais.Count == 0)
        {
            progresso?.Report(new ProgressoImportacaoAnimes
            {
                Percentual = 100,
                Mensagem = "Nenhum anime para importar."
            });
            return resultado;
        }

        // Fila de expansão em largura (BFS): os ids iniciais (anime atual + relações diretas)
        // começam no nível 1. Ao processar cada anime, suas relações reais na ApiMyAnimeList são
        // consultadas; qualquer id ainda desconhecido é enfileirado para o próximo nível, até o
        // limite de ProfundidadeMaximaRelacionados.
        var conhecidos = new HashSet<int>(idsIniciais);
        var fila = new Queue<(int MalId, int Profundidade)>(idsIniciais.Select(id => (id, 1)));
        var processados = 0;

        while (fila.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (malId, profundidade) = fila.Dequeue();
            processados++;

            // Busca os detalhes primeiro: o cache da ApiMyAnimeList (chave mal-anime-{id}) fica
            // aquecido para a consulta de relações logo em seguida, reduzindo o risco de falha transitória.
            var detalhes = await BuscarAnimeComRetryAsync(tituloMyAnime, malId, resultado.ErrosDetalhados, cancellationToken);
            var animesRelacionadosIds = await ObterAnimesRelacionadosAsync(
                malId,
                animesRelacionadosPorMalId,
                resultado.ErrosDetalhados,
                cancellationToken);

            if (detalhes is null)
            {
                var salvouFallback = await TentarSalvarModoDegradacaoAsync(
                    myAnimeId,
                    tituloMyAnime,
                    malId,
                    resultado,
                    animesRelacionadosIds,
                    cancellationToken);
                if (!salvouFallback)
                    resultado.AnimesComFalha++;
            }
            else
            {
                var dtoAnime = ConversorAnimeDtoService.CriarAdicionaAnimeDto(
                    detalhes,
                    myAnimeId,
                    animesRelacionadosIds);

                try
                {
                    var animeFoiCriado = await GarantirAnimeNaColecaoAsync(
                        dtoAnime,
                        myAnimeId,
                        animesRelacionadosIds,
                        cancellationToken);
                    if (animeFoiCriado)
                        resultado.AnimesSalvos++;
                    else
                        resultado.AnimesIgnorados++;
                }
                catch (Exception ex)
                {
                    resultado.AnimesComFalha++;
                    resultado.ErrosDetalhados.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Falha ao salvar MalId {malId} (nível {profundidade}) da coleção '{tituloMyAnime}' no DB local: {ex.Message}");
                }
            }

            if (profundidade < ProfundidadeMaximaRelacionados && animesRelacionadosIds is not null)
            {
                foreach (var novoId in animesRelacionadosIds.OrderBy(id => id))
                {
                    if (novoId > 0 && conhecidos.Add(novoId))
                        fila.Enqueue((novoId, profundidade + 1));
                }
            }

            var totalConhecido = processados + fila.Count;
            var percentual = totalConhecido == 0
                ? 100
                : (int)Math.Round((processados / (double)totalConhecido) * 100, MidpointRounding.AwayFromZero);
            progresso?.Report(new ProgressoImportacaoAnimes
            {
                Percentual = Math.Clamp(percentual, 0, 99),
                Mensagem = $"Salvando animes da coleção '{tituloMyAnime}': {processados} processados, {fila.Count} na fila " +
                    $"(MalId {malId}, nível {profundidade}/{ProfundidadeMaximaRelacionados})"
            });
        }

        progresso?.Report(new ProgressoImportacaoAnimes
        {
            Percentual = 100,
            Mensagem = $"Importação concluída: {processados} animes processados para a coleção '{tituloMyAnime}'."
        });

        return resultado;
    }

    private async Task<bool> TentarSalvarModoDegradacaoAsync(
        int myAnimeId,
        string tituloMyAnime,
        int malId,
        ResultadoImportacaoAnimes resultado,
        IReadOnlyCollection<int>? animesRelacionadosIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var dtoFallback = new AdicionaAnimeDto
            {
                MalId = malId,
                Titulo = $"Anime_{malId}_Fallback",
                Episodios = 1,
                MyAnimeID = myAnimeId,
                AnimesRelacionadosIds = [.. animesRelacionadosIds ?? []],
                Source = "Fallback",
                Synopsis = $"Registro em modo de degradação. Falha ao consultar detalhes na ApiMyAnimeList para a coleção '{tituloMyAnime}'."
            };

            var animeFoiCriado = await GarantirAnimeNaColecaoAsync(
                dtoFallback,
                myAnimeId,
                animesRelacionadosIds,
                cancellationToken);
            if (animeFoiCriado)
            {
                resultado.AnimesSalvosModoDegradacao++;
            }
            else
            {
                resultado.AnimesIgnorados++;
                return true;
            }

            resultado.ErrosDetalhados.Add(
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Modo de degradação aplicado para MalId {malId} da coleção '{tituloMyAnime}'. Anime salvo com dados mínimos.");
            return true;
        }
        catch (Exception ex)
        {
            resultado.ErrosDetalhados.Add(
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Falha no modo de degradação para MalId {malId} da coleção '{tituloMyAnime}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Resolve os MalIds relacionados de <paramref name="malId"/>. Usa o mapa conhecido (já carregado
    /// pela UI para o anime atualmente visualizado) quando disponível; caso contrário, consulta a
    /// ApiMyAnimeList diretamente, garantindo que TODO anime importado (incluindo os relacionados de
    /// segundo grau) tenha seu próprio grafo de relações persistido corretamente.
    /// </summary>
    private async Task<IReadOnlyCollection<int>?> ObterAnimesRelacionadosAsync(
        int malId,
        IReadOnlyDictionary<int, IReadOnlyCollection<int>>? animesRelacionadosConhecidos,
        List<string> errosDetalhados,
        CancellationToken cancellationToken)
    {
        if (animesRelacionadosConhecidos is not null &&
            animesRelacionadosConhecidos.TryGetValue(malId, out var conhecidos))
            return conhecidos.Where(id => id > 0 && id != malId).Distinct().ToList();

        if (_relacoesCache.TryGetValue(malId, out var emCache))
            return emCache;

        var errosTentativas = new List<string>();
        for (var tentativa = 1; tentativa <= MaxTentativasApiMyAnimeList; tentativa++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var grupos = await _myAnimeListApiService.BuscarRelacoesAsync(malId, cancellationToken);
                var ids = grupos
                    .SelectMany(grupo => grupo.Entry ?? [])
                    .Select(entrada => entrada.MalId)
                    .Where(id => id > 0 && id != malId)
                    .Distinct()
                    .ToList();
                _relacoesCache[malId] = ids;
                return ids;
            }
            catch (Exception ex)
            {
                errosTentativas.Add($"Tentativa {tentativa}: {ex.Message}");
            }

            if (tentativa < MaxTentativasApiMyAnimeList)
                await Task.Delay(DelayTentativaApiMyAnimeList, cancellationToken);
        }

        errosDetalhados.Add(
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Falha ao consultar relações da ApiMyAnimeList para MalId {malId} após {MaxTentativasApiMyAnimeList} tentativas. Detalhes: {string.Join(" | ", errosTentativas)}");
        _relacoesCache[malId] = null;
        return null;
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

    private async Task<bool> GarantirAnimeNaColecaoAsync(
        AdicionaAnimeDto dto,
        int myAnimeId,
        IReadOnlyCollection<int>? animesRelacionadosIds,
        CancellationToken cancellationToken)
    {
        var animeFoiCriado = true;
        try
        {
            await _apiMyAnimesService.AdicionarAnimeAsync(dto, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            animeFoiCriado = false;
            if (animesRelacionadosIds is not null)
            {
                await _apiMyAnimesService.AtualizarAnimesRelacionadosIdsAsync(
                    dto.MalId,
                    animesRelacionadosIds,
                    cancellationToken);
            }
        }

        await _apiMyAnimesService.AssociarAnimeAoMyAnimeAsync(
            dto.MalId,
            myAnimeId,
            cancellationToken);
        return animeFoiCriado;
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
