using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.FileMonitoring;

namespace WinAppDtudo.Services;

/// <summary>Dados de catalogo usados para enriquecer o relatorio (titulo e total de episodios do anime).</summary>
public sealed record AnimeCatalogEntry(int MalId, string Title, int? Episodes);

/// <summary>Consulta de titulos no DB_Local (via ApiMyAnimes). Falhas aqui nunca interrompem a verificacao das pastas.</summary>
public interface IAnimeFolderCatalog
{
    Task<string?> GetCollectionTitleAsync(int myAnimeId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<int, AnimeCatalogEntry>> GetAnimesAsync(
        IEnumerable<int> malIds, CancellationToken cancellationToken);
}

/// <summary>Adaptador de <see cref="IAnimeFolderCatalog"/> sobre o <see cref="ApiMyAnimesService"/>.</summary>
public sealed class ApiMyAnimesFolderCatalog(ApiMyAnimesService service) : IAnimeFolderCatalog
{
    private const int MaxIdsPerRequest = 200;
    private readonly ApiMyAnimesService _service = service ?? throw new ArgumentNullException(nameof(service));

    public async Task<string?> GetCollectionTitleAsync(int myAnimeId, CancellationToken cancellationToken)
    {
        var collection = await _service.ObterMyAnimePorIdAsync(myAnimeId).WaitAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(collection?.Titulo) ? null : collection.Titulo.Trim();
    }

    public async Task<IReadOnlyDictionary<int, AnimeCatalogEntry>> GetAnimesAsync(
        IEnumerable<int> malIds, CancellationToken cancellationToken)
    {
        var ids = malIds.Where(id => id > 0).Distinct().ToList();
        var result = new Dictionary<int, AnimeCatalogEntry>();
        for (var offset = 0; offset < ids.Count; offset += MaxIdsPerRequest)
        {
            var chunk = ids.Skip(offset).Take(MaxIdsPerRequest).ToList();
            var animes = await _service.ObterAnimesRelacionadosAsync(chunk).WaitAsync(cancellationToken);
            foreach (var anime in animes)
            {
                var title = !string.IsNullOrWhiteSpace(anime.Title) ? anime.Title : anime.Titulo;
                result[anime.MalId] = new AnimeCatalogEntry(
                    anime.MalId,
                    string.IsNullOrWhiteSpace(title) ? $"MAL {anime.MalId}" : title.Trim(),
                    anime.Episodes is > 0 ? anime.Episodes : anime.Episodios > 0 ? anime.Episodios : null);
            }
        }

        return result;
    }
}

public enum AnimeFolderRefreshPhase
{
    Starting,
    AttachedToExisting,
    Planning,
    Processing,
    Retrying,
    Cancelling,
    Finished
}

/// <summary>Atualizacao de progresso com texto detalhado da etapa e, quando houver, a nova linha do relatorio.</summary>
/// <param name="Processed">Animes ja verificados.</param>
/// <param name="Total">Total de animes a verificar (0 enquanto a ApiMyAnimes ainda planeja a execucao).</param>
public sealed record AnimeFolderRefreshProgress(
    AnimeFolderRefreshPhase Phase,
    string Message,
    int Processed,
    int Total,
    AnimeFolderStatusRow? Row = null);

public sealed record AnimeFolderRefreshResult(
    AnimeFolderRefreshRunDto Run,
    IReadOnlyList<AnimeFolderStatusRow> Rows,
    bool CancelledByUser,
    bool AttachedToExisting,
    string Summary,
    IReadOnlyList<string> Warnings);

/// <summary>Ajustes de tempo e tolerancia do acompanhamento (valores reduzidos nos testes).</summary>
public sealed record AnimeFolderRefreshOptions
{
    public static AnimeFolderRefreshOptions Default { get; } = new();

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(800);
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Falhas transitorias consecutivas (API fora do ar) toleradas antes de desistir do acompanhamento.</summary>
    public int MaxConsecutiveFailures { get; init; } = 5;

    public int PageSize { get; init; } = 200;

