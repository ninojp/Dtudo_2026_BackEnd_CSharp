using ApiFileStorage.Configuration;
using ApiFileStorage.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApiFileStorage.Tests;

public sealed class FileStorageScannerTests : IDisposable
{
    private readonly string _temporaryDirectory;

    public FileStorageScannerTests()
    {
        _temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "DtudoFileStorageScannerTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
    }

    [Fact]
    public async Task AmsiScanOfHarmlessFileReturnsCleanWithoutNativeMacroEntryPoint()
    {
        var filePath = Path.Combine(_temporaryDirectory, "harmless.txt");
        await File.WriteAllTextAsync(filePath, "Dtudo ApiFileStorage scanner validation.");
        var options = Options.Create(new FileStorageOptions
        {
            Scanner = new FileStorageScannerOptions
            {
                RequireDefender = false,
                RequireAmsi = true
            },
            Limits = new FileStorageLimitsOptions
            {
                ScannerTimeoutSeconds = 5
            }
        });
        var scanner = new CompositeFileScanner(
            options,
            NullLogger<CompositeFileScanner>.Instance);

        var result = await scanner.ScanAsync(filePath);

        Assert.Equal(FileScanVerdict.Clean, result.Verdict);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
