using ApiMyAnimes.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Services;

/// <summary>
/// Progresso de episodios vistos por usuario. Toda operacao recebe o <c>accountId</c> (claim <c>sub</c> do token)
/// e so le ou altera linhas dessa conta.
/// </summary>
/// <param name="context">Contexto do banco local.</param>
/// <param name="timeProvider">Relogio usado em <c>WatchedAtUtc</c>.</param>
public sealed class AnimeWatchService(MyAnimesContext context, TimeProvider timeProvider)
{
    private const int MaxSaveAttempts = 3;

    private sealed record AnimeTotals(int? Episodes, int Episodios)
    {
        public int? Total => AnimeStatusRules.ResolveTotalEpisodes(Episodes, Episodios);
    }

    /// <summary>Lista os episodios vistos do anime pela conta.</summary>
    public async Task<AnimeWatchResult> GetAsync(string accountId, int malId, CancellationToken cancellationToken)
    {
        var anime = await FindAnimeAsync(malId, cancellationToken);
        if (anime is null) return AnimeWatchResult.NotFound;

        return AnimeWatchResult.Success(await BuildDetailAsync(accountId, malId, anime.Total, cancellationToken));
    }

    /// <summary>Marca os episodios como vistos. Idempotente: episodios ja vistos nao mudam nem duplicam.</summary>
    public async Task<AnimeWatchResult> MarkEpisodesAsync(
        string accountId,
        int malId,
        IReadOnlyCollection<int>? episodes,
        CancellationToken cancellationToken)
    {
        var anime = await FindAnimeAsync(malId, cancellationToken);
        if (anime is null) return AnimeWatchResult.NotFound;

        if (!AnimeWatchRules.TryNormalizeToMark(episodes, anime.Total, out var normalized, out var error))
            return Invalid(error);

        await InsertMissingAsync(accountId, malId, normalized, cancellationToken);
        return AnimeWatchResult.Success(await BuildDetailAsync(accountId, malId, anime.Total, cancellationToken));
    }

    /// <summary>Marca todos os episodios (1..total). Exige total conhecido.</summary>
    public async Task<AnimeWatchResult> MarkAllAsync(string accountId, int malId, CancellationToken cancellationToken)
    {
        var anime = await FindAnimeAsync(malId, cancellationToken);
        if (anime is null) return AnimeWatchResult.NotFound;

        if (anime.Total is not { } total)
            return new AnimeWatchResult(AnimeWatchOutcome.TotalUnknown, Error: "O total de episodios deste anime e desconhecido.");

        if (total > AnimeWatchRules.MaxEpisodeNumber)
            return Invalid($"O total de episodios ({total}) excede o limite de {AnimeWatchRules.MaxEpisodeNumber}.");

        await InsertMissingAsync(accountId, malId, Enumerable.Range(1, total).ToArray(), cancellationToken);
        return AnimeWatchResult.Success(await BuildDetailAsync(accountId, malId, total, cancellationToken));
    }

    /// <summary>Desmarca os episodios informados. Lista vazia nao apaga nada.</summary>
    public async Task<AnimeWatchResult> UnmarkEpisodesAsync(
        string accountId,
        int malId,
        IReadOnlyCollection<int>? episodes,
        CancellationToken cancellationToken)
    {
        var anime = await FindAnimeAsync(malId, cancellationToken);
        if (anime is null) return AnimeWatchResult.NotFound;

        if (!AnimeWatchRules.TryNormalizeToUnmark(episodes, out var normalized, out var error))
            return Invalid(error);

        if (normalized.Count > 0)
        {
            await PersistAsync(async () =>
            {
                var rows = await context.AnimeEpisodeWatches
                    .Where(watch => watch.AccountId == accountId
                        && watch.MalId == malId
                        && normalized.Contains(watch.EpisodeNumber))
                    .ToListAsync(cancellationToken);
                context.AnimeEpisodeWatches.RemoveRange(rows);
            }, cancellationToken);
        }

        return AnimeWatchResult.Success(await BuildDetailAsync(accountId, malId, anime.Total, cancellationToken));
    }

