using System.Security.Cryptography;
using System.Text;
using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ApiMyAnimes.Services;

/// <summary>
/// Exportacao e exclusao (LGPD) dos dados pessoais de UMA conta na ApiMyAnimes: progresso de episodios vistos
/// (<c>AnimeEpisodeWatches</c>) e o vinculo da conta com execucoes de atualizacao de status de pasta (<c>RequestedBy</c>).
/// Toda consulta e filtrada pelo <c>accountId</c>; nada de outra conta e lido ou alterado e nenhum caminho fisico e exposto.
/// A trilha <c>SecurityAuditEvents</c> e append-only, nao guarda o identificador da conta (somente um hash curto) e segue a retencao propria.
/// </summary>
/// <param name="context">Contexto do banco local.</param>
/// <param name="auditWriter">Escritor da trilha de auditoria.</param>
public sealed class AccountPrivacyService(MyAnimesContext context, ISecurityAuditWriter auditWriter)
{
    /// <summary>Valor gravado em <c>RequestedBy</c> quando a conta e excluida (anonimizacao; a execucao em si e dado global).</summary>
    public const string AnonymizedRequester = "deleted-account";

    private const int EraseBatchSize = 1000;

    /// <summary>Exporta os dados pessoais da conta.</summary>
    public async Task<MyAnimesPrivacyExportDto> ExportAsync(
        string accountId,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var watches = await context.AnimeEpisodeWatches
            .AsNoTracking()
            .Where(watch => watch.AccountId == accountId)
            .OrderBy(watch => watch.MalId)
            .ThenBy(watch => watch.EpisodeNumber)
            .Select(watch => new { watch.MalId, watch.EpisodeNumber, watch.WatchedAtUtc })
            .ToListAsync(cancellationToken);

        var progress = watches
            .GroupBy(watch => watch.MalId)
            .Select(group => new MyAnimesPrivacyAnimeProgressDto(
                group.Key,
                group.Select(watch => new MyAnimesPrivacyEpisodeDto(watch.EpisodeNumber, AsUtc(watch.WatchedAtUtc))).ToList()))
            .ToList();

        var runs = await context.AnimeFolderRefreshRuns
            .AsNoTracking()
            .Where(run => run.RequestedBy == accountId)
            .OrderBy(run => run.StartedAtUtc)
            .Select(run => new MyAnimesPrivacyFolderRefreshRunDto(
                run.Id, run.StartedAtUtc, run.FinishedAtUtc, run.Status, run.MyAnimeId, run.TotalAnimes, run.ProcessedAnimes))
            .ToListAsync(cancellationToken);

        await RecordAuditAsync(actor, "privacy.export", accountId, "Success", correlationId,
            $"episodes={watches.Count};runs={runs.Count}", cancellationToken);

        return new MyAnimesPrivacyExportDto(MyAnimesPrivacyContract.ExportSchemaVersion, progress, runs);
    }

    /// <summary>
    /// Apaga o progresso da conta e anonimiza <c>RequestedBy</c> das execucoes que ela iniciou. Idempotente: sem dado,
    /// retorna zeros (nunca erro). Em lotes, para nao manter muitas linhas rastreadas de uma vez.
    /// </summary>
    public async Task<MyAnimesPrivacyEraseResultDto> EraseAsync(
        string accountId,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var deletedEpisodes = 0;
        while (true)
        {
            var batch = await context.AnimeEpisodeWatches
                .Where(watch => watch.AccountId == accountId)
                .Take(EraseBatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0) break;

            context.AnimeEpisodeWatches.RemoveRange(batch);
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            deletedEpisodes += batch.Count;
        }

        var anonymizedRuns = 0;
        while (true)
        {
            var batch = await context.AnimeFolderRefreshRuns
                .Where(run => run.RequestedBy == accountId)
                .Take(EraseBatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0) break;

            foreach (var run in batch) run.RequestedBy = AnonymizedRequester;
            await context.SaveChangesAsync(cancellationToken);
            context.ChangeTracker.Clear();
            anonymizedRuns += batch.Count;
        }

        await RecordAuditAsync(actor, "privacy.erase", accountId, "Success", correlationId,
            $"episodes={deletedEpisodes};runs={anonymizedRuns}", cancellationToken);

        return new MyAnimesPrivacyEraseResultDto(deletedEpisodes, anonymizedRuns);
    }

    private Task RecordAuditAsync(
        string actor,
        string action,
        string accountId,
        string result,
        string correlationId,
        string reason,
        CancellationToken cancellationToken) =>
        auditWriter.RecordAsync(
            new SecurityAuditEntry(
                actor,
                action,
                $"account-hash:{HashAccount(accountId)}",
                result,
                "internal-service",
                correlationId,
                reason),
            cancellationToken);

    /// <summary>Hash curto (nao reversivel na pratica) para correlacionar eventos sem gravar o identificador da conta.</summary>
    private static string HashAccount(string accountId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(accountId)))[..16];

    private static DateTimeOffset AsUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
