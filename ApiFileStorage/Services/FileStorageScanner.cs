using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using ApiFileStorage.Configuration;

namespace ApiFileStorage.Services;

public sealed class StorageSpaceChecker(IOptions<FileStorageOptions> options) : IStorageSpaceChecker
{
    private readonly FileStorageLimitsOptions _limits = options.Value.Limits;

    public void EnsureAvailable(string rootPath, long requiredBytes)
    {
        if (requiredBytes < 0)
        {
            throw new FileStorageInsufficientSpaceException();
        }

        long requiredWithReserve;
        try
        {
            requiredWithReserve = checked(requiredBytes + _limits.MinimumFreeSpaceBytes);
        }
        catch (OverflowException)
        {
            throw new FileStorageInsufficientSpaceException();
        }

        string? driveRoot;
        try
        {
            driveRoot = Path.GetPathRoot(rootPath);
            if (string.IsNullOrWhiteSpace(driveRoot))
            {
                throw new FileStorageInsufficientSpaceException();
            }

            var drive = new DriveInfo(driveRoot);
            if (!drive.IsReady || drive.AvailableFreeSpace < requiredWithReserve)
            {
                throw new FileStorageInsufficientSpaceException();
            }
        }
        catch (FileStorageInsufficientSpaceException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            throw new FileStorageInsufficientSpaceException();
        }
    }
}

public sealed class CompositeFileScanner(
    IOptions<FileStorageOptions> options,
    ILogger<CompositeFileScanner> logger) : IFileScanner
{
    private const int AmsiResultDetected = 0x8000;
    private readonly FileStorageScannerOptions _scannerOptions = options.Value.Scanner;
    private readonly FileStorageLimitsOptions _limits = options.Value.Limits;

    public async Task<FileScanResult> ScanAsync(string quarantinedFilePath, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows() || !Path.IsPathFullyQualified(quarantinedFilePath) || !File.Exists(quarantinedFilePath))
        {
            throw new FileStorageScannerUnavailableException();
        }

        if (_scannerOptions.RequireDefender)
        {
            var defenderResult = await ScanWithDefenderAsync(quarantinedFilePath, cancellationToken);
            if (defenderResult.Verdict != FileScanVerdict.Clean)
            {
                return defenderResult;
            }
        }

        if (_scannerOptions.RequireAmsi)
        {
            var amsiResult = await ScanWithAmsiAsync(quarantinedFilePath, cancellationToken);
            if (amsiResult.Verdict != FileScanVerdict.Clean)
            {
                return amsiResult;
            }
        }

        if (!_scannerOptions.RequireDefender && !_scannerOptions.RequireAmsi)
        {
            logger.LogError("Nenhum scanner obrigatorio foi configurado; a importacao permanece bloqueada.");
            throw new FileStorageScannerUnavailableException();
        }

        return new FileScanResult(FileScanVerdict.Clean);
    }

    private async Task<FileScanResult> ScanWithDefenderAsync(string filePath, CancellationToken cancellationToken)
    {
        var executablePath = ResolveDefenderExecutablePath(_scannerOptions.DefenderExecutablePath);
        if (executablePath is null)
        {
            throw new FileStorageScannerUnavailableException();
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            }
        };
        process.StartInfo.ArgumentList.Add("-Scan");
        process.StartInfo.ArgumentList.Add("-ScanType");
        process.StartInfo.ArgumentList.Add("3");
        process.StartInfo.ArgumentList.Add("-File");
        process.StartInfo.ArgumentList.Add(filePath);

        try
        {
            if (!process.Start())
            {
                throw new FileStorageScannerUnavailableException();
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _limits.ScannerTimeoutSeconds)));
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw new FileStorageScannerUnavailableException();
        }
        catch (FileStorageScannerUnavailableException)
        {
            TryKill(process);
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            TryKill(process);
            throw new FileStorageScannerUnavailableException();
        }
        finally
        {
            TryKill(process);
        }

        return process.ExitCode switch
        {
            0 => new FileScanResult(FileScanVerdict.Clean),
            2 => new FileScanResult(FileScanVerdict.ThreatDetected),
            _ => throw new FileStorageScannerUnavailableException()
        };
    }

    private static async Task<FileScanResult> ScanWithAmsiAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new FileStorageScannerUnavailableException();
        }

        byte[] content;
        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                64 * 1024,
                FileOptions.SequentialScan | FileOptions.Asynchronous);
            if (stream.Length > int.MaxValue)
            {
                throw new FileStorageScannerUnavailableException();
            }

            content = new byte[stream.Length];
            var offset = 0;
            while (offset < content.Length)
            {
                var read = await stream.ReadAsync(content.AsMemory(offset), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                offset += read;
            }

            if (offset != content.Length)
            {
                throw new FileStorageScannerUnavailableException();
            }
        }
        catch (FileStorageScannerUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileStorageScannerUnavailableException();
        }

        var initializeResult = AmsiInitialize("Dtudo.ApiFileStorage", out var context);
        if (initializeResult != 0)
        {
            throw new FileStorageScannerUnavailableException();
        }

        try
        {
            var openSessionResult = AmsiOpenSession(context, out var session);
            if (openSessionResult != 0)
            {
                throw new FileStorageScannerUnavailableException();
            }

            try
            {
                var scanResult = AmsiScanBuffer(
                    context,
                    content,
                    checked((uint)content.Length),
                    Path.GetFileName(filePath),
                    session,
                    out var amsiResult);
                if (scanResult != 0)
                {
                    throw new FileStorageScannerUnavailableException();
                }

                return amsiResult >= AmsiResultDetected
                    ? new FileScanResult(FileScanVerdict.ThreatDetected)
                    : new FileScanResult(FileScanVerdict.Clean);
            }
            finally
            {
                AmsiCloseSession(context, session);
            }
        }
        finally
        {
            AmsiUninitialize(context);
        }
    }

    private static string? ResolveDefenderExecutablePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return File.Exists(configuredPath) ? configuredPath : null;
        }

        var platformRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Microsoft",
            "Windows Defender",
            "Platform");
        if (!Directory.Exists(platformRoot))
        {
            return null;
        }

        return Directory.EnumerateDirectories(platformRoot)
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => Path.Combine(path, "MpCmdRun.exe"))
            .FirstOrDefault(File.Exists);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    [DllImport("amsi.dll", CharSet = CharSet.Unicode)]
    private static extern int AmsiInitialize(string appName, out nint context);

    [DllImport("amsi.dll")]
    private static extern int AmsiOpenSession(nint context, out nint session);

    [DllImport("amsi.dll", CharSet = CharSet.Unicode)]
    private static extern int AmsiScanBuffer(
        nint context,
        byte[] buffer,
        uint length,
        string contentName,
        nint session,
        out int result);

    [DllImport("amsi.dll")]
    private static extern void AmsiCloseSession(nint context, nint session);

    [DllImport("amsi.dll")]
    private static extern void AmsiUninitialize(nint context);
}
