using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiFileStorage.Configuration;
using Microsoft.Extensions.Options;

namespace ApiFileStorage.Services;

public sealed class StorageOperationJournal
{
    public string Version { get; set; } = "v1";

    public string OperationType { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string KeyHash { get; set; } = string.Empty;

    public string RootId { get; set; } = string.Empty;

    public string ObjectId { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long DeclaredLength { get; set; }

    public long Length { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public DateTimeOffset? PromotedAtUtc { get; set; }

    public DateTimeOffset? PurgeAtUtc { get; set; }
}

public sealed class FileStorageLifecycleService(
    IStoragePathResolver pathResolver,
    StorageRootCatalog rootCatalog,
    IOptions<FileStorageOptions> options,
    IFileScanner scanner,
    IStorageSpaceChecker spaceChecker) : IFileStorageLifecycleService
{
    private const string ImportOperation = "import";
    private const string DeleteOperation = "delete";
    private const string StateStaging = "staging";
    private const string StateAwaitingScan = "awaiting-scan";
    private const string StateScanning = "scanning";
    private const string StateReadyToPromote = "ready-to-promote";
    private const string StatePromoting = "promoting";
    private const string StateAwaitingPromotion = "awaiting-promotion";
    private const string StateCompleted = "completed";
    private const string StateRejected = "rejected";
    private const string StateMovingToTrash = "moving-to-trash";
    private const string StateTrashed = "trashed";
    private const string StatePurged = "purged";

    private static readonly JsonSerializerOptions JournalJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly FileStorageOptions _storageOptions = options.Value;
    private readonly IReadOnlyList<AllowedFileType> _allowedFileTypes = CreateAllowedFileTypes(options.Value);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _operationLocks = new(StringComparer.Ordinal);

    public async Task<ImportStorageFileResult> ImportAsync(
        ImportStorageFileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var target = ValidateImportCommand(command);
        var keyHash = HashIdempotencyKey(command.IdempotencyKey);
        var root = rootCatalog.Get(target.RootId);
        EnsureLifecycleDirectories(root);

        using var operationLock = await AcquireOperationLockAsync(root, keyHash, cancellationToken);
        var journalPath = GetJournalPath(root, keyHash, quarantine: true);
        var trashJournalPath = GetJournalPath(root, keyHash, quarantine: false);
        EnsureSecureDirectory(root, Path.GetDirectoryName(journalPath)!);
        var journal = await ReadJournalIfExistsAsync(journalPath, cancellationToken);
        if (journal is null)
        {
            if (File.Exists(trashJournalPath))
            {
                throw new FileStorageConflictException();
            }

            journal = CreateImportJournal(command, target, keyHash);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
        }
        else
        {
            ValidateJournalIdentity(journal, ImportOperation, command.IdempotencyKey, keyHash, target, command.FileName, command.ContentType, command.DeclaredLength);
            if (journal.State == StateCompleted)
            {
                return ToImportResult(journal, replayed: true);
            }

            if (journal.State == StateRejected || journal.State == StatePurged)
            {
                throw new FileStorageThreatDetectedException();
            }
        }

        var stagePath = GetPayloadPath(root, keyHash, quarantine: true);
        if (journal.State == StateStaging || !File.Exists(stagePath))
        {
            TryDelete(stagePath);
            try
            {
                var staged = await StageContentAsync(command, target, stagePath, cancellationToken);
                if (!string.IsNullOrEmpty(journal.Sha256)
                    && (!string.Equals(journal.Sha256, staged.Sha256, StringComparison.Ordinal)
                        || journal.Length != staged.Length))
                {
                    throw new FileStorageConflictException();
                }

                journal.Length = staged.Length;
                journal.Sha256 = staged.Sha256;
                journal.State = StateAwaitingScan;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
            }
            catch (FileStorageConflictException)
            {
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                throw;
            }
            catch
            {
                TryDelete(stagePath);
                throw;
            }
        }

        return await ContinueImportAsync(journal, journalPath, stagePath, target, cancellationToken);
    }

    public async Task<DeleteStorageFileResult> DeleteAsync(
        DeleteStorageFileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateIdempotencyKey(command.IdempotencyKey);
        var target = pathResolver.ResolveWriteTarget(command.ObjectId);
        var root = rootCatalog.Get(target.RootId);
        EnsureLifecycleDirectories(root);
        var keyHash = HashIdempotencyKey(command.IdempotencyKey);

        using var operationLock = await AcquireOperationLockAsync(root, keyHash, cancellationToken);
        var journalPath = GetJournalPath(root, keyHash, quarantine: false);
        var importJournalPath = GetJournalPath(root, keyHash, quarantine: true);
        EnsureSecureDirectory(root, Path.GetDirectoryName(journalPath)!);
        if (File.Exists(importJournalPath))
        {
            throw new FileStorageConflictException();
        }

        var journal = await ReadJournalIfExistsAsync(journalPath, cancellationToken);
        if (journal is not null)
        {
            ValidateJournalIdentity(journal, DeleteOperation, command.IdempotencyKey, keyHash, target, string.Empty, string.Empty, 0);
            if (journal.State is StateTrashed or StatePurged)
            {
                return ToDeleteResult(journal, replayed: true);
            }
        }
        else
        {
            journal = new StorageOperationJournal
            {
                OperationType = DeleteOperation,
                State = StateMovingToTrash,
                KeyHash = keyHash,
                RootId = target.RootId,
                ObjectId = command.ObjectId,
                RelativePath = target.RelativePath,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            await WriteJournalAsync(journalPath, journal, cancellationToken);
        }

        var metadata = pathResolver.ResolveExisting(command.ObjectId);
        if (metadata.Kind != StorageObjectKind.File)
        {
            throw new FileStorageValidationException();
        }

        journal.Length = metadata.Length;
        journal.Sha256 = await ComputeSha256Async(target.FullPath, cancellationToken);
        journal.State = StateMovingToTrash;
        Touch(journal);
        await WriteJournalAsync(journalPath, journal, cancellationToken);

        var trashPath = GetPayloadPath(root, keyHash, quarantine: false);
        try
        {
            EnsureSecureDirectory(root, Path.GetDirectoryName(trashPath)!);
            File.Move(target.FullPath, trashPath, overwrite: false);
        }
        catch (IOException)
        {
            throw new FileStorageConflictException();
        }

        journal.State = StateTrashed;
        journal.PurgeAtUtc = DateTimeOffset.UtcNow.AddDays(7);
        Touch(journal);
        await WriteJournalAsync(journalPath, journal, cancellationToken);

        return ToDeleteResult(journal, replayed: false);
    }

    public async Task<ReconcileStorageResult> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        var completedImports = 0;
        var completedDeletes = 0;
        var awaitingScanner = 0;
        var awaitingPromotion = 0;
        var rejectedOperations = 0;
        var purgedTrashItems = 0;

        foreach (var root in rootCatalog.Roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureLifecycleDirectories(root);
            var quarantineOperationsPath = GetOperationsPath(root, quarantine: true);
            foreach (var journalPath in Directory.EnumerateFiles(quarantineOperationsPath, "operation.json", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var journal = await ReadJournalIfExistsAsync(journalPath, cancellationToken);
                if (journal is null || journal.OperationType != ImportOperation)
                {
                    continue;
                }

                using var operationLock = await AcquireOperationLockAsync(root, journal.KeyHash, cancellationToken);
                var outcome = await ReconcileImportAsync(root, journalPath, journal, cancellationToken);
                completedImports += outcome.Completed ? 1 : 0;
                awaitingScanner += outcome.AwaitingScanner ? 1 : 0;
                awaitingPromotion += outcome.AwaitingPromotion ? 1 : 0;
                rejectedOperations += outcome.Rejected ? 1 : 0;
            }

            var trashOperationsPath = GetOperationsPath(root, quarantine: false);
            foreach (var journalPath in Directory.EnumerateFiles(trashOperationsPath, "operation.json", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var journal = await ReadJournalIfExistsAsync(journalPath, cancellationToken);
                if (journal is null || journal.OperationType != DeleteOperation)
                {
                    continue;
                }

                using var operationLock = await AcquireOperationLockAsync(root, journal.KeyHash, cancellationToken);
                var deleteOutcome = await ReconcileDeleteAsync(root, journalPath, journal, cancellationToken);
                completedDeletes += deleteOutcome.Completed ? 1 : 0;
                purgedTrashItems += deleteOutcome.Purged ? 1 : 0;
            }
        }

        return new ReconcileStorageResult(
            completedImports,
            completedDeletes,
            awaitingScanner,
            awaitingPromotion,
            rejectedOperations,
            purgedTrashItems);
    }

    private async Task<ImportStorageFileResult> ContinueImportAsync(
        StorageOperationJournal journal,
        string journalPath,
        string stagePath,
        StorageWriteTarget target,
        CancellationToken cancellationToken)
    {
        if (journal.State is StateAwaitingScan or StateScanning)
        {
            journal.State = StateScanning;
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);

            FileScanResult scanResult;
            try
            {
                scanResult = await scanner.ScanAsync(stagePath, cancellationToken);
            }
            catch (FileStorageScannerUnavailableException)
            {
                journal.State = StateAwaitingScan;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                throw;
            }

            if (scanResult.Verdict == FileScanVerdict.Unavailable)
            {
                journal.State = StateAwaitingScan;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                throw new FileStorageScannerUnavailableException();
            }

            if (scanResult.Verdict == FileScanVerdict.ThreatDetected)
            {
                journal.State = StateRejected;
                journal.PurgeAtUtc = DateTimeOffset.UtcNow.AddDays(7);
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                throw new FileStorageThreatDetectedException();
            }

            if (scanResult.Verdict != FileScanVerdict.Clean)
            {
                journal.State = StateAwaitingScan;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                throw new FileStorageScannerUnavailableException();
            }

            journal.State = StateReadyToPromote;
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
        }

        return await PromoteAsync(journal, journalPath, stagePath, target, cancellationToken);
    }

    private async Task<ImportStorageFileResult> PromoteAsync(
        StorageOperationJournal journal,
        string journalPath,
        string stagePath,
        StorageWriteTarget target,
        CancellationToken cancellationToken)
    {
        var root = rootCatalog.Get(target.RootId);
        EnsureSecureDirectory(root, target.ParentPath);

        var stagedFileInfo = new FileInfo(stagePath);
        if (!stagedFileInfo.Exists
            || stagedFileInfo.Length != journal.Length
            || !string.Equals(await ComputeSha256Async(stagePath, cancellationToken), journal.Sha256, StringComparison.Ordinal))
        {
            journal.State = StateRejected;
            journal.PurgeAtUtc = DateTimeOffset.UtcNow.AddDays(7);
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
            throw new FileStorageIntegrityException();
        }

        if (TryResolveExistingFile(target, out var existingMetadata))
        {
            if (journal.State == StatePromoting
                && string.Equals(await ComputeSha256Async(target.FullPath, cancellationToken), journal.Sha256, StringComparison.Ordinal))
            {
                TryDelete(stagePath);
                journal.State = StateCompleted;
                journal.PromotedAtUtc ??= DateTimeOffset.UtcNow;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                return ToImportResult(journal, replayed: false);
            }

            _ = existingMetadata;
            journal.State = StateAwaitingPromotion;
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
            throw new FileStorageConflictException();
        }

        journal.State = StatePromoting;
        Touch(journal);
        await WriteJournalAsync(journalPath, journal, cancellationToken);
        try
        {
            File.Move(stagePath, target.FullPath, overwrite: false);
        }
        catch (IOException)
        {
            journal.State = StateAwaitingPromotion;
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
            throw new FileStorageConflictException();
        }

        journal.State = StateCompleted;
        journal.PromotedAtUtc = DateTimeOffset.UtcNow;
        Touch(journal);
        await WriteJournalAsync(journalPath, journal, cancellationToken);
        return ToImportResult(journal, replayed: false);
    }

    private async Task<ReconcileImportOutcome> ReconcileImportAsync(
        AllowedStorageRoot root,
        string journalPath,
        StorageOperationJournal journal,
        CancellationToken cancellationToken)
    {
        if (journal.State == StateCompleted)
        {
            return ReconcileImportOutcome.None;
        }

        var stagePath = GetPayloadPath(root, journal.KeyHash, quarantine: true);
        if (journal.State == StateRejected || journal.State == StatePurged)
        {
            if (journal.PurgeAtUtc <= DateTimeOffset.UtcNow)
            {
                TryDelete(stagePath);
                journal.State = StatePurged;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
            }

            return new ReconcileImportOutcome(false, false, false, true);
        }

        StorageWriteTarget target;
        try
        {
            target = pathResolver.ResolveWriteTarget(journal.ObjectId);
        }
        catch (StoragePathRejectedException)
        {
            journal.State = StateRejected;
            journal.PurgeAtUtc = DateTimeOffset.UtcNow.AddDays(7);
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
            return new ReconcileImportOutcome(false, false, false, true);
        }

        if (!File.Exists(stagePath))
        {
            if (journal.State == StatePromoting
                && TryResolveExistingFile(target, out _)
                && string.Equals(await ComputeSha256Async(target.FullPath, cancellationToken), journal.Sha256, StringComparison.Ordinal))
            {
                journal.State = StateCompleted;
                journal.PromotedAtUtc ??= DateTimeOffset.UtcNow;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                return new ReconcileImportOutcome(true, false, false, false);
            }

            journal.State = StateRejected;
            journal.PurgeAtUtc = DateTimeOffset.UtcNow.AddDays(7);
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
            return new ReconcileImportOutcome(false, false, false, true);
        }

        if (journal.State == StateStaging)
        {
            try
            {
                var fileType = ResolveFileType(journal.FileName, journal.ContentType);
                var staged = await InspectStagedContentAsync(stagePath, fileType, cancellationToken);
                if (staged.Length != journal.DeclaredLength
                    || (!string.IsNullOrEmpty(journal.Sha256) && !string.Equals(staged.Sha256, journal.Sha256, StringComparison.Ordinal)))
                {
                    throw new FileStorageIntegrityException();
                }

                journal.Length = staged.Length;
                journal.Sha256 = staged.Sha256;
                journal.State = StateAwaitingScan;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
            }
            catch (FileStorageIntegrityException)
            {
                journal.State = StateRejected;
                journal.PurgeAtUtc = DateTimeOffset.UtcNow.AddDays(7);
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                return new ReconcileImportOutcome(false, false, false, true);
            }
        }

        if (journal.State is StateAwaitingScan or StateScanning)
        {
            journal.State = StateScanning;
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
            FileScanResult scanResult;
            try
            {
                scanResult = await scanner.ScanAsync(stagePath, cancellationToken);
            }
            catch (FileStorageScannerUnavailableException)
            {
                journal.State = StateAwaitingScan;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                return new ReconcileImportOutcome(false, true, false, false);
            }

            if (scanResult.Verdict == FileScanVerdict.Unavailable)
            {
                journal.State = StateAwaitingScan;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                return new ReconcileImportOutcome(false, true, false, false);
            }

            if (scanResult.Verdict == FileScanVerdict.ThreatDetected)
            {
                journal.State = StateRejected;
                journal.PurgeAtUtc = DateTimeOffset.UtcNow.AddDays(7);
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                return new ReconcileImportOutcome(false, false, false, true);
            }

            if (scanResult.Verdict != FileScanVerdict.Clean)
            {
                journal.State = StateAwaitingScan;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                return new ReconcileImportOutcome(false, true, false, false);
            }

            journal.State = StateReadyToPromote;
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
        }

        try
        {
            await PromoteAsync(journal, journalPath, stagePath, target, cancellationToken);
            return new ReconcileImportOutcome(true, false, false, false);
        }
        catch (FileStorageConflictException)
        {
            return new ReconcileImportOutcome(false, false, true, false);
        }
    }

    private async Task<ReconcileDeleteOutcome> ReconcileDeleteAsync(
        AllowedStorageRoot root,
        string journalPath,
        StorageOperationJournal journal,
        CancellationToken cancellationToken)
    {
        var trashPath = GetPayloadPath(root, journal.KeyHash, quarantine: false);
        var completed = false;
        if (journal.State == StateMovingToTrash && File.Exists(trashPath))
        {
            journal.State = StateTrashed;
            journal.PurgeAtUtc ??= DateTimeOffset.UtcNow.AddDays(7);
            Touch(journal);
            await WriteJournalAsync(journalPath, journal, cancellationToken);
            completed = true;
        }

        if (journal.State == StateTrashed && journal.PurgeAtUtc <= DateTimeOffset.UtcNow)
        {
            TryDelete(trashPath);
            if (!File.Exists(trashPath))
            {
                journal.State = StatePurged;
                Touch(journal);
                await WriteJournalAsync(journalPath, journal, cancellationToken);
                return new ReconcileDeleteOutcome(true, true);
            }
        }

        return new ReconcileDeleteOutcome(completed, false);
    }

    private StorageWriteTarget ValidateImportCommand(ImportStorageFileCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ObjectId)
            || command.Content is null
            || !command.Content.CanRead
            || command.DeclaredLength < 0
            || command.DeclaredLength > _storageOptions.Limits.MaxFileSizeBytes)
        {
            throw new FileStorageValidationException();
        }

        ValidateIdempotencyKey(command.IdempotencyKey);
        var target = pathResolver.ResolveWriteTarget(command.ObjectId);
        var fileName = ValidateFileName(command.FileName);
        var contentType = NormalizeContentType(command.ContentType);
        var fileType = ResolveFileType(fileName, contentType);
        var targetExtension = Path.GetExtension(target.RelativePath);
        if (!string.Equals(targetExtension, fileType.Extension, StringComparison.OrdinalIgnoreCase))
        {
            throw new FileStorageValidationException();
        }

        return target;
    }

    private async Task<StagedContent> StageContentAsync(
        ImportStorageFileCommand command,
        StorageWriteTarget target,
        string stagePath,
        CancellationToken cancellationToken)
    {
        var fileName = ValidateFileName(command.FileName);
        var contentType = NormalizeContentType(command.ContentType);
        var fileType = ResolveFileType(fileName, contentType);
        var root = rootCatalog.Get(target.RootId);
        spaceChecker.EnsureAvailable(root.CanonicalPath, command.DeclaredLength);
        EnsureSecureDirectory(root, Path.GetDirectoryName(stagePath)!);

        var prefixLength = checked(fileType.MagicOffset + fileType.MagicBytes.Length);
        var prefix = new byte[prefixLength];
        var prefixCount = 0;
        var buffer = new byte[64 * 1024];
        long length = 0;
        try
        {
            await using var stage = new FileStream(
                stagePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read,
                buffer.Length,
                FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            while (true)
            {
                var read = await command.Content.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                length = checked(length + read);
                if (length > _storageOptions.Limits.MaxFileSizeBytes)
                {
                    throw new FileStorageValidationException();
                }

                await stage.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                hash.AppendData(buffer, 0, read);
                if (prefixCount < prefix.Length)
                {
                    var copyLength = Math.Min(prefix.Length - prefixCount, read);
                    Buffer.BlockCopy(buffer, 0, prefix, prefixCount, copyLength);
                    prefixCount += copyLength;
                }
            }

            await stage.FlushAsync(cancellationToken);
            stage.Flush(flushToDisk: true);
            if (length != command.DeclaredLength || prefixCount < prefix.Length || !MatchesMagic(prefix, fileType))
            {
                throw new FileStorageValidationException();
            }

            return new StagedContent(length, Convert.ToHexString(hash.GetHashAndReset()));
        }
        catch (IOException)
        {
            TryDelete(stagePath);
            throw new FileStorageInsufficientSpaceException();
        }
        catch
        {
            TryDelete(stagePath);
            throw;
        }
    }

    private async Task<StagedContent> InspectStagedContentAsync(
        string stagePath,
        AllowedFileType fileType,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(stagePath);
        if (!fileInfo.Exists || fileInfo.Length > _storageOptions.Limits.MaxFileSizeBytes)
        {
            throw new FileStorageIntegrityException();
        }

        await using var stream = new FileStream(stagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan | FileOptions.Asynchronous);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var prefix = new byte[fileType.MagicOffset + fileType.MagicBytes.Length];
        var prefixCount = 0;
        var buffer = new byte[64 * 1024];
        long length = 0;
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                break;
            }

            length = checked(length + read);
            hash.AppendData(buffer, 0, read);
            if (prefixCount < prefix.Length)
            {
                var copyLength = Math.Min(prefix.Length - prefixCount, read);
                Buffer.BlockCopy(buffer, 0, prefix, prefixCount, copyLength);
                prefixCount += copyLength;
            }
        }

        if (prefixCount < prefix.Length || !MatchesMagic(prefix, fileType))
        {
            throw new FileStorageIntegrityException();
        }

        return new StagedContent(length, Convert.ToHexString(hash.GetHashAndReset()));
    }

    private StorageOperationJournal CreateImportJournal(
        ImportStorageFileCommand command,
        StorageWriteTarget target,
        string keyHash)
        => new()
        {
            OperationType = ImportOperation,
            State = StateStaging,
            KeyHash = keyHash,
            RootId = target.RootId,
            ObjectId = command.ObjectId,
            RelativePath = target.RelativePath,
            FileName = ValidateFileName(command.FileName),
            ContentType = NormalizeContentType(command.ContentType),
            DeclaredLength = command.DeclaredLength,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

    private string ValidateFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName.Length > _storageOptions.Limits.MaxFileNameLength
            || fileName.Contains('/')
            || fileName.Contains('\\')
            || Path.GetFileName(fileName) != fileName
            || fileName.Any(character => character == '\0' || char.IsControl(character)))
        {
            throw new FileStorageValidationException();
        }

        return fileName;
    }

