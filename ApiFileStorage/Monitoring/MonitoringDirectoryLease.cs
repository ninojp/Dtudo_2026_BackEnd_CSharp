using ApiFileStorage.Services;

namespace ApiFileStorage.Monitoring;

internal sealed class MonitoringDirectoryLease : IDisposable
{
    private readonly List<OpenedStorageHandle> handles = [];

    public static MonitoringDirectoryLease Open(string path)
    {
        var lease = new MonitoringDirectoryLease();
        try
        {
            var ancestors = new Stack<string>();
            for (var directory = new DirectoryInfo(path); directory is not null; directory = directory.Parent)
                ancestors.Push(directory.FullName);
            while (ancestors.TryPop(out var directory))
            {
                var handle = WindowsFileSystem.OpenAbsolute(directory);
                lease.handles.Add(handle);
                if (!handle.Information.IsDirectory || !string.Equals(StorageRootCatalog.NormalizeComparablePath(handle.FinalPath),
                    StorageRootCatalog.NormalizeComparablePath(directory), StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Diretorio sem resolucao fisica estavel.");
            }
            return lease;
        }
        catch (Exception exception)
        {
            lease.Dispose();
            throw new IOException("Nao foi possivel ler o diretorio sem redirecionamento: " + path, exception);
        }
    }

    public void Dispose()
    {
        for (var index = handles.Count - 1; index >= 0; index--) handles[index].Dispose();
        handles.Clear();
    }
}
