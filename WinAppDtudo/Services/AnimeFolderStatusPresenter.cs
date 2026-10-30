using System.Drawing;
using System.Globalization;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.FileMonitoring;

namespace WinAppDtudo.Services;

/// <summary>Linha do relatorio: resultado da verificacao de pasta de um anime, ja enriquecida com titulos e ultima verificacao.</summary>
/// <param name="State">Estado da pasta (none/partial/complete); nulo quando nao ha status conhecido para o anime.</param>
/// <param name="PreservedPrevious">Verdadeiro quando a verificacao nao conseguiu concluir e o status anterior foi mantido.</param>
public sealed record AnimeFolderStatusRow(
    int MalId,
    int MyAnimeId,
    string CollectionTitle,
    string Title,
    string? State,
    int? Videos,
    int? Expected,
    DateTime? CheckedAtUtc,
    string Outcome,
    string Detail,
    bool PreservedPrevious);

/// <summary>Filtros do relatorio na tela de status das pastas.</summary>
public enum AnimeFolderStatusFilter
{
    All,
    Complete,
    Partial,
    None,
    Attention
}

/// <summary>Texto, formato e cor do estado de pasta. Cores iguais as do site (cinza, azul e verde), sobre fundo escuro.</summary>
public static class AnimeFolderStatusPresenter
{
    public const string Placeholder = "—";

    public static string StateText(string? state) =>
        state is null
            ? Placeholder
            : AnimeFolderState.Normalize(state) switch
            {
                AnimeFolderState.Complete => "Completo",
                AnimeFolderState.Partial => "Parcial",
                _ => "Sem pasta"
            };

    public static Color StateColor(string? state) =>
        state is null
            ? DarkModeColors.FolderStatusNoneColor
            : AnimeFolderState.Normalize(state) switch
            {
                AnimeFolderState.Complete => DarkModeColors.FolderStatusCompleteColor,
                AnimeFolderState.Partial => DarkModeColors.FolderStatusPartialColor,
                _ => DarkModeColors.FolderStatusNoneColor
            };

    /// <summary>"12/26" (videos/episodios), "12/—" sem total de episodios conhecido ou "—" sem contagem.</summary>
    public static string FormatVideos(int? videos, int? expected)
    {
        if (videos is null) return Placeholder;
        var total = expected is > 0 ? expected.Value.ToString(CultureInfo.InvariantCulture) : Placeholder;
        return string.Create(CultureInfo.InvariantCulture, $"{videos.Value}/{total}");
    }

    public static string FormatCheckedAt(DateTime? checkedAtUtc) =>
        checkedAtUtc is { } value
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"))
            : Placeholder;

    /// <summary>Texto curto do resultado da verificacao do anime (coluna "Resultado" e mensagem de progresso).</summary>
    public static string OutcomeText(AnimeFolderStatusRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var detail = string.IsNullOrWhiteSpace(row.Detail) ? string.Empty : $": {row.Detail.Trim()}";
        return row.Outcome switch
        {
            AnimeFolderRefreshOutcomes.Updated => row.State is null
                ? "atualizado"
                : $"{StateText(row.State).ToLowerInvariant()} ({FormatVideos(row.Videos, row.Expected)} vídeos)",
            AnimeFolderRefreshOutcomes.NotFound => "pasta não encontrada",
            AnimeFolderRefreshOutcomes.NoLocation => "sem local de armazenamento configurado",
            AnimeFolderRefreshOutcomes.Unavailable => $"indisponível, status anterior mantido{detail}",
            AnimeFolderRefreshOutcomes.Failed => $"falhou, status anterior mantido{detail}",
            _ => $"{row.Outcome}{detail}"
        };
    }

    public static bool Matches(AnimeFolderStatusRow row, AnimeFolderStatusFilter filter)
    {
        ArgumentNullException.ThrowIfNull(row);
        return filter switch
        {
            AnimeFolderStatusFilter.All => true,
            AnimeFolderStatusFilter.Attention => row.PreservedPrevious,
            AnimeFolderStatusFilter.Complete => !row.PreservedPrevious && AnimeFolderState.Normalize(row.State) == AnimeFolderState.Complete,
            AnimeFolderStatusFilter.Partial => !row.PreservedPrevious && AnimeFolderState.Normalize(row.State) == AnimeFolderState.Partial,
            AnimeFolderStatusFilter.None => !row.PreservedPrevious && AnimeFolderState.Normalize(row.State) == AnimeFolderState.None,
            _ => true
        };
    }

    /// <summary>Resumo final, ex.: "Concluído: 70 completos, 10 parciais, 7 sem pasta".</summary>
    public static string BuildSummary(
        AnimeFolderRefreshRunDto run, IReadOnlyCollection<AnimeFolderStatusRow> rows, bool cancelledByUser)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(rows);

        var complete = rows.Count(row => !row.PreservedPrevious && AnimeFolderState.Normalize(row.State) == AnimeFolderState.Complete);
        var partial = rows.Count(row => !row.PreservedPrevious && AnimeFolderState.Normalize(row.State) == AnimeFolderState.Partial);
        var none = rows.Count(row => !row.PreservedPrevious && AnimeFolderState.Normalize(row.State) == AnimeFolderState.None);
        var unavailable = rows.Count(row => string.Equals(row.Outcome, AnimeFolderRefreshOutcomes.Unavailable, StringComparison.OrdinalIgnoreCase));
        var failed = rows.Count(row => string.Equals(row.Outcome, AnimeFolderRefreshOutcomes.Failed, StringComparison.OrdinalIgnoreCase));

        var parts = new List<string>
        {
            Plural(complete, "completo", "completos"),
            Plural(partial, "parcial", "parciais"),
            $"{none} sem pasta"
        };
        if (unavailable > 0) parts.Add($"{Plural(unavailable, "indisponível", "indisponíveis")} (status anterior mantido)");
        if (failed > 0) parts.Add($"{Plural(failed, "falha", "falhas")} (status anterior mantido)");
        var counts = string.Join(", ", parts);

        var total = Math.Max(run.Total, rows.Count);
        var prefix = (run.Status, cancelledByUser) switch
        {
            (_, true) or (AnimeFolderRefreshStatuses.Cancelled, _) =>
                $"Cancelado após {rows.Count} de {total} animes (os já verificados permanecem gravados)",
            (AnimeFolderRefreshStatuses.Failed, _) => "A verificação falhou",
            (AnimeFolderRefreshStatuses.Interrupted, _) => $"Verificação interrompida após {rows.Count} de {total} animes",
            (AnimeFolderRefreshStatuses.CompletedWithErrors, _) => "Concluído com ressalvas",
            (AnimeFolderRefreshStatuses.Running, _) => $"Em andamento ({rows.Count} de {total} animes)",
            _ => "Concluído"
        };

        var summary = $"{prefix}: {counts}";
        if (!string.IsNullOrWhiteSpace(run.Error)) summary += $"\n{run.Error.Trim()}";
        return summary;
    }

    private static string Plural(int count, string singular, string plural) =>
        $"{count} {(count == 1 ? singular : plural)}";
}