    /// <summary>Tempo maximo esperando a ApiMyAnimes encerrar a execucao depois do pedido de cancelamento.</summary>
    public TimeSpan CancelGracePeriod { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Dispara a verificacao das pastas na ApiMyAnimes e acompanha o progresso, gerando mensagens por etapa e o relatorio por anime.
/// Nao conhece UI: quem exibe recebe <see cref="IProgress{T}"/>. Cancelar interrompe a verificacao no servidor, e os animes
/// ja resolvidos permanecem gravados.
/// </summary>
public sealed class AnimeFolderRefreshCoordinator(
    IAnimeFolderStatusApi api,
    IAnimeFolderCatalog? catalog = null,
    AnimeFolderRefreshOptions? options = null)
{
    private readonly IAnimeFolderStatusApi _api = api ?? throw new ArgumentNullException(nameof(api));
    private readonly AnimeFolderRefreshOptions _options = options ?? AnimeFolderRefreshOptions.Default;

    /// <summary>
    /// Executa a verificacao de uma colecao (<paramref name="myAnimeId"/>) ou de todas (<c>null</c>) e aguarda o fim.
    /// Lanca <see cref="FolderRefreshException"/> com mensagem clara quando nao e possivel iniciar ou acompanhar.
    /// O cancelamento nao lanca: devolve o resultado parcial com <see cref="AnimeFolderRefreshResult.CancelledByUser"/>.
    /// </summary>
    public async Task<AnimeFolderRefreshResult> RunAsync(
        int? myAnimeId, IProgress<AnimeFolderRefreshProgress>? progress, CancellationToken cancellationToken)
    {
        var state = new RunState(progress);
        try
        {
            await StartAsync(myAnimeId, state, cancellationToken);
            await PollAsync(state, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (state.RunId is null) throw;
            await CancelAndDrainAsync(state);
            return BuildResult(state, cancelledByUser: true);
        }

        return BuildResult(state, cancelledByUser: false);
    }

    private async Task StartAsync(int? myAnimeId, RunState state, CancellationToken cancellationToken)
    {
        state.Report(AnimeFolderRefreshPhase.Starting,
            myAnimeId is null
                ? "Solicitando à ApiMyAnimes a verificação das pastas de todas as coleções…"
                : "Solicitando à ApiMyAnimes a verificação das pastas da coleção…");

        if (myAnimeId is { } id)
        {
            state.FixedCollectionTitle = await TryGetCollectionTitleAsync(id, state, cancellationToken);
            if (state.FixedCollectionTitle is not null)
                state.Report(AnimeFolderRefreshPhase.Starting, $"Coleção {state.FixedCollectionTitle}: solicitando a verificação das pastas…");
        }

        try
        {
            var run = myAnimeId is { } collectionId
                ? await _api.StartCollectionRefreshAsync(collectionId, cancellationToken)
                : await _api.StartAllRefreshAsync(cancellationToken);
            state.Attach(run);
        }
        catch (FolderRefreshException exception) when (exception.Kind == FolderRefreshFailureKind.Busy && exception.ActiveRunId is { } activeRunId)
        {
            var active = await _api.GetRunAsync(activeRunId, false, 0, 1, cancellationToken);
            state.Attach(active);
            state.AttachedToExisting = true;
            state.Report(AnimeFolderRefreshPhase.AttachedToExisting,
                "Já havia uma verificação de pastas em andamento na ApiMyAnimes; acompanhando essa execução…");
        }
    }

    private async Task PollAsync(RunState state, CancellationToken cancellationToken)
    {
        var failures = 0;
        while (true)
        {
            AnimeFolderRefreshRunDto run;
            try
            {
                run = await _api.GetRunAsync(state.RunId!.Value, true, state.LastItemId, _options.PageSize, cancellationToken);
                failures = 0;
            }
            catch (FolderRefreshException exception) when (exception.IsTransient)
            {
                failures++;
                if (failures > _options.MaxConsecutiveFailures)
                {
                    throw new FolderRefreshException(
                        FolderRefreshFailureKind.ApiUnavailable,
                        $"A ApiMyAnimes ficou indisponível durante a verificação ({exception.Message}). " +
                        "A verificação pode continuar no servidor; abra a tela novamente para acompanhar.",
                        exception.StatusCode, null, exception);
                }

                state.Report(AnimeFolderRefreshPhase.Retrying,
                    $"ApiMyAnimes não respondeu; tentando novamente ({failures}/{_options.MaxConsecutiveFailures})…");
                await Task.Delay(_options.RetryDelay, cancellationToken);
                continue;
            }
            catch (FolderRefreshException exception) when (exception.Kind == FolderRefreshFailureKind.NotFound)
            {
                throw new FolderRefreshException(
                    FolderRefreshFailureKind.NotFound,
                    "A execução deixou de existir na ApiMyAnimes (a API pode ter sido reiniciada). Inicie a verificação novamente.",
                    exception.StatusCode, null, exception);
            }

            state.LastRun = run;
            var items = run.Items ?? [];
            if (items.Count > 0)
            {
                state.LastItemId = Math.Max(state.LastItemId, items.Max(item => item.Id));
                await ProcessItemsAsync(state, run, items, cancellationToken);
            }
            else if (!AnimeFolderRefreshStatuses.IsFinal(run.Status))
            {
                state.ReportWaiting(run);
            }

            var pageFull = items.Count >= _options.PageSize;
            if (AnimeFolderRefreshStatuses.IsFinal(run.Status) && !pageFull)
            {
                state.Report(AnimeFolderRefreshPhase.Finished,
                    AnimeFolderStatusPresenter.BuildSummary(run, state.Rows, cancelledByUser: false));
                return;
            }

            if (!pageFull) await Task.Delay(_options.PollInterval, cancellationToken);
        }
    }

    private async Task ProcessItemsAsync(
        RunState state, AnimeFolderRefreshRunDto run, IReadOnlyList<AnimeFolderRefreshItemDto> items, CancellationToken cancellationToken)
    {
        await EnrichAsync(state, items, cancellationToken);

        foreach (var item in items.OrderBy(item => item.Id))
        {
            var row = BuildRow(state, item);
            state.Rows.Add(row);
            var total = Math.Max(run.Total, state.Rows.Count);
            state.Report(AnimeFolderRefreshPhase.Processing,
                $"Coleção {row.CollectionTitle}: '{row.Title}' verificado ({state.Rows.Count}/{total}) — {AnimeFolderStatusPresenter.OutcomeText(row)}",
                row, total);
        }
    }

    private async Task EnrichAsync(RunState state, IReadOnlyList<AnimeFolderRefreshItemDto> items, CancellationToken cancellationToken)
    {
        var newMalIds = items.Select(item => item.MalId).Where(id => !state.Animes.ContainsKey(id) || !state.FolderInfo.ContainsKey(id)).Distinct().ToList();
        if (newMalIds.Count == 0 && items.All(item => state.CollectionTitles.ContainsKey(item.MyAnimeId))) return;

        if (catalog is not null)
        {
            var missingAnimes = newMalIds.Where(id => !state.Animes.ContainsKey(id)).ToList();
            if (missingAnimes.Count > 0)
            {
                try
                {
                    foreach (var (malId, entry) in await catalog.GetAnimesAsync(missingAnimes, cancellationToken))
                        state.Animes[malId] = entry;
                }
                catch (Exception exception) when (IsRecoverable(exception, cancellationToken))
                {
                    state.Warn("Não foi possível obter os nomes dos animes; o relatório usa os códigos MAL.");
                }
            }

            foreach (var collectionId in items.Select(item => item.MyAnimeId).Distinct().Where(id => !state.CollectionTitles.ContainsKey(id)))
                await TryGetCollectionTitleAsync(collectionId, state, cancellationToken);
        }

        if (newMalIds.Count > 0)
        {
            try
            {
                foreach (var (malId, info) in await _api.GetFolderStatusAsync(newMalIds, cancellationToken))
                    state.FolderInfo[malId] = info;
            }
            catch (Exception exception) when (IsRecoverable(exception, cancellationToken))
            {
                state.Warn("Não foi possível obter a última verificação e o total de episódios; o relatório usa apenas os dados da execução.");
            }
        }
    }

    private async Task<string?> TryGetCollectionTitleAsync(int myAnimeId, RunState state, CancellationToken cancellationToken)
    {
        if (state.CollectionTitles.TryGetValue(myAnimeId, out var cached)) return cached;
        string? title = null;
        if (catalog is not null)
        {
            try
            {
                title = await catalog.GetCollectionTitleAsync(myAnimeId, cancellationToken);
            }
            catch (Exception exception) when (IsRecoverable(exception, cancellationToken))
            {
                state.Warn("Não foi possível obter o nome de algumas coleções; o relatório usa o código MyAnime.");
            }
        }

        state.CollectionTitles[myAnimeId] = title;
        return title;
    }

    private static AnimeFolderStatusRow BuildRow(RunState state, AnimeFolderRefreshItemDto item)
    {
        state.Animes.TryGetValue(item.MalId, out var anime);
        state.FolderInfo.TryGetValue(item.MalId, out var info);
        state.CollectionTitles.TryGetValue(item.MyAnimeId, out var collectionTitle);

        var preserved = AnimeFolderRefreshOutcomes.PreservesPreviousStatus(item.Outcome);
        var itemState = string.IsNullOrWhiteSpace(item.State) ? null : AnimeFolderState.Normalize(item.State);
        var rowState = preserved ? info?.State : itemState ?? info?.State;
        var videos = preserved ? info?.Videos : item.Videos ?? info?.Videos;
        var expected = info?.Expected is > 0 ? info.Expected : anime?.Episodes;
        var checkedAt = preserved ? info?.CheckedAtUtc : item.OccurredAtUtc.UtcDateTime;

        return new AnimeFolderStatusRow(
            item.MalId,
            item.MyAnimeId,
            collectionTitle ?? $"MyAnime {item.MyAnimeId}",
            anime?.Title ?? $"MAL {item.MalId}",
            rowState,
            videos,
            expected,
            checkedAt,
            item.Outcome,
            item.Detail,
            preserved);
    }

    private async Task CancelAndDrainAsync(RunState state)
    {
        using var grace = new CancellationTokenSource(_options.CancelGracePeriod);
        state.Report(AnimeFolderRefreshPhase.Cancelling,
            "Cancelando a verificação… os animes já verificados permanecem gravados.");
        try
        {
            await _api.CancelRunAsync(state.RunId!.Value, grace.Token);
        }
        catch (Exception exception) when (exception is FolderRefreshException or OperationCanceledException)
        {
            // Melhor esforco: se o pedido falhar, ainda assim paramos de acompanhar e devolvemos o que foi verificado.
            state.Warn("Não foi possível confirmar o cancelamento na ApiMyAnimes; a verificação pode terminar sozinha no servidor.");
        }

        try
        {
            await PollAsync(state, grace.Token);
        }
        catch (Exception exception) when (exception is FolderRefreshException or OperationCanceledException)
        {
            if (state.LastRun is { } last && !AnimeFolderRefreshStatuses.IsFinal(last.Status))
                state.Warn("A ApiMyAnimes ainda estava encerrando a verificação quando o acompanhamento parou.");
        }
    }

    private static AnimeFolderRefreshResult BuildResult(RunState state, bool cancelledByUser)
    {
        var run = state.LastRun ?? throw new InvalidOperationException("Execucao sem estado conhecido.");
        var summary = AnimeFolderStatusPresenter.BuildSummary(run, state.Rows, cancelledByUser);
        return new AnimeFolderRefreshResult(run, state.Rows, cancelledByUser, state.AttachedToExisting, summary, state.Warnings);
    }

    private static bool IsRecoverable(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested
        && (exception is FolderRefreshException or HttpRequestException or TimeoutException or OperationCanceledException
            or System.Text.Json.JsonException or WinAppAuthenticationException);

    private sealed class RunState(IProgress<AnimeFolderRefreshProgress>? progress)
    {
        public Guid? RunId { get; private set; }
        public AnimeFolderRefreshRunDto? LastRun { get; set; }
        public long LastItemId { get; set; }
        public bool AttachedToExisting { get; set; }
        public string? FixedCollectionTitle { get; set; }
        public List<AnimeFolderStatusRow> Rows { get; } = [];
        public Dictionary<int, AnimeCatalogEntry> Animes { get; } = [];
        public Dictionary<int, AnimeFolderStatusInfo> FolderInfo { get; } = [];
        public Dictionary<int, string?> CollectionTitles { get; } = [];
        public List<string> Warnings { get; } = [];

        public void Attach(AnimeFolderRefreshRunDto run)
        {
            RunId = run.Id;
            LastRun = run;
        }

        public void Warn(string message)
        {
            if (!Warnings.Contains(message)) Warnings.Add(message);
        }

        public void Report(AnimeFolderRefreshPhase phase, string message, AnimeFolderStatusRow? row = null, int? total = null) =>
            progress?.Report(new AnimeFolderRefreshProgress(phase, message, Rows.Count, total ?? LastRun?.Total ?? 0, row));

        public void ReportWaiting(AnimeFolderRefreshRunDto run)
        {
            if (run.Total <= 0)
            {
                Report(AnimeFolderRefreshPhase.Planning,
                    "Planejando: localizando as pastas das coleções na ApiFileStorage…");
                return;
            }

            Report(AnimeFolderRefreshPhase.Processing,
                $"Contando os vídeos de {run.Total} animes; aguardando o primeiro resultado…");
        }
    }
}
