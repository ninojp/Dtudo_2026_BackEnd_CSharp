using LibDtudo.Shared.Dtos.FileMonitoring;

namespace ApiFileStorage.Monitoring;

/// <summary>Descobre candidatos de colecao sem alterar arquivos ou diretorios.</summary>
public sealed class CollectionSyncDiscoveryService(MonitoringRoots roots)
{
    public Task<CollectionSyncDiscoveryDto> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var discoveredRoots = new List<CollectionSyncRootDto>();
        var folders = new List<CollectionSyncFolderDto>();
        var issues = new List<CollectionSyncIssueDto>();

        foreach (var root in roots.All.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                MonitoringReader.EnsureNoRedirectedAncestors(root.Path);
                DiscoverRoot(root, folders, issues, cancellationToken);
                discoveredRoots.Add(new CollectionSyncRootDto(root.Key, root.Path, true, null));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                discoveredRoots.Add(new CollectionSyncRootDto(root.Key, root.Path, false, Limit(exception.Message)));
                issues.Add(new CollectionSyncIssueDto("Error", "ROOT_UNAVAILABLE", root.Key, "", Limit(exception.Message)));
            }
        }

        return Task.FromResult(new CollectionSyncDiscoveryDto(
            startedAtUtc,
            DateTimeOffset.UtcNow,
            discoveredRoots,
            folders,
            issues));
    }

    private static void DiscoverRoot(
        MonitoringRoot root,
        List<CollectionSyncFolderDto> folders,
        List<CollectionSyncIssueDto> issues,
        CancellationToken cancellationToken)
    {
        var rootDirectory = new DirectoryInfo(root.Path);
        foreach (var item in rootDirectory.EnumerateFileSystemInfos("*", EnumerationOptions()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((item.Attributes & FileAttributes.Directory) == 0 || MonitoringRoots.IsExcluded(item.Name)) continue;
            var relativePath = item.Name;
            if ((item.Attributes & (FileAttributes.ReparsePoint | FileAttributes.System)) != 0)
            {
                issues.Add(new CollectionSyncIssueDto("Warning", "SKIPPED_DIRECTORY", root.Key, relativePath,
                    "Diretorio de sistema ou link ignorado."));
                continue;
            }

            if (!MonitoringPathPolicy.IsValidRelativePath(relativePath))
            {
                issues.Add(new CollectionSyncIssueDto("Error", "INVALID_FOLDER_NAME", root.Key, relativePath,
                    "Nome de pasta invalido para um caminho relativo de monitoramento."));
                continue;
            }

            var coverIds = new HashSet<int>();
            try
            {
                CollectCoverIds(item.FullName, coverIds, issues, root.Key, relativePath, cancellationToken);
                folders.Add(new CollectionSyncFolderDto(root.Key, relativePath, coverIds.OrderBy(id => id).ToArray()));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                issues.Add(new CollectionSyncIssueDto("Error", "FOLDER_READ_FAILED", root.Key, relativePath, Limit(exception.Message)));
            }
        }
    }

    private static void CollectCoverIds(
        string collectionPath,
        HashSet<int> coverIds,
        List<CollectionSyncIssueDto> issues,
        string rootKey,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var pending = new Stack<string>();
        pending.Push(collectionPath);
        while (pending.TryPop(out var currentPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                foreach (var item in new DirectoryInfo(currentPath).EnumerateFileSystemInfos("*", EnumerationOptions()))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var itemRelativePath = Path.GetRelativePath(collectionPath, item.FullName);
                    if ((item.Attributes & (FileAttributes.ReparsePoint | FileAttributes.System)) != 0)
                    {
                        if ((item.Attributes & FileAttributes.Directory) != 0)
                            issues.Add(new CollectionSyncIssueDto("Warning", "SKIPPED_DIRECTORY", rootKey,
                                relativePath + "\\" + itemRelativePath, "Subdiretorio de sistema ou link ignorado."));
                        continue;
                    }
                    if ((item.Attributes & FileAttributes.Directory) != 0)
                    {
                        if (!MonitoringRoots.IsExcluded(item.Name)) pending.Push(item.FullName);
                        continue;
                    }
                    if (!ImageExtensions.Contains(Path.GetExtension(item.Name))) continue;
                    var imageName = Path.GetFileNameWithoutExtension(item.Name);
                    if (int.TryParse(imageName, out var id) && id > 0) coverIds.Add(id);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
            {
                var currentRelativePath = Path.GetRelativePath(collectionPath, currentPath);
                issues.Add(new CollectionSyncIssueDto("Warning", "SUBFOLDER_READ_FAILED", rootKey,
                    relativePath + (currentRelativePath == "." ? "" : "\\" + currentRelativePath), Limit(exception.Message)));
            }
        }
    }

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"
    };

    private static EnumerationOptions EnumerationOptions() => new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = false,
        AttributesToSkip = 0,
        ReturnSpecialDirectories = false
    };

    private static string Limit(string value) => value.Length > 2000 ? value[..2000] : value;
}
