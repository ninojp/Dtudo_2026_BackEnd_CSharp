using LibDtudo.Shared.Dtos.FileMonitoring;

namespace ApiFileStorage.Monitoring.Relocation;

public sealed record RootKeyTranslation(string From, string To);

public static class MonitoringRootKeyPlan
{
    public static IReadOnlyList<RootKeyTranslation> Plan(IEnumerable<string?> storedKeys) =>
        storedKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key!)
            .Distinct(StringComparer.Ordinal)
            .Select(key => new RootKeyTranslation(key, MonitoringPathPolicy.NormalizeRootKey(key)))
            .Where(translation => !string.Equals(translation.From, translation.To, StringComparison.Ordinal))
            .OrderBy(translation => translation.From, StringComparer.Ordinal)
            .ToArray();
}