    /// <summary>Remove todo o progresso da conta neste anime.</summary>
    public async Task<AnimeWatchResult> ClearAsync(string accountId, int malId, CancellationToken cancellationToken)
    {
        var anime = await FindAnimeAsync(malId, cancellationToken);
        if (anime is null) return AnimeWatchResult.NotFound;

        await PersistAsync(async () =>
        {
            var rows = await context.AnimeEpisodeWatches
                .Where(watch => watch.AccountId == accountId && watch.MalId == malId)
                .ToListAsync(cancellationToken);
            context.AnimeEpisodeWatches.RemoveRange(rows);
        }, cancellationToken);

        return AnimeWatchResult.Success(await BuildDetailAsync(accountId, malId, anime.Total, cancellationToken));
    }

    /// <summary>
    /// Quantidade de episodios vistos por anime, em uma unica consulta agregada (GROUP BY MalId) para a conta.
    /// Episodios acima do total atual do anime (total reduzido depois da marcacao) nao contam.
    /// Animes sem nenhum episodio visto nao aparecem no resultado.
    /// </summary>
    public async Task<Dictionary<int, int>> GetWatchedCountsAsync(
        string accountId,
        IReadOnlyCollection<int> malIds,
        CancellationToken cancellationToken)
    {
        if (malIds.Count == 0) return [];

        return await (
            from watch in context.AnimeEpisodeWatches.AsNoTracking()
            where watch.AccountId == accountId && malIds.Contains(watch.MalId)
            join anime in context.Animes.AsNoTracking() on watch.MalId equals anime.MalId
            let total = anime.Episodes > 0
                ? anime.Episodes
                : anime.Episodios > 1 ? anime.Episodios : (int?)null
            where total == null || watch.EpisodeNumber <= total
            group watch by watch.MalId into grouped
            select new { MalId = grouped.Key, Count = grouped.Count() })
            .ToDictionaryAsync(item => item.MalId, item => item.Count, cancellationToken);
    }

    private static AnimeWatchResult Invalid(string? error) => new(AnimeWatchOutcome.InvalidEpisodes, Error: error);

    private Task<AnimeTotals?> FindAnimeAsync(int malId, CancellationToken cancellationToken) =>
        context.Animes
            .AsNoTracking()
            .Where(anime => anime.MalId == malId)
            .Select(anime => new AnimeTotals(anime.Episodes, anime.Episodios))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task InsertMissingAsync(
        string accountId,
        int malId,
        IReadOnlyList<int> episodes,
        CancellationToken cancellationToken)
    {
        if (episodes.Count == 0) return;

        await PersistAsync(async () =>
        {
            var existing = await context.AnimeEpisodeWatches
                .AsNoTracking()
                .Where(watch => watch.AccountId == accountId
                    && watch.MalId == malId
                    && episodes.Contains(watch.EpisodeNumber))
                .Select(watch => watch.EpisodeNumber)
                .ToListAsync(cancellationToken);
            var existingSet = existing.ToHashSet();
            var now = timeProvider.GetUtcNow().UtcDateTime;

            context.AnimeEpisodeWatches.AddRange(episodes
                .Where(episode => !existingSet.Contains(episode))
                .Select(episode => new AnimeEpisodeWatch
                {
                    AccountId = accountId,
                    MalId = malId,
                    EpisodeNumber = episode,
                    WatchedAtUtc = now
                }));
        }, cancellationToken);
    }

    /// <summary>
    /// Prepara as alteracoes e salva. Requisicoes paralelas da mesma conta podem colidir na chave ou apagar a mesma linha;
    /// como as operacoes sao idempotentes, descarta o estado rastreado e refaz a preparacao algumas vezes.
    /// </summary>
    private async Task PersistAsync(Func<Task> stage, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            await stage();
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException) when (attempt < MaxSaveAttempts)
            {
                context.ChangeTracker.Clear();
            }
        }
    }

    private async Task<AnimeWatchDetailDto> BuildDetailAsync(
        string accountId,
        int malId,
        int? total,
        CancellationToken cancellationToken)
    {
        var query = context.AnimeEpisodeWatches
            .AsNoTracking()
            .Where(watch => watch.AccountId == accountId && watch.MalId == malId);
        if (total is { } known) query = query.Where(watch => watch.EpisodeNumber <= known);

        var episodes = await query
            .OrderBy(watch => watch.EpisodeNumber)
            .Select(watch => watch.EpisodeNumber)
            .ToListAsync(cancellationToken);

        return new AnimeWatchDetailDto(
            malId,
            total,
            episodes.Count,
            AnimeStatusRules.ResolveWatchState(episodes.Count, total),
            episodes);
    }
}
