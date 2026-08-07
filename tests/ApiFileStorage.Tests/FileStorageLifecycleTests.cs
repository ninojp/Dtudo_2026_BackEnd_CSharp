using System.Security.Cryptography;
using System.Text.Json;
using ApiFileStorage.Configuration;
using ApiFileStorage.Services;
using Microsoft.Extensions.Options;

namespace ApiFileStorage.Tests;

public sealed class FileStorageLifecycleTests : IDisposable
{
    private readonly string _temporaryDirectory;
    private readonly StorageRootCatalog _rootCatalog;
    private readonly SecureStoragePathResolver _pathResolver;
    private readonly FakeScanner _scanner = new();
    private readonly FakeSpaceChecker _spaceChecker = new();
    private readonly FileStorageLifecycleService _service;

    public FileStorageLifecycleTests()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "DtudoFileStorageLifecycleTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
        var options = new FileStorageOptions
        {
            Roots =
            [
                new AllowedStorageRootOptions
                {
                    Id = "media",
                    Path = _temporaryDirectory
                }
            ],
            Limits = new FileStorageLimitsOptions
            {
                MaxFileSizeBytes = 1024,
                MaxFileNameLength = 255,
                MinimumFreeSpaceBytes = 0,
                MaxIdempotencyKeyLength = 128,
                ScannerTimeoutSeconds = 5
            },
            AllowedFileTypes =
            [
                new AllowedStorageFileTypeOptions
                {
                    Extension = ".png",
                    MimeType = "image/png",
                    MagicBytesHex = "89504E470D0A1A0A"
                }
            ]
        };
        _rootCatalog = new StorageRootCatalog(Options.Create(options));
        _pathResolver = new SecureStoragePathResolver(_rootCatalog);
        _service = new FileStorageLifecycleService(
            _pathResolver,
            _rootCatalog,
            Options.Create(options),
            _scanner,
            _spaceChecker);
    }

    [Fact]
    public async Task MagicMismatch_IsRejectedBeforePromotion()
    {
        var objectId = StorageObjectId.Create("media", "imports/fake.png");

        await Assert.ThrowsAsync<FileStorageValidationException>(() => ImportAsync(
            objectId,
            "fake.png",
            [0x25, 0x50, 0x44, 0x46],
            "fake-magic"));

        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "imports", "fake.png")));
        Assert.Equal(0, _scanner.Calls);
    }

    [Fact]
    public async Task ScannerUnavailable_FailsClosed_AndRetryCanReconcileQuarantine()
    {
        var objectId = StorageObjectId.Create("media", "imports/scanner.png");
        _scanner.Verdict = FileScanVerdict.Unavailable;

        await Assert.ThrowsAsync<FileStorageScannerUnavailableException>(() => ImportAsync(
            objectId,
            "scanner.png",
            ValidPng(),
            "scanner-unavailable"));

        var destinationPath = Path.Combine(_temporaryDirectory, "imports", "scanner.png");
        Assert.False(File.Exists(destinationPath));
        Assert.True(_scanner.LastScannedPath?.Contains(".dtudo-quarantine", StringComparison.OrdinalIgnoreCase));

        _scanner.Verdict = FileScanVerdict.Clean;
        var result = await ImportAsync(objectId, "scanner.png", ValidPng(), "scanner-unavailable");

        Assert.False(result.Replayed);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(ValidPng())), result.Sha256);
        Assert.True(File.Exists(destinationPath));
        Assert.Equal(2, _scanner.Calls);
    }

    [Fact]
    public async Task UnknownScannerVerdict_FailsClosed()
    {
        var objectId = StorageObjectId.Create("media", "imports/unknown-verdict.png");
        _scanner.Verdict = (FileScanVerdict)999;

        await Assert.ThrowsAsync<FileStorageScannerUnavailableException>(() => ImportAsync(
            objectId,
            "unknown-verdict.png",
            ValidPng(),
            "unknown-verdict"));

        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "imports", "unknown-verdict.png")));
    }

    [Fact]
    public async Task SyntheticThreat_IsNeverPromoted()
    {
        var objectId = StorageObjectId.Create("media", "imports/synthetic.png");
        _scanner.Verdict = FileScanVerdict.ThreatDetected;

        await Assert.ThrowsAsync<FileStorageThreatDetectedException>(() => ImportAsync(
            objectId,
            "synthetic.png",
            ValidPngWithSyntheticMarker(),
            "synthetic-threat"));

        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "imports", "synthetic.png")));
        Assert.True(_scanner.LastScannedPath?.Contains(".dtudo-quarantine", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LargeFileAndInsufficientSpace_AreRejected()
    {
        var largeObjectId = StorageObjectId.Create("media", "imports/large.png");
        var largeContent = new byte[1025];
        ValidPng().CopyTo(largeContent, 0);

        await Assert.ThrowsAsync<FileStorageValidationException>(() => ImportAsync(
            largeObjectId,
            "large.png",
            largeContent,
            "large-file"));

        _spaceChecker.Available = false;
        var spaceObjectId = StorageObjectId.Create("media", "imports/space.png");
        await Assert.ThrowsAsync<FileStorageInsufficientSpaceException>(() => ImportAsync(
            spaceObjectId,
            "space.png",
            ValidPng(),
            "no-space"));

        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "imports", "large.png")));
        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "imports", "space.png")));
    }

    [Fact]
    public async Task SameIdempotencyKey_IsPromotedOnceUnderConcurrency()
    {
        var objectId = StorageObjectId.Create("media", "imports/concurrent.png");
        var first = ImportAsync(objectId, "concurrent.png", ValidPng(), "same-key");
        var second = ImportAsync(objectId, "concurrent.png", ValidPng(), "same-key");

        var results = await Task.WhenAll(first, second);

        Assert.Single(results, result => !result.Replayed);
        Assert.Single(results, result => result.Replayed);
        Assert.Equal(1, _scanner.Calls);
        Assert.True(File.Exists(Path.Combine(_temporaryDirectory, "imports", "concurrent.png")));
    }

    [Fact]
    public async Task IdempotencyKey_CannotBeReusedForAnotherObject()
    {
        var firstObjectId = StorageObjectId.Create("media", "imports/first.png");
        var secondObjectId = StorageObjectId.Create("media", "imports/second.png");

        await ImportAsync(firstObjectId, "first.png", ValidPng(), "single-operation-key");

        await Assert.ThrowsAsync<FileStorageConflictException>(() => ImportAsync(
            secondObjectId,
            "second.png",
            ValidPng(),
            "single-operation-key"));

        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "imports", "second.png")));
    }

    [Fact]
    public async Task PartialPromotionFailure_IsReconciledAfterConflictIsRemoved()
    {
        var objectId = StorageObjectId.Create("media", "imports/partial.png");
        var destinationPath = Path.Combine(_temporaryDirectory, "imports", "partial.png");
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.WriteAllBytes(destinationPath, [1, 2, 3]);

        await Assert.ThrowsAsync<FileStorageConflictException>(() => ImportAsync(
            objectId,
            "partial.png",
            ValidPng(),
            "partial-promotion"));

        File.Delete(destinationPath);
        var reconciliation = await _service.ReconcileAsync();

        Assert.Equal(1, reconciliation.CompletedImports);
        Assert.True(File.Exists(destinationPath));
        Assert.Equal(ValidPng(), await File.ReadAllBytesAsync(destinationPath));
    }

    [Fact]
    public async Task DeleteMovesToSevenDayTrash_AndReconciliationPurgesExpiredPayload()
    {
        var objectId = StorageObjectId.Create("media", "imports/trash.png");
        await ImportAsync(objectId, "trash.png", ValidPng(), "import-trash");

        var deleteResult = await _service.DeleteAsync(new DeleteStorageFileCommand(objectId, "delete-trash"));
        Assert.InRange(deleteResult.PurgeAtUtc - DateTimeOffset.UtcNow, TimeSpan.FromDays(6.99), TimeSpan.FromDays(7.01));
        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, "imports", "trash.png")));

        var keyHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("delete-trash")));
        var journalPath = Path.Combine(_temporaryDirectory, ".dtudo-trash", "operations", keyHash, "operation.json");
        var journal = JsonSerializer.Deserialize<StorageOperationJournal>(
            await File.ReadAllTextAsync(journalPath),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        journal.PurgeAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await File.WriteAllTextAsync(journalPath, JsonSerializer.Serialize(journal));

        var reconciliation = await _service.ReconcileAsync();

        Assert.Equal(1, reconciliation.PurgedTrashItems);
        Assert.False(File.Exists(Path.Combine(_temporaryDirectory, ".dtudo-trash", "operations", keyHash, "payload.bin")));
    }

    private async Task<ImportStorageFileResult> ImportAsync(
        string objectId,
        string fileName,
        byte[] content,
        string idempotencyKey)
        => await _service.ImportAsync(new ImportStorageFileCommand(
            objectId,
            fileName,
            "image/png",
            content.Length,
            new MemoryStream(content, writable: false),
            idempotencyKey));

    private static byte[] ValidPng()
        => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

    private static byte[] ValidPngWithSyntheticMarker()
        => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x44, 0x54, 0x55, 0x44, 0x4F, 0x2D, 0x53, 0x59, 0x4E, 0x54, 0x48, 0x45, 0x54, 0x49, 0x43, 0x41, 0x4C, 0x2D, 0x4D, 0x41, 0x4C, 0x57, 0x41, 0x52, 0x45];

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    private sealed class FakeScanner : IFileScanner
    {
        public FileScanVerdict Verdict { get; set; } = FileScanVerdict.Clean;

        public int Calls { get; private set; }

        public string? LastScannedPath { get; private set; }

        public Task<FileScanResult> ScanAsync(string quarantinedFilePath, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastScannedPath = quarantinedFilePath;
            return Task.FromResult(new FileScanResult(Verdict));
        }
    }

    private sealed class FakeSpaceChecker : IStorageSpaceChecker
    {
        public bool Available { get; set; } = true;

        public void EnsureAvailable(string rootPath, long requiredBytes)
        {
            if (!Available)
            {
                throw new FileStorageInsufficientSpaceException();
            }
        }
    }
}
