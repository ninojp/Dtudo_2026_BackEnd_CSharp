using LibDtudo.Shared.Dtos.FileMonitoring;

namespace ApiFileStorage.Monitoring;

public sealed record MonitoringRoot(string Key, string Path);

public sealed class MonitoringRoots
{
    private readonly IReadOnlyDictionary<string, MonitoringRoot> roots;

    public MonitoringRoots(IEnumerable<MonitoringRoot> configuredRoots)
    {
        roots = configuredRoots.ToDictionary(root => root.Key, root => root with
        {
            Path = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(root.Path))
        }, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<MonitoringRoot> All => roots.Values.ToArray();

    public static MonitoringRoots Collections() => new(MonitoringPathPolicy.CollectionRoots().Select(root => new MonitoringRoot(root.Key, root.Path)));

    public string Resolve(string rootKey, string relativePath)
    {
        if (!roots.TryGetValue(rootKey, out var root))
            throw new ArgumentException("Raiz de monitoramento desconhecida.");
        if (!MonitoringPathPolicy.IsValidRelativePath(relativePath, allowEmpty: true))
            throw new ArgumentException("Caminho relativo invalido.");
        if (relativePath.Length == 0) return root.Path;
        var fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(root.Path, relativePath));
        if (!fullPath.StartsWith(root.Path + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Caminho fora da raiz permitida.");
        return fullPath;
    }

    public static bool IsExcluded(string name) => MonitoringPathPolicy.IsExcluded(name);
}
