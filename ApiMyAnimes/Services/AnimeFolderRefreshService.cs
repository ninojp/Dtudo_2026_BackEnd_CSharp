using System.Collections.Concurrent;
using ApiMyAnimes.Configuration;
using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos.FileMonitoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiMyAnimes.Services;

/// <summary>
/// Calcula e persiste o status da pasta de cada anime (<see cref="AnimeFolderStatus"/>) chamando a ApiFileStorage:
/// descoberta das subpastas de cada colecao (uma chamada por colecao), mapeamento anime-pasta
/// (<see cref="AnimeFolderMapper"/>) e contagem de videos por anime. Concorrencia limitada, cancelavel, falha por anime
/// isolada, progresso persistido por anime. Disco offline ou falha de rede preservam o status anterior.
/// </summary>
public sealed class AnimeFolderRefreshService(
    MyAnimesContext context,
    IAnimeFolderScanClient client,
    IOptions<AnimeFolderRefreshOptions> options,
    TimeProvider timeProvider,
    ILogger<AnimeFolderRefreshService> logger)
{
    private const int MaxItemsPerPage = 500;
    private const int MaxRunsListed = 50;
    private const string UnavailableCollectionDetail = "Pasta da colecao indisponivel (disco offline ou pasta removida); status anterior preservado.";
    private const string InterruptedError = "Execucao interrompida: o processo que a executava foi encerrado.";

    /// <summary>
    /// Registra uma nova execucao em andamento. O chamador garante que nao ha outra execucao ativa neste processo; execucoes
    /// "em andamento" remanescentes no banco (processo encerrado) sao marcadas como interrompidas.
    /// </summary>
    public async Task<AnimeFolderRefreshRun> CreateRunAsync(int? myAnimeId, string requestedBy, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await MarkStaleRunsInterruptedAsync(now, cancellationToken);
        var run = new AnimeFolderRefreshRun
        {
            Id = Guid.NewGuid(),
            StartedAtUtc = now,
            Status = AnimeFolderRefreshStatuses.Running,
            MyAnimeId = myAnimeId,
            RequestedBy = Truncate(requestedBy, AnimeFolderRefreshRun.RequestedByMaxLength)
        };
        context.AnimeFolderRefreshRuns.Add(run);
        await context.SaveChangesAsync(cancellationToken);
        return run;
    }

    /// <summary>Consulta uma execucao; <c>null</c> se nao existir. <paramref name="isActive"/> evita exibir execucao morta como ativa.</summary>
    public async Task<AnimeFolderRefreshRunDto?> GetRunAsync(
        Guid runId, bool isActive, bool includeItems, long afterId, int take, CancellationToken cancellationToken)
    {
        var run = await context.AnimeFolderRefreshRuns.FirstOrDefaultAsync(item => item.Id == runId, cancellationToken);
        if (run is null) return null;
        if (run.Status == AnimeFolderRefreshStatuses.Running && !isActive)
        {
            run.Status = AnimeFolderRefreshStatuses.Interrupted;
            run.FinishedAtUtc = timeProvider.GetUtcNow();
            run.Error = InterruptedError;
            await context.SaveChangesAsync(cancellationToken);
        }
        if (!includeItems) return AnimeFolderRefreshRunDto.From(run);

        var items = await context.AnimeFolderRefreshItems.AsNoTracking()
            .Where(item => item.RunId == runId && item.Id > afterId)
            .OrderBy(item => item.Id)
            .Take(Math.Clamp(take, 1, MaxItemsPerPage))
            .ToListAsync(cancellationToken);
        return AnimeFolderRefreshRunDto.From(run, items.Select(AnimeFolderRefreshItemDto.From).ToList());
    }

    /// <summary>Execucoes mais recentes (sem relatorio por anime).</summary>
    public async Task<IReadOnlyList<AnimeFolderRefreshRunDto>> ListRunsAsync(int take, CancellationToken cancellationToken)
    {
        var runs = await context.AnimeFolderRefreshRuns.AsNoTracking()
            .OrderByDescending(run => run.StartedAtUtc).ThenByDescending(run => run.Id)
            .Take(Math.Clamp(take, 1, MaxRunsListed))
            .ToListAsync(cancellationToken);
        return runs.Select(run => AnimeFolderRefreshRunDto.From(run)).ToList();
    }

    /// <summary>Executa a atualizacao da execucao ja criada. Nao lanca por falha de anime nem por cancelamento: registra o resultado na execucao.</summary>
    public async Task ExecuteAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await context.AnimeFolderRefreshRuns.FirstAsync(item => item.Id == runId, CancellationToken.None);
        var finalStatus = AnimeFolderRefreshStatuses.Completed;
        string? error = null;
        try
        {
            var plan = await BuildPlanAsync(run.MyAnimeId, cancellationToken);
            run.TotalAnimes = plan.Collections.Sum(collection => collection.Animes.Count);
            await context.SaveChangesAsync(CancellationToken.None);

            var writer = await ResultWriter.CreateAsync(context, run, plan.AllMalIds, timeProvider, cancellationToken);
            await RunAsync(plan, writer, cancellationToken);
            if (run.Failed > 0 || run.Unavailable > 0) finalStatus = AnimeFolderRefreshStatuses.CompletedWithErrors;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            finalStatus = AnimeFolderRefreshStatuses.Cancelled;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha inesperada na atualizacao do status de pasta {RunId}.", runId);
            finalStatus = AnimeFolderRefreshStatuses.Failed;
            error = "Falha inesperada na atualizacao; consulte os logs da ApiMyAnimes.";
        }

        await FinishAsync(runId, finalStatus, error);
    }

    private async Task FinishAsync(Guid runId, string status, string? error)
    {
        // Estado limpo: uma falha de gravacao anterior nao pode impedir o fechamento da execucao.
        context.ChangeTracker.Clear();
        var run = await context.AnimeFolderRefreshRuns.FirstAsync(item => item.Id == runId, CancellationToken.None);
        run.Status = status;
        run.FinishedAtUtc = timeProvider.GetUtcNow();
        run.Error = error is null ? null : Truncate(error, AnimeFolderRefreshRun.ErrorMaxLength);
        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task MarkStaleRunsInterruptedAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var stale = await context.AnimeFolderRefreshRuns
            .Where(run => run.Status == AnimeFolderRefreshStatuses.Running)
            .ToListAsync(cancellationToken);
        foreach (var run in stale)
        {
            run.Status = AnimeFolderRefreshStatuses.Interrupted;
            run.FinishedAtUtc = now;
            run.Error = InterruptedError;
        }
    }

    private async Task RunAsync(Plan plan, ResultWriter writer, CancellationToken cancellationToken)
    {
        var parallel = new ParallelOptions
        {
            MaxDegreeOfParallelism = options.Value.MaxConcurrency,
            CancellationToken = cancellationToken
        };
        var scanJobs = new ConcurrentBag<ScanJob>();

        // Fase 1: uma descoberta por colecao; anime sem pasta/falha ja tem seu resultado definitivo.
        await Parallel.ForEachAsync(plan.Collections, parallel, async (collection, token) =>
            await DiscoverCollectionAsync(collection, writer, scanJobs, token));

        // Fase 2: contagem de videos por anime encontrado.
        await Parallel.ForEachAsync(scanJobs.OrderBy(job => job.Anime.MalId), parallel, async (job, token) =>
            await ScanAnimeAsync(job, writer, token));
    }

    private async Task DiscoverCollectionAsync(
        CollectionPlan collection, ResultWriter writer, ConcurrentBag<ScanJob> scanJobs, CancellationToken cancellationToken)
    {
        if (collection.Location is null)
        {
            foreach (var anime in collection.Animes)
                await writer.RecordNoFolderAsync(collection, anime, AnimeFolderRefreshOutcomes.NoLocation,
                    "A colecao nao tem pasta vinculada.");
            return;
        }

        AnimeFolderDiscoveryResponse discovery;
        try
        {
            discovery = await client.DiscoverAsync(
                new AnimeFolderDiscoveryRequest(collection.Location.RootKey, collection.Location.RelativePath), cancellationToken);
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning("Falha na descoberta da colecao {MyAnimeId}: {Reason}", collection.MyAnimeId, exception.Message);
            foreach (var anime in collection.Animes)
                await writer.RecordPreservedAsync(collection, anime, AnimeFolderRefreshOutcomes.Failed, FailureDetail(exception));
            return;
        }

        if (!discovery.Exists)
        {
            foreach (var anime in collection.Animes)
                await writer.RecordPreservedAsync(collection, anime, AnimeFolderRefreshOutcomes.Unavailable, UnavailableCollectionDetail);
            return;
        }

        var matches = AnimeFolderMapper.Map(
            collection.Location.RelativePath,
            collection.Animes.Select(anime => new AnimeFolderMappingAnime(anime.MalId, anime.Year, anime.Type, anime.Titles)).ToList(),
            discovery.Folders);
        foreach (var anime in collection.Animes)
        {
            if (matches.TryGetValue(anime.MalId, out var match))
                scanJobs.Add(new ScanJob(collection, anime, match));
            else if (discovery.Truncated)
                await writer.RecordPreservedAsync(collection, anime, AnimeFolderRefreshOutcomes.Unavailable,
                    "Listagem de pastas truncada; nao foi possivel concluir que o anime nao tem pasta.");
            else
                await writer.RecordNoFolderAsync(collection, anime, AnimeFolderRefreshOutcomes.NotFound,
                    "Nenhuma subpasta da colecao corresponde ao anime (capa ou nome).");
        }
    }

    private async Task ScanAnimeAsync(ScanJob job, ResultWriter writer, CancellationToken cancellationToken)
    {
        AnimeFolderScanResponse scan;
        try
        {
            scan = await client.ScanAsync(
                new AnimeFolderScanRequest(job.Collection.Location!.RootKey, job.Match.RelativePath, job.Anime.Expected), cancellationToken);
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning("Falha na contagem do anime {MalId}: {Reason}", job.Anime.MalId, exception.Message);
            await writer.RecordPreservedAsync(job.Collection, job.Anime, AnimeFolderRefreshOutcomes.Failed, FailureDetail(exception));
            return;
        }

        if (!scan.Exists)
        {
            await writer.RecordPreservedAsync(job.Collection, job.Anime, AnimeFolderRefreshOutcomes.Unavailable,
                "Pasta do anime indisponivel na contagem; status anterior preservado.");
            return;
        }
        await writer.RecordUpdatedAsync(job.Collection, job.Anime, job.Match, scan);
    }

    private static string FailureDetail(Exception exception) =>
        exception is AnimeFolderClientException ? exception.Message : "Falha inesperada ao consultar a ApiFileStorage.";

    private async Task<Plan> BuildPlanAsync(int? myAnimeId, CancellationToken cancellationToken)
    {
        var collections = await context.MyAnimes.AsNoTracking()
            .Where(item => myAnimeId == null || item.Id == myAnimeId)
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.AnimesMalId })
            .ToListAsync(cancellationToken);
        var locations = await context.MonitoringLocations.AsNoTracking()
            .Where(item => myAnimeId == null || item.MyAnimeId == myAnimeId)
            .ToDictionaryAsync(item => item.MyAnimeId, cancellationToken);

        var memberIds = collections.SelectMany(item => item.AnimesMalId).Where(id => id > 0).ToHashSet();
        var animes = await context.Animes.AsNoTracking()
            .Where(anime => myAnimeId == null || anime.MyAnimeID == myAnimeId || memberIds.Contains(anime.MalId))
            .Select(anime => new
            {
                anime.MalId, anime.MyAnimeID, anime.Titulo, anime.Title, anime.TitleEnglish,
                anime.Year, anime.Type, anime.Episodes, anime.Episodios
            })
            .ToListAsync(cancellationToken);
        var byMalId = animes.ToDictionary(anime => anime.MalId);

        var assigned = new HashSet<int>();
        var plans = new List<CollectionPlan>();
        foreach (var collection in collections)
        {
            var ids = animes.Where(anime => anime.MyAnimeID == collection.Id).Select(anime => anime.MalId)
                .Concat(collection.AnimesMalId.Where(byMalId.ContainsKey))
                .Distinct()
                .Where(assigned.Add)
                .Order()
                .ToList();
            if (ids.Count == 0) continue;
            var items = ids.Select(id =>
            {
                var anime = byMalId[id];
                var titles = new[] { anime.Titulo, anime.Title, anime.TitleEnglish }
                    .Where(title => !string.IsNullOrWhiteSpace(title)).Select(title => title!).Distinct().ToList();
                return new PlanAnime(id, anime.Year, anime.Type, titles,
                    AnimeStatusRules.ResolveTotalEpisodes(anime.Episodes, anime.Episodios));
            }).ToList();
            locations.TryGetValue(collection.Id, out var location);
            plans.Add(new CollectionPlan(collection.Id, location is null ? null : new CollectionLocation(
                MonitoringPathPolicy.NormalizeRootKey(location.RootKey), location.RelativePath), items));
        }
        return new Plan(plans, plans.SelectMany(item => item.Animes).Select(item => item.MalId).ToHashSet());
    }

    private static string Truncate(string value, int maxLength) => value.Length > maxLength ? value[..maxLength] : value;

    private sealed record Plan(IReadOnlyList<CollectionPlan> Collections, HashSet<int> AllMalIds);
    private sealed record CollectionLocation(string RootKey, string RelativePath);
    private sealed record CollectionPlan(int MyAnimeId, CollectionLocation? Location, IReadOnlyList<PlanAnime> Animes);
    private sealed record PlanAnime(int MalId, int? Year, string? Type, IReadOnlyList<string> Titles, int? Expected);
    private sealed record ScanJob(CollectionPlan Collection, PlanAnime Anime, AnimeFolderMatch Match);

    /// <summary>Serializa as gravacoes (o <see cref="DbContext"/> nao e thread-safe): status, item do relatorio e contadores.</summary>
    private sealed class ResultWriter
    {
        private readonly MyAnimesContext context;
        private readonly AnimeFolderRefreshRun run;
        private readonly Dictionary<int, AnimeFolderStatus> existing;
        private readonly TimeProvider timeProvider;
        private readonly SemaphoreSlim gate = new(1, 1);

        private ResultWriter(MyAnimesContext context, AnimeFolderRefreshRun run, Dictionary<int, AnimeFolderStatus> existing, TimeProvider timeProvider)
        {
            this.context = context;
            this.run = run;
            this.existing = existing;
            this.timeProvider = timeProvider;
        }

        public static async Task<ResultWriter> CreateAsync(
            MyAnimesContext context, AnimeFolderRefreshRun run, HashSet<int> malIds, TimeProvider timeProvider, CancellationToken cancellationToken)
        {
            var query = run.MyAnimeId is null
                ? context.AnimeFolderStatuses
                : context.AnimeFolderStatuses.Where(status => malIds.Contains(status.MalId));
            return new ResultWriter(context, run, await query.ToDictionaryAsync(status => status.MalId, cancellationToken), timeProvider);
        }

        public Task RecordUpdatedAsync(CollectionPlan collection, PlanAnime anime, AnimeFolderMatch match, AnimeFolderScanResponse scan)
        {
            // Defesa: uma resposta fora do contrato nao pode violar as restricoes da tabela.
            var videos = Math.Max(0, scan.VideoCount);
            var state = scan.State is AnimeStatusStates.None or AnimeStatusStates.Partial or AnimeStatusStates.Complete
                ? scan.State
                : AnimeFolderState.Evaluate(true, videos, anime.Expected, scan.Truncated);
            return WriteAsync(collection, anime, AnimeFolderRefreshOutcomes.Updated, state, videos,
                $"Pasta encontrada ({match.Method}); {videos} video(s)" + (scan.Truncated ? ", leitura truncada." : "."),
                status =>
                {
                    status.RootKey = collection.Location!.RootKey;
                    status.RelativePath = match.RelativePath;
                    status.VideoCount = videos;
                    status.State = state;
                    status.Truncated = scan.Truncated;
                });
        }

        public Task RecordNoFolderAsync(CollectionPlan collection, PlanAnime anime, string outcome, string detail) =>
            WriteAsync(collection, anime, outcome, AnimeStatusStates.None, 0, detail, status =>
            {
                status.RootKey = string.Empty;
                status.RelativePath = string.Empty;
                status.VideoCount = 0;
                status.State = AnimeStatusStates.None;
                status.Truncated = false;
            });

        public Task RecordPreservedAsync(CollectionPlan collection, PlanAnime anime, string outcome, string detail) =>
            WriteAsync(collection, anime, outcome, null, null, detail, applyStatus: null);

        private async Task WriteAsync(
            CollectionPlan collection, PlanAnime anime, string outcome, string? state, int? videos, string detail, Action<AnimeFolderStatus>? applyStatus)
        {
            await gate.WaitAsync(CancellationToken.None);
            try
            {
                var now = timeProvider.GetUtcNow();
                if (applyStatus is not null)
                {
                    if (!existing.TryGetValue(anime.MalId, out var status))
                    {
                        status = new AnimeFolderStatus { MalId = anime.MalId };
                        existing[anime.MalId] = status;
                        context.AnimeFolderStatuses.Add(status);
                    }
                    status.MyAnimeId = collection.MyAnimeId;
                    status.ExpectedEpisodes = anime.Expected;
                    status.CheckedAtUtc = now.UtcDateTime;
                    applyStatus(status);
                }

                context.AnimeFolderRefreshItems.Add(new AnimeFolderRefreshItem
                {
                    RunId = run.Id,
                    MalId = anime.MalId,
                    MyAnimeId = collection.MyAnimeId,
                    Outcome = outcome,
                    State = state,
                    VideoCount = videos,
                    Detail = Truncate(detail, AnimeFolderRefreshItem.DetailMaxLength),
                    OccurredAtUtc = now
                });
                run.ProcessedAnimes++;
                switch (outcome)
                {
                    case AnimeFolderRefreshOutcomes.Updated: run.Updated++; break;
                    case AnimeFolderRefreshOutcomes.NotFound or AnimeFolderRefreshOutcomes.NoLocation: run.NotFound++; break;
                    case AnimeFolderRefreshOutcomes.Unavailable: run.Unavailable++; break;
                    default: run.Failed++; break;
                }
                await context.SaveChangesAsync(CancellationToken.None);
            }
            finally
            {
                gate.Release();
            }
        }
    }
}
