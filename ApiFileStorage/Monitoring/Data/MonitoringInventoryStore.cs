using Microsoft.EntityFrameworkCore;

namespace ApiFileStorage.Monitoring.Data;

public sealed class MonitoringInventoryStore(MonitoringDbContext context)
{
    public async Task AppendSnapshotAsync(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Id == Guid.Empty || snapshot.LocationId == Guid.Empty
            || !Enum.IsDefined(snapshot.Completion) || snapshot.FinishedAtUtc < snapshot.StartedAtUtc)
        {
            throw new ArgumentException("Invalid snapshot identity, completion or time interval.", nameof(snapshot));
        }

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long bytes = 0;
        long files = 0;
        long directories = 0;
        foreach (var entry in snapshot.Entries)
        {
            if (!IsRelativeEntryPath(entry.RelativePath) || !paths.Add(entry.RelativePath)
                || entry.LengthBytes < 0 || (entry.IsDirectory && entry.LengthBytes != 0)
                || entry.Id != 0 || (entry.SnapshotId != Guid.Empty && entry.SnapshotId != snapshot.Id))
            {
                throw new ArgumentException("Invalid or repeated snapshot entry.", nameof(snapshot));
            }

            bytes = checked(bytes + entry.LengthBytes);
            if (entry.IsDirectory)
            {
                directories++;
            }
            else
            {
                files++;
            }
        }

        if (snapshot.TotalBytes != bytes || snapshot.FileCount != files || snapshot.DirectoryCount != directories)
        {
            throw new ArgumentException("Snapshot totals must match its observed entries.", nameof(snapshot));
        }

        context.Snapshots.Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<InventorySnapshot?> GetLatestCompleteSnapshotAsync(Guid locationId, CancellationToken cancellationToken = default) =>
        context.Snapshots.AsNoTracking()
            .Where(snapshot => snapshot.LocationId == locationId && snapshot.Completion == InventoryCompletion.Complete)
            .OrderByDescending(snapshot => snapshot.FinishedAtUtc)
            .ThenByDescending(snapshot => snapshot.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryEntry>> ReadEntriesAsync(
        Guid snapshotId, long afterId = 0, int pageSize = 200, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(afterId);
        if (pageSize is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        return await context.Entries.AsNoTracking()
            .Where(entry => entry.SnapshotId == snapshotId && entry.Id > afterId)
            .OrderBy(entry => entry.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private static bool IsRelativeEntryPath(string path) =>
        !string.IsNullOrWhiteSpace(path)
        && !Path.IsPathRooted(path)
        && !path.Contains(':')
        && !path.Contains('\0')
        && !path.Contains('/')
        && path.Split('\\').All(segment => segment.Length > 0 && segment is not "." and not "..");
}
