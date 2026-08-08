using System.Globalization;
using System.Text.Json;
using ApiFileStorage.Configuration;
using Microsoft.Extensions.Options;

namespace ApiFileStorage.Services;

public sealed record FileStorageHealthStatus(
    string Status,
    IReadOnlyList<FileStorageRootHealthStatus> Roots,
    FileStorageScannerHealthStatus Scanner,
    FileStorageQuarantineHealthStatus Quarantine);

public sealed record FileStorageRootHealthStatus(
    string Id,
    string Status,
    long AvailableBytes,
    long TotalBytes,
    long MinimumFreeSpaceBytes);

public sealed record FileStorageScannerHealthStatus(string Status);

public sealed record FileStorageQuarantineHealthStatus(
    string Status,
    int PendingCount,
    int ThreatCount,
    int TrashCount);

public sealed class FileStorageHealthService(
    StorageRootCatalog rootCatalog,
    IOptions<FileStorageOptions> options)
{
    private readonly FileStorageOptions _options = options.Value;

    public FileStorageHealthStatus GetStatus()
    {
        var roots = rootCatalog.Roots
            .Select(GetRootStatus)
            .ToArray();
        var scanner = GetScannerStatus();
        var quarantine = GetQuarantineStatus(rootCatalog.Roots);
        var status = roots.Any(root => root.Status == "unavailable")
            ? "unavailable"
            : "ok";

        return new FileStorageHealthStatus(status, roots, scanner, quarantine);
    }

    private FileStorageRootHealthStatus GetRootStatus(AllowedStorageRoot root)
    {
        var minimum = Math.Max(0, _options.Limits.MinimumFreeSpaceBytes);
        try
        {
            var driveRoot = Path.GetPathRoot(root.CanonicalPath);
            if (string.IsNullOrWhiteSpace(driveRoot))
            {
                return new FileStorageRootHealthStatus(root.Id, "unavailable", 0, 0, minimum);
            }

            var drive = new DriveInfo(driveRoot);
            if (!drive.IsReady)
            {
                return new FileStorageRootHealthStatus(root.Id, "unavailable", 0, 0, minimum);
            }

            return new FileStorageRootHealthStatus(
                root.Id,
                drive.AvailableFreeSpace < minimum ? "critical" : "ok",
                drive.AvailableFreeSpace,
                drive.TotalSize,
                minimum);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new FileStorageRootHealthStatus(root.Id, "unavailable", 0, 0, minimum);
        }
    }

    private FileStorageScannerHealthStatus GetScannerStatus()
    {
        var scanner = _options.Scanner;
        var defenderReady = !scanner.RequireDefender
            || ResolveDefenderExecutablePath(scanner.DefenderExecutablePath) is not null;
        var amsiReady = !scanner.RequireAmsi || OperatingSystem.IsWindows();
        return new FileStorageScannerHealthStatus(defenderReady && amsiReady ? "ok" : "unavailable");
    }

    private static FileStorageQuarantineHealthStatus GetQuarantineStatus(
        IEnumerable<AllowedStorageRoot> roots)
    {
        var pendingCount = 0;
        var threatCount = 0;
        var trashCount = 0;

        try
        {
            foreach (var root in roots)
            {
                var quarantinePath = Path.Combine(
                    root.CanonicalPath,
                    StorageInternalPathPolicy.QuarantineDirectoryName,
                    "operations");
                var trashPath = Path.Combine(
                    root.CanonicalPath,
                    StorageInternalPathPolicy.TrashDirectoryName,
                    "operations");

                foreach (var operationPath in EnumerateOperationJournals(quarantinePath))
                {
                    var journal = ReadJournal(operationPath);
                    if (journal is null)
                    {
                        return new FileStorageQuarantineHealthStatus("unavailable", 0, 0, 0);
                    }

                    if (string.Equals(journal.State, "rejected", StringComparison.Ordinal))
                    {
                        threatCount++;
                    }
                    else if (!string.Equals(journal.State, "completed", StringComparison.Ordinal)
                        && !string.Equals(journal.State, "purged", StringComparison.Ordinal))
                    {
                        pendingCount++;
                    }
                }

                trashCount += EnumerateOperationJournals(trashPath).Count();
            }

            return new FileStorageQuarantineHealthStatus("ok", pendingCount, threatCount, trashCount);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new FileStorageQuarantineHealthStatus("unavailable", 0, 0, 0);
        }
    }

    private static IEnumerable<string> EnumerateOperationJournals(string operationsPath)
    {
        if (!Directory.Exists(operationsPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(operationsPath, "operation.json", SearchOption.AllDirectories);
    }

    private static StorageOperationJournal? ReadJournal(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<StorageOperationJournal>(stream);
    }

    private static string? ResolveDefenderExecutablePath(string? configuredPath)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            candidates.Add(configuredPath);
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            candidates.Add(Path.Combine(programFiles, "Windows Defender", "MpCmdRun.exe"));
        }

        var programW6432 = Environment.GetEnvironmentVariable("ProgramW6432");
        if (!string.IsNullOrWhiteSpace(programW6432))
        {
            candidates.Add(Path.Combine(programW6432, "Windows Defender", "MpCmdRun.exe"));
        }

        return candidates
            .Where(path => Path.IsPathFullyQualified(path))
            .Select(path => Path.GetFullPath(path))
            .FirstOrDefault(File.Exists);
    }
}
