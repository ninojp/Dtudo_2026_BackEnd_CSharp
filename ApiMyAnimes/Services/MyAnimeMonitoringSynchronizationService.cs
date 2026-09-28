using System.Data;
using System.Globalization;
using System.Text;
using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos.FileMonitoring;
using LibDtudo.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ApiMyAnimes.Services;

public sealed class MyAnimeMonitoringSynchronizationService(
    MyAnimesContext context,
    ILogger<MyAnimeMonitoringSynchronizationService> logger)
{
    private static readonly SemaphoreSlim SynchronizationGate = new(1, 1);

    public async Task<CollectionSyncResultDto> SynchronizeAsync(
        CollectionSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Discovery);
        if (!await SynchronizationGate.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("Ja existe uma sincronizacao de colecoes em execucao.");

        var run = new MyAnimeMonitoringSyncRun
        {
            Id = Guid.NewGuid(),
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = "Running"
        };
        context.MonitoringSyncRuns.Add(run);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var roots = MonitoringPathPolicy.CollectionRoots();
            var catalog = await context.MyAnimes.AsNoTracking()
                .Select(item => new CatalogItem(item.Id, item.Titulo, item.AnimesMalId))
                .ToArrayAsync(cancellationToken);
            var storedMappings = await context.MonitoringLocations.AsNoTracking()
                .Select(item => new StoredMapping(item.MyAnimeId, item.RootKey, item.RelativePath))
                .ToArrayAsync(cancellationToken);
            var issues = ValidateDiscovery(request.Discovery, roots, storedMappings);
            var plans = BuildPlan(request.Discovery.Folders, catalog, storedMappings);
            var events = BuildEvents(request.Discovery, issues, plans, storedMappings);
            var unavailableRootKeys = request.Discovery.Roots
                .Where(root => !root.Available)
                .Select(root => root.Key)
                .ToHashSet(StringComparer.Ordinal);
            var applicablePlans = plans
                .Where(plan => !unavailableRootKeys.Contains(plan.Folder.RootKey))
                .ToArray();
            var blockingIssues = issues.Where(item => item.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase)
                && item.Code != "ROOT_UNAVAILABLE").ToArray();

            FillRunSummary(run, request.Discovery, applicablePlans, events);
            if (blockingIssues.Length > 0)
            {
                run.AddedMappings = 0;
                run.UpdatedMappings = 0;
                run.UnchangedMappings = 0;
                run.Status = "Blocked";
                run.FinishedAtUtc = DateTimeOffset.UtcNow;
                context.MonitoringSyncEvents.AddRange(events.Select(item => ToEntity(run.Id, item)));
                await context.SaveChangesAsync(cancellationToken);
                return await GetAsync(run.Id, cancellationToken);
            }

            IDbContextTransaction? transaction = null;
            try
            {
                if (context.Database.IsRelational())
                    transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

                await ApplyPlanAsync(applicablePlans, cancellationToken);
                context.MonitoringSyncEvents.AddRange(events.Select(item => ToEntity(run.Id, item)));
                run.Status = unavailableRootKeys.Count == 0 ? "Completed" : "Partial";
                run.FinishedAtUtc = DateTimeOffset.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                await VerifyPlanAsync(applicablePlans, cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                if (transaction is not null) await transaction.DisposeAsync();
            }

            return await GetAsync(run.Id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await MarkFailureAsync(run.Id, "Cancelled", "Sincronizacao cancelada antes da conclusao.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha na sincronizacao de colecoes {RunId}", run.Id);
            await MarkFailureAsync(run.Id, "Failed", exception.Message);
            throw;
        }
        finally
        {
            SynchronizationGate.Release();
        }
    }

    public async Task<CollectionSyncResultDto> GetAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await context.MonitoringSyncRuns.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == runId, cancellationToken)
            ?? throw new KeyNotFoundException();
        var events = await context.MonitoringSyncEvents.AsNoTracking()
            .Where(item => item.RunId == runId)
            .OrderBy(item => item.Id)
            .Select(item => new CollectionSyncEventDto(item.Id, item.OccurredAtUtc, item.Severity, item.Code,
                item.RootKey, item.RelativePath, item.MyAnimeId, item.PreviousRootKey, item.NewRootKey, item.Detail))
            .ToArrayAsync(cancellationToken);
        return ToResult(run, events);
    }

    private async Task ApplyPlanAsync(IReadOnlyList<SyncPlan> plans, CancellationToken cancellationToken)
    {
        foreach (var plan in plans.Where(item => item.Action is SyncAction.Add or SyncAction.Update))
        {
            if (plan.Action == SyncAction.Add)
            {
                if (await context.MonitoringLocations.AnyAsync(item => item.MyAnimeId == plan.MyAnimeId, cancellationToken))
                    throw new InvalidOperationException($"A colecao MyAnimeId {plan.MyAnimeId} foi alterada durante a sincronizacao.");
                context.MonitoringLocations.Add(new MyAnimeMonitoringLocation
                {
                    MyAnimeId = plan.MyAnimeId!.Value,
                    RootKey = plan.Folder.RootKey,
                    RelativePath = plan.Folder.RelativePath
                });
                continue;
            }

            var location = await context.MonitoringLocations.FindAsync([plan.MyAnimeId!.Value], cancellationToken)
                ?? throw new InvalidOperationException($"O vinculo MyAnimeId {plan.MyAnimeId} desapareceu durante a sincronizacao.");
            if (!string.Equals(location.RootKey, plan.Existing!.StoredRootKey, StringComparison.Ordinal)
                || !string.Equals(location.RelativePath, plan.Existing.RelativePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"O vinculo MyAnimeId {plan.MyAnimeId} foi alterado durante a sincronizacao.");
            location.RootKey = plan.Folder.RootKey;
        }
    }

    private async Task VerifyPlanAsync(IReadOnlyList<SyncPlan> plans, CancellationToken cancellationToken)
    {
        var ids = plans.Where(item => item.Action is SyncAction.Add or SyncAction.Update)
            .Select(item => item.MyAnimeId!.Value).Distinct().ToArray();
        if (ids.Length == 0) return;
        var persisted = await context.MonitoringLocations.AsNoTracking()
            .Where(item => ids.Contains(item.MyAnimeId))
            .ToDictionaryAsync(item => item.MyAnimeId, cancellationToken);
        foreach (var plan in plans.Where(item => item.Action is SyncAction.Add or SyncAction.Update))
        {
            if (!persisted.TryGetValue(plan.MyAnimeId!.Value, out var location)
                || !string.Equals(location.RootKey, plan.Folder.RootKey, StringComparison.Ordinal)
                || !string.Equals(location.RelativePath, plan.Folder.RelativePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Verificacao falhou para MyAnimeId {plan.MyAnimeId}.");
        }
    }

    private static List<CollectionSyncIssueDto> ValidateDiscovery(
        CollectionSyncDiscoveryDto discovery,
        IReadOnlyList<MonitoringRootDto> roots,
        IReadOnlyList<StoredMapping> storedMappings)
    {
        var issues = discovery.Issues.ToList();
        var expected = roots.ToDictionary(item => item.Key, StringComparer.Ordinal);
        var actual = discovery.Roots.GroupBy(item => item.Key, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        foreach (var root in roots)
        {
            if (!actual.TryGetValue(root.Key, out var entries) || entries.Length != 1)
            {
                issues.Add(new CollectionSyncIssueDto("Error", "DISCOVERY_ROOT_MISSING", root.Key, "", "A descoberta nao retornou exatamente uma entrada para esta raiz."));
                continue;
            }
            var entry = entries[0];
            if (!string.Equals(entry.Path, root.Path, StringComparison.OrdinalIgnoreCase))
                issues.Add(new CollectionSyncIssueDto("Error", "ROOT_PATH_MISMATCH", root.Key, "", $"Caminho esperado: {root.Path}; recebido: {entry.Path}."));
            if (!entry.Available && !issues.Any(issue => issue.RootKey == root.Key && issue.Code == "ROOT_UNAVAILABLE"))
                issues.Add(new CollectionSyncIssueDto("Error", "ROOT_UNAVAILABLE", root.Key, "", entry.Error ?? "Raiz indisponivel."));
        }
        foreach (var root in discovery.Roots.Where(item => !expected.ContainsKey(item.Key)))
            issues.Add(new CollectionSyncIssueDto("Error", "UNAUTHORIZED_ROOT", root.Key, "", "A descoberta informou uma raiz fora da politica autorizada."));
        foreach (var duplicate in discovery.Folders.GroupBy(item => item.RootKey + "\0" + item.RelativePath, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            issues.Add(new CollectionSyncIssueDto("Error", "DUPLICATE_FOLDER", duplicate.First().RootKey, duplicate.First().RelativePath, "A descoberta informou a mesma pasta mais de uma vez."));
        foreach (var folder in discovery.Folders)
        {
            if (!expected.ContainsKey(folder.RootKey))
                issues.Add(new CollectionSyncIssueDto("Error", "UNAUTHORIZED_ROOT", folder.RootKey, folder.RelativePath, "Pasta fora das raizes autorizadas."));
            else if (!MonitoringPathPolicy.IsValidRelativePath(folder.RelativePath))
                issues.Add(new CollectionSyncIssueDto("Error", "INVALID_RELATIVE_PATH", folder.RootKey, folder.RelativePath, "Caminho relativo invalido."));
            if (folder.CoverIds.Any(id => id <= 0))
                issues.Add(new CollectionSyncIssueDto("Error", "INVALID_COVER_ID", folder.RootKey, folder.RelativePath, "A descoberta informou um ID de capa invalido."));
        }
        foreach (var mapping in storedMappings)
        {
            var normalized = MonitoringPathPolicy.NormalizeRootKey(mapping.RootKey);
            if (!expected.ContainsKey(normalized))
                issues.Add(new CollectionSyncIssueDto("Error", "STALE_MAPPING_ROOT", mapping.RootKey, mapping.RelativePath,
                    $"A chave persistida nao pertence a nenhuma raiz atual: {mapping.RootKey}."));
        }
        return issues;
    }

    private static List<SyncPlan> BuildPlan(
        IReadOnlyList<CollectionSyncFolderDto> folders,
        IReadOnlyList<CatalogItem> catalog,
        IReadOnlyList<StoredMapping> storedMappings)
    {
        var existing = storedMappings.Select(item => new ExistingMapping(
            item.MyAnimeId,
            MonitoringPathPolicy.NormalizeRootKey(item.RootKey),
            item.RelativePath,
            item.RootKey)).ToArray();
        var plans = new List<SyncPlan>();
        foreach (var folder in folders.OrderBy(item => item.RootKey, StringComparer.Ordinal).ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            var (candidates, evidence) = FindCandidates(folder, catalog);

            var atLocation = existing.Where(item => string.Equals(item.RootKey, folder.RootKey, StringComparison.Ordinal)
                && string.Equals(item.RelativePath, folder.RelativePath, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (atLocation.Length > 1)
            {
                plans.Add(new SyncPlan(folder, null, candidates.Select(item => item.Id).Distinct().ToArray(), SyncAction.Conflict, null, "Mais de uma colecao esta associada a esta pasta."));
            }
            else if (atLocation.Length == 1)
            {
                var mapping = atLocation[0];
                plans.Add(new SyncPlan(folder, mapping.MyAnimeId, candidates.Select(item => item.Id).Distinct().ToArray(),
                    string.Equals(mapping.StoredRootKey, folder.RootKey, StringComparison.Ordinal) ? SyncAction.Unchanged : SyncAction.Update,
                    mapping, "Vinculo existente preservado."));
            }
            else if (candidates.Count > 1)
            {
                plans.Add(new SyncPlan(folder, null, candidates.Select(item => item.Id).Distinct().ToArray(), SyncAction.Ambiguous,
                    null, evidence + " Mais de uma colecao permanece candidata."));
            }
            else if (candidates.Count == 1)
            {
                var candidate = candidates[0];
                var mappedElsewhere = existing.Any(item => item.MyAnimeId == candidate.Id);
                plans.Add(new SyncPlan(folder, candidate.Id, [candidate.Id], mappedElsewhere ? SyncAction.Conflict : SyncAction.Add,
                    mappedElsewhere ? existing.First(item => item.MyAnimeId == candidate.Id) : null,
                    mappedElsewhere ? "A colecao ja esta associada a outra pasta." : evidence));
            }
            else
            {
                plans.Add(new SyncPlan(folder, null, [], SyncAction.MissingCatalog, null, evidence));
            }
        }

        foreach (var group in plans.Where(item => item.Action == SyncAction.Add && item.MyAnimeId.HasValue)
            .GroupBy(item => item.MyAnimeId!.Value))
        {
            if (group.Count() <= 1) continue;
            foreach (var plan in group)
            {
                var index = plans.IndexOf(plan);
                plans[index] = plan with { Action = SyncAction.Conflict, Detail = "A mesma colecao possui mais de uma pasta candidata." };
            }
        }
        return plans;
    }

    private static (List<CatalogItem> Candidates, string Evidence) FindCandidates(
        CollectionSyncFolderDto folder,
        IReadOnlyList<CatalogItem> catalog)
    {
        var titleCandidates = catalog.Where(item => NamesShareFirstWords(folder.RelativePath, item.Titulo)).ToList();
        var catalogIds = catalog.SelectMany(item => item.AnimesMalId).Where(id => id > 0).ToHashSet();
        var validImageIds = folder.CoverIds.Where(catalogIds.Contains).Distinct().ToArray();
        if (validImageIds.Length == 0)
        {
            return (titleCandidates, titleCandidates.Count == 0
                ? "Nenhuma imagem com mal_id presente no catalogo e nenhum nome correspondente foi encontrado."
                : "Correspondencia baseada nas primeiras quatro palavras do nome da pasta.");
        }

        var scored = catalog
            .Select(item => new { Item = item, Score = validImageIds.Count(id => item.AnimesMalId.Contains(id)) })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ToArray();
        if (scored.Length == 0)
            return (titleCandidates, "Os mal_id das imagens nao correspondem a nenhuma colecao do catalogo; nome usado como fallback.");

        var bestScore = scored[0].Score;
        var best = scored.Where(item => item.Score == bestScore).Select(item => item.Item).ToList();
        if (best.Count > 1)
        {
            var titleBest = best.Where(item => titleCandidates.Any(candidate => candidate.Id == item.Id)).ToList();
            if (titleBest.Count == 1) best = titleBest;
        }
        var coverage = $"{bestScore} de {validImageIds.Length} mal_id validos das imagens correspondem a colecao";
        return (best, best.Count == 1 ? coverage + "; correspondencia priorizada por imagens." : coverage + "; candidatos empatados.");
    }

    private static bool NamesShareFirstWords(string folderName, string catalogTitle)
    {
        var folderWords = GetWords(folderName);
        var titleWords = GetWords(catalogTitle);
        var count = Math.Min(4, Math.Min(folderWords.Count, titleWords.Count));
        return count > 0 && folderWords.Take(count).SequenceEqual(titleWords.Take(count), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> GetWords(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var words = new List<string>();
        var current = new StringBuilder();
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(character))
            {
                current.Append(char.ToUpperInvariant(character));
                continue;
            }
            if (current.Length > 0) { words.Add(current.ToString()); current.Clear(); }
        }
        if (current.Length > 0) words.Add(current.ToString());
        return words;
    }

    private static List<SyncEventDraft> BuildEvents(
        CollectionSyncDiscoveryDto discovery,
        IReadOnlyList<CollectionSyncIssueDto> validationIssues,
        IReadOnlyList<SyncPlan> plans,
        IReadOnlyList<StoredMapping> storedMappings)
    {
        var events = new List<SyncEventDraft>();
        foreach (var root in discovery.Roots.OrderBy(item => item.Key, StringComparer.Ordinal))
            events.Add(new SyncEventDraft(root.Available ? "Info" : "Error", root.Available ? "ROOT_AVAILABLE" : "ROOT_UNAVAILABLE",
                root.Key, "", null, null, null, root.Error ?? "Raiz disponivel."));
        foreach (var issue in validationIssues)
            events.Add(new SyncEventDraft(issue.Severity, issue.Code, issue.RootKey, issue.RelativePath, null, null, null, issue.Detail));
        foreach (var plan in plans)
        {
            var (severity, code) = plan.Action switch
            {
                SyncAction.Add => ("Info", "MAPPING_ADDED"),
                SyncAction.Update => ("Info", "MAPPING_UPDATED"),
                SyncAction.Unchanged => ("Info", "MAPPING_UNCHANGED"),
                SyncAction.Ambiguous => ("Warning", "AMBIGUOUS_FOLDER"),
                SyncAction.MissingCatalog => ("Warning", "MISSING_CATALOG"),
                SyncAction.Conflict => ("Error", "MAPPING_CONFLICT"),
                _ => ("Error", "UNKNOWN_SYNC_ACTION")
            };
            var candidateDetail = plan.CandidateIds.Count == 0 ? "" : $" Candidatos: {string.Join(", ", plan.CandidateIds)}.";
            var imageIds = plan.Folder.CoverIds.Take(20).ToArray();
            var imageDetail = imageIds.Length == 0 ? "" : $" Imagens numericas: {string.Join(", ", imageIds)}{(plan.Folder.CoverIds.Count > imageIds.Length ? ", ..." : ".")}";
            var detail = plan.Detail + candidateDetail + imageDetail;
            events.Add(new SyncEventDraft(severity, code, plan.Folder.RootKey, plan.Folder.RelativePath,
                plan.MyAnimeId, plan.Existing?.StoredRootKey, plan.Action == SyncAction.Update ? plan.Folder.RootKey : null, detail));
        }
        var discoveredLocations = discovery.Folders
            .Select(folder => folder.RootKey + "\0" + folder.RelativePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in storedMappings)
        {
            var normalizedRoot = MonitoringPathPolicy.NormalizeRootKey(mapping.RootKey);
            if (!discoveredLocations.Contains(normalizedRoot + "\0" + mapping.RelativePath))
            {
                events.Add(new SyncEventDraft("Warning", "EXISTING_MAPPING_NOT_DISCOVERED", normalizedRoot,
                    mapping.RelativePath, mapping.MyAnimeId, mapping.RootKey,
                    normalizedRoot.Equals(mapping.RootKey, StringComparison.Ordinal) ? null : normalizedRoot,
                    "O vinculo existente nao foi encontrado na descoberta atual; nenhuma alteracao foi aplicada."));
            }
        }
        return events;
    }

    private static void FillRunSummary(MyAnimeMonitoringSyncRun run, CollectionSyncDiscoveryDto discovery,
        IReadOnlyList<SyncPlan> plans, IReadOnlyList<SyncEventDraft> events)
    {
        run.AuthorizedRoots = MonitoringPathPolicy.CollectionRoots().Count;
        run.AvailableRoots = discovery.Roots.Count(item => item.Available);
        run.DiscoveredFolders = discovery.Folders.Count;
        run.AddedMappings = plans.Count(item => item.Action == SyncAction.Add);
        run.UpdatedMappings = plans.Count(item => item.Action == SyncAction.Update);
        run.UnchangedMappings = plans.Count(item => item.Action == SyncAction.Unchanged);
        run.MissingCatalog = plans.Count(item => item.Action == SyncAction.MissingCatalog);
        run.AmbiguousFolders = plans.Count(item => item.Action == SyncAction.Ambiguous);
        run.Conflicts = plans.Count(item => item.Action == SyncAction.Conflict);
        run.Errors = events.Count(item => item.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
        run.Warnings = events.Count(item => item.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase));
    }

    private async Task MarkFailureAsync(Guid runId, string status, string error)
    {
        try
        {
            context.ChangeTracker.Clear();
            var run = await context.MonitoringSyncRuns.SingleOrDefaultAsync(item => item.Id == runId);
            if (run is null) return;
            run.Status = status;
            run.Error = Limit(error);
            run.FinishedAtUtc = DateTimeOffset.UtcNow;
            run.Errors++;
            context.MonitoringSyncEvents.Add(new MyAnimeMonitoringSyncEvent
            {
                RunId = runId, OccurredAtUtc = DateTimeOffset.UtcNow, Severity = "Error", Code = "SYNC_FAILED",
                Detail = Limit(error)
            });
            await context.SaveChangesAsync();
        }
        catch (Exception loggingException)
        {
            logger.LogError(loggingException, "Nao foi possivel registrar a falha da sincronizacao {RunId}", runId);
        }
    }

    private static MyAnimeMonitoringSyncEvent ToEntity(Guid runId, SyncEventDraft item) => new()
    {
        RunId = runId, OccurredAtUtc = DateTimeOffset.UtcNow, Severity = item.Severity, Code = item.Code,
        RootKey = item.RootKey, RelativePath = item.RelativePath, MyAnimeId = item.MyAnimeId,
        PreviousRootKey = item.PreviousRootKey, NewRootKey = item.NewRootKey, Detail = Limit(item.Detail)
    };

    private static CollectionSyncResultDto ToResult(MyAnimeMonitoringSyncRun run, IReadOnlyList<CollectionSyncEventDto> events) =>
        new(run.Id, run.Status, run.StartedAtUtc, run.FinishedAtUtc, run.AuthorizedRoots, run.AvailableRoots,
            run.DiscoveredFolders, run.AddedMappings, run.UpdatedMappings, run.UnchangedMappings, run.MissingCatalog,
            run.AmbiguousFolders, run.Conflicts, run.Errors, run.Warnings, events);

    private static string ToCollectionFolderName(string title)
    {
        var name = title.Trim();
        foreach (var character in Path.GetInvalidFileNameChars()) name = name.Replace(character, ' ');
        while (name.Contains("  ", StringComparison.Ordinal)) name = name.Replace("  ", " ", StringComparison.Ordinal);
        name = name.Trim().TrimEnd('.', ' ');
        if (name.Length > 255) name = name[..255].TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(name)) return "SemNome";
        var reserved = new[] { "CON", "PRN", "AUX", "NUL" }.Concat(Enumerable.Range(1, 9).SelectMany(number => new[] { $"COM{number}", $"LPT{number}" }));
        return reserved.Contains(Path.GetFileNameWithoutExtension(name), StringComparer.OrdinalIgnoreCase) ? "_" + name : name;
    }

    private static string Limit(string value) => value.Length > 2000 ? value[..2000] : value;

    private sealed record CatalogItem(int Id, string Titulo, List<int> AnimesMalId);
    private sealed record StoredMapping(int MyAnimeId, string RootKey, string RelativePath);
    private sealed record ExistingMapping(int MyAnimeId, string RootKey, string RelativePath, string StoredRootKey);
    private sealed record SyncEventDraft(string Severity, string Code, string RootKey, string RelativePath, int? MyAnimeId,
        string? PreviousRootKey, string? NewRootKey, string Detail);
    private sealed record SyncPlan(CollectionSyncFolderDto Folder, int? MyAnimeId, IReadOnlyList<int> CandidateIds,
        SyncAction Action, ExistingMapping? Existing, string Detail);
    private enum SyncAction { Add, Update, Unchanged, MissingCatalog, Ambiguous, Conflict }
}