    private void ValidateIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)
            || key.Length > _storageOptions.Limits.MaxIdempotencyKeyLength
            || key.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not '-' and not '~'))
        {
            throw new FileStorageValidationException();
        }
    }

    private static string NormalizeContentType(string? contentType)
    {
        var normalized = contentType?.Split(';', 2)[0].Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('/'))
        {
            throw new FileStorageValidationException();
        }

        return normalized;
    }

    private AllowedFileType ResolveFileType(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        var fileType = _allowedFileTypes.FirstOrDefault(candidate =>
            string.Equals(candidate.Extension, extension, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.MimeType, contentType, StringComparison.OrdinalIgnoreCase));
        return fileType ?? throw new FileStorageValidationException();
    }

    private static bool MatchesMagic(byte[] prefix, AllowedFileType fileType)
    {
        if (prefix.Length < fileType.MagicOffset + fileType.MagicBytes.Length)
        {
            return false;
        }

        return prefix.AsSpan(fileType.MagicOffset, fileType.MagicBytes.Length).SequenceEqual(fileType.MagicBytes);
    }

    private static IReadOnlyList<AllowedFileType> CreateAllowedFileTypes(FileStorageOptions options)
    {
        var result = new List<AllowedFileType>();
        foreach (var configured in options.AllowedFileTypes ?? [])
        {
            if (configured is null
                || string.IsNullOrWhiteSpace(configured.Extension)
                || !configured.Extension.StartsWith(".", StringComparison.Ordinal)
                || configured.Extension.Length > 16
                || configured.Extension.Any(character => character is '/' or '\\' or ':' or '*' or '?')
                || string.IsNullOrWhiteSpace(configured.MimeType)
                || configured.MimeType.Contains(';', StringComparison.Ordinal)
                || configured.MagicOffset < 0)
            {
                throw new InvalidOperationException("A allowlist de tipos de arquivo possui configuracao invalida.");
            }

            byte[] magicBytes;
            try
            {
                magicBytes = Convert.FromHexString(configured.MagicBytesHex.Replace(" ", string.Empty, StringComparison.Ordinal));
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("A allowlist de tipos de arquivo possui magic bytes invalidos.");
            }

            if (magicBytes.Length == 0 || result.Any(existing =>
                    string.Equals(existing.Extension, configured.Extension, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(existing.MimeType, configured.MimeType, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("A allowlist de tipos de arquivo possui entradas duplicadas ou vazias.");
            }

            result.Add(new AllowedFileType(
                configured.Extension.ToLowerInvariant(),
                configured.MimeType.ToLowerInvariant(),
                magicBytes,
                configured.MagicOffset));
        }

        return result;
    }

    private static void ValidateJournalIdentity(
        StorageOperationJournal journal,
        string operationType,
        string idempotencyKey,
        string keyHash,
        StorageWriteTarget target,
        string fileName,
        string contentType,
        long declaredLength)
    {
        if (!string.Equals(journal.Version, "v1", StringComparison.Ordinal)
            || !string.Equals(journal.OperationType, operationType, StringComparison.Ordinal)
            || !string.Equals(journal.KeyHash, keyHash, StringComparison.Ordinal)
            || !string.Equals(journal.RootId, target.RootId, StringComparison.Ordinal)
            || !string.Equals(journal.ObjectId, StorageObjectId.Create(target.RootId, target.RelativePath), StringComparison.Ordinal)
            || !string.Equals(journal.RelativePath, target.RelativePath, StringComparison.Ordinal)
            || (operationType == ImportOperation
                && (!string.Equals(journal.FileName, fileName, StringComparison.Ordinal)
                    || !string.Equals(journal.ContentType, contentType, StringComparison.Ordinal)
                    || journal.DeclaredLength != declaredLength)))
        {
            throw new FileStorageConflictException();
        }

        _ = idempotencyKey;
    }

    private static string HashIdempotencyKey(string key)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    private static void Touch(StorageOperationJournal journal)
        => journal.UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static ImportStorageFileResult ToImportResult(StorageOperationJournal journal, bool replayed)
        => new(journal.ObjectId, journal.Sha256, journal.Length, journal.PromotedAtUtc ?? journal.UpdatedAtUtc, replayed);

    private static DeleteStorageFileResult ToDeleteResult(StorageOperationJournal journal, bool replayed)
        => new(journal.ObjectId, journal.Sha256, journal.PurgeAtUtc ?? journal.UpdatedAtUtc.AddDays(7), replayed);

    private bool TryResolveExistingFile(StorageWriteTarget target, out StorageObjectMetadata? metadata)
    {
        try
        {
            metadata = pathResolver.ResolveExisting(StorageObjectId.Create(target.RootId, target.RelativePath));
            if (metadata.Kind != StorageObjectKind.File)
            {
                throw new FileStorageConflictException();
            }

            return true;
        }
        catch (StorageObjectNotFoundException)
        {
            metadata = null;
            return false;
        }
    }

    private void EnsureLifecycleDirectories(AllowedStorageRoot root)
    {
        var quarantinePath = Path.Combine(root.CanonicalPath, StorageInternalPathPolicy.QuarantineDirectoryName);
        var trashPath = Path.Combine(root.CanonicalPath, StorageInternalPathPolicy.TrashDirectoryName);
        EnsureSecureDirectory(root, quarantinePath);
        EnsureSecureDirectory(root, Path.Combine(quarantinePath, "operations"));
        EnsureSecureDirectory(root, trashPath);
        EnsureSecureDirectory(root, Path.Combine(trashPath, "operations"));
    }

    private static void EnsureSecureDirectory(AllowedStorageRoot root, string directoryPath)
    {
        var fullPath = StorageRootCatalog.NormalizeComparablePath(directoryPath);
        if (!StorageRootCatalog.IsWithinRoot(root.CanonicalPath, fullPath))
        {
            throw new FileStorageIntegrityException();
        }

        try
        {
            Directory.CreateDirectory(fullPath);
            var relativePath = Path.GetRelativePath(root.CanonicalPath, fullPath);
            var segments = relativePath == "."
                ? []
                : relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
            using var opened = segments.Length == 0
                ? WindowsFileSystem.OpenAbsolute(root.CanonicalPath)
                : WindowsFileSystem.OpenRelative(root.CanonicalPath, segments);
            if (!opened.Information.IsDirectory
                || !StorageRootCatalog.IsWithinRoot(root.CanonicalPath, StorageRootCatalog.NormalizeComparablePath(opened.FinalPath)))
            {
                throw new FileStorageIntegrityException();
            }
        }
        catch (StoragePathRejectedException)
        {
            throw new FileStorageIntegrityException();
        }
        catch (StorageAccessDeniedException)
        {
            throw new FileStorageIntegrityException();
        }
        catch (IOException)
        {
            throw new FileStorageIntegrityException();
        }
    }

    private static string GetOperationsPath(AllowedStorageRoot root, bool quarantine)
        => Path.Combine(
            root.CanonicalPath,
            quarantine ? StorageInternalPathPolicy.QuarantineDirectoryName : StorageInternalPathPolicy.TrashDirectoryName,
            "operations");

    private static string GetJournalPath(AllowedStorageRoot root, string keyHash, bool quarantine)
        => Path.Combine(GetOperationsPath(root, quarantine), keyHash, "operation.json");

    private static string GetPayloadPath(AllowedStorageRoot root, string keyHash, bool quarantine)
        => Path.Combine(GetOperationsPath(root, quarantine), keyHash, "payload.bin");

    private static async Task<StorageOperationJournal?> ReadJournalIfExistsAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024, FileOptions.SequentialScan | FileOptions.Asynchronous);
            return await JsonSerializer.DeserializeAsync<StorageOperationJournal>(stream, JournalJsonOptions, cancellationToken)
                ?? throw new FileStorageIntegrityException();
        }
        catch (JsonException)
        {
            throw new FileStorageIntegrityException();
        }
    }

    private static async Task WriteJournalAsync(
        string path,
        StorageOperationJournal journal,
        CancellationToken cancellationToken)
    {
        var temporaryPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 16 * 1024, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, journal, JournalJsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private async Task<OperationLock> AcquireOperationLockAsync(
        AllowedStorageRoot root,
        string keyHash,
        CancellationToken cancellationToken)
    {
        var semaphore = _operationLocks.GetOrAdd(keyHash, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var lockPath = Path.Combine(GetOperationsPath(root, quarantine: true), keyHash + ".lock");
            for (var attempt = 0; attempt < 1200; attempt++)
            {
                try
                {
                    var lockStream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    return new OperationLock(semaphore, lockStream);
                }
                catch (IOException) when (attempt < 1199)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);
                }
            }

            throw new FileStorageConflictException();
        }
        catch
        {
            semaphore.Release();
            throw;
        }
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan | FileOptions.Asynchronous);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record AllowedFileType(
        string Extension,
        string MimeType,
        byte[] MagicBytes,
        int MagicOffset);

    private sealed record StagedContent(long Length, string Sha256);

    private sealed record ReconcileImportOutcome(
        bool Completed,
        bool AwaitingScanner,
        bool AwaitingPromotion,
        bool Rejected)
    {
        public static ReconcileImportOutcome None { get; } = new(false, false, false, false);
    }

    private sealed record ReconcileDeleteOutcome(bool Completed, bool Purged);

    private sealed class OperationLock(SemaphoreSlim semaphore, FileStream lockStream) : IDisposable
    {
        public void Dispose()
        {
            lockStream.Dispose();
            semaphore.Release();
        }
    }
}
