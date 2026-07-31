namespace LibDtudo.Shared.Search;

/// <summary>
/// Compara títulos para impedir cadastros duplicados, exigindo equivalência
/// exata após a normalização compartilhada da busca local.
/// </summary>
public static class AnimeTitleEquivalence
{
    public static bool AreEquivalent(string? left, string? right)
    {
        var leftNormalized = AnimeSearchTextNormalizer.Normalize(left);
        var rightNormalized = AnimeSearchTextNormalizer.Normalize(right);

        return !leftNormalized.IsEmpty
            && leftNormalized.Value.Equals(rightNormalized.Value, StringComparison.Ordinal);
    }

    public static string? FindEquivalentTitle(
        IEnumerable<string?> candidateTitles,
        IEnumerable<string?> existingTitles)
    {
        var normalizedCandidateTitles = candidateTitles
            .Select(AnimeSearchTextNormalizer.Normalize)
            .Where(title => !title.IsEmpty)
            .Select(title => title.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (normalizedCandidateTitles.Count == 0)
            return null;

        return existingTitles.FirstOrDefault(title =>
        {
            var normalizedTitle = AnimeSearchTextNormalizer.Normalize(title);
            return !normalizedTitle.IsEmpty
                && normalizedCandidateTitles.Contains(normalizedTitle.Value);
        });
    }
}
