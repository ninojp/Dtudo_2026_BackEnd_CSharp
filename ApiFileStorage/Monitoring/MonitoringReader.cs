using ApiFileStorage.Monitoring.Data;

namespace ApiFileStorage.Monitoring;

public sealed record MonitoringReadResult(InventorySnapshot Snapshot, IReadOnlyList<MonitoringObservation> Observations);

public sealed class MonitoringReader(MonitoringRoots roots)
{
    public MonitoringReadResult Read(MonitoredLocation location, CancellationToken cancellationToken)
    {
        var path = roots.Resolve(location.RootKey, location.RelativePath);
        var snapshot = new InventorySnapshot
        {
            Id = Guid.NewGuid(), LocationId = location.Id, StartedAtUtc = DateTimeOffset.UtcNow,
            Completion = InventoryCompletion.Complete
        };
        var observations = new List<MonitoringObservation>();
        var pending = new Stack<string>();
        pending.Push(path);
        while (pending.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativeDirectory = Path.GetRelativePath(path, directory);
            try
            {
                using var lease = MonitoringDirectoryLease.Open(directory);
                var includedCount = 0;
                foreach (var item in new DirectoryInfo(directory).EnumerateFileSystemInfos("*", new EnumerationOptions
                {
                    RecurseSubdirectories = false, IgnoreInaccessible = false, AttributesToSkip = 0,
                    ReturnSpecialDirectories = false
                }))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (MonitoringRoots.IsExcluded(item.Name)) continue;
                    includedCount++;
                    var relative = Path.GetRelativePath(path, item.FullName);
                    if ((item.Attributes & (FileAttributes.ReparsePoint | FileAttributes.System)) != 0)
                    {
                        snapshot.Completion = InventoryCompletion.Partial;
                        AddObservation("ENTRY_SKIPPED", relative, "Item de sistema ou link ignorado; inventario parcial.");
                        continue;
                    }
                    var isDirectory = (item.Attributes & FileAttributes.Directory) != 0;
                    var length = isDirectory ? 0 : ((FileInfo)item).Length;
                    snapshot.Entries.Add(new InventoryEntry
                    {
                        RelativePath = relative, IsDirectory = isDirectory, LengthBytes = length,
                        LastWriteTimeUtc = new DateTimeOffset(item.LastWriteTimeUtc), Attributes = (int)item.Attributes
                    });
                    if (isDirectory)
                    {
                        snapshot.DirectoryCount++;
                        pending.Push(item.FullName);
                    }
                    else
                    {
                        snapshot.FileCount++;
                        snapshot.TotalBytes = checked(snapshot.TotalBytes + length);
                        if (length == 0) AddObservation("ZERO_BYTES", relative, "Arquivo com tamanho zero.");
                    }
                    if (item.FullName.Length >= 260) AddObservation("LONG_PATH", relative, $"Caminho com {item.FullName.Length} caracteres.");
                }
                if (includedCount == 0) AddObservation("EMPTY_DIRECTORY", relativeDirectory == "." ? "" : relativeDirectory, "Pasta sem itens no escopo.");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                snapshot.Completion = directory == path ? InventoryCompletion.Unavailable : InventoryCompletion.Partial;
                AddObservation("READ_FAILED", relativeDirectory == "." ? "" : relativeDirectory, exception.Message);
            }
        }
        snapshot.FinishedAtUtc = DateTimeOffset.UtcNow;
        return new MonitoringReadResult(snapshot, observations);

        void AddObservation(string code, string relative, string detail) => observations.Add(new MonitoringObservation
        {
            LocationId = location.Id, ObservedAtUtc = DateTimeOffset.UtcNow, Source = ObservationSource.Monitor,
            Code = code, RelativePath = relative, Detail = detail.Length > 2000 ? detail[..2000] : detail
        });
    }

    public static void EnsureNoRedirectedAncestors(string path)
    {
        for (var current = new DirectoryInfo(path); current is not null; current = current.Parent)
        {
            current.Refresh();
            if (!current.Exists) throw new DirectoryNotFoundException("Diretorio indisponivel: " + current.FullName);
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Links e juncoes nao sao permitidos: " + current.FullName);
        }
    }
}
