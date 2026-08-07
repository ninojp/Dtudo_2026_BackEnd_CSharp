namespace ApiFileStorage.Services;

public sealed record ImportStorageFileCommand(
    string ObjectId,
    string FileName,
    string ContentType,
    long DeclaredLength,
    Stream Content,
    string IdempotencyKey);

public sealed record ImportStorageFileResult(
    string ObjectId,
    string Sha256,
    long Length,
    DateTimeOffset PromotedAtUtc,
    bool Replayed);

public sealed record DeleteStorageFileCommand(
    string ObjectId,
    string IdempotencyKey);

public sealed record DeleteStorageFileRequest(string ObjectId);

public sealed record DeleteStorageFileResult(
    string ObjectId,
    string Sha256,
    DateTimeOffset PurgeAtUtc,
    bool Replayed);

public sealed record ReconcileStorageResult(
    int CompletedImports,
    int CompletedDeletes,
    int AwaitingScanner,
    int AwaitingPromotion,
    int RejectedOperations,
    int PurgedTrashItems);

public interface IFileStorageLifecycleService
{
    Task<ImportStorageFileResult> ImportAsync(
        ImportStorageFileCommand command,
        CancellationToken cancellationToken = default);

    Task<DeleteStorageFileResult> DeleteAsync(
        DeleteStorageFileCommand command,
        CancellationToken cancellationToken = default);

    Task<ReconcileStorageResult> ReconcileAsync(
        CancellationToken cancellationToken = default);
}

public enum FileScanVerdict
{
    Clean,
    ThreatDetected,
    Unavailable
}

public sealed record FileScanResult(FileScanVerdict Verdict);

public interface IFileScanner
{
    Task<FileScanResult> ScanAsync(string quarantinedFilePath, CancellationToken cancellationToken = default);
}

public interface IStorageSpaceChecker
{
    void EnsureAvailable(string rootPath, long requiredBytes);
}

public sealed class FileStorageValidationException : InvalidOperationException
{
    public FileStorageValidationException()
        : base("A importacao nao atende a politica de arquivos permitidos.")
    {
    }
}

public sealed class FileStorageConflictException : InvalidOperationException
{
    public FileStorageConflictException()
        : base("A operacao de armazenamento conflita com um objeto ou chave existente.")
    {
    }
}

public sealed class FileStorageInsufficientSpaceException : IOException
{
    public FileStorageInsufficientSpaceException()
        : base("Nao ha espaco disponivel para concluir a operacao de armazenamento.")
    {
    }
}

public sealed class FileStorageScannerUnavailableException : IOException
{
    public FileStorageScannerUnavailableException()
        : base("O scanner obrigatorio nao esta disponivel.")
    {
    }
}

public sealed class FileStorageThreatDetectedException : InvalidOperationException
{
    public FileStorageThreatDetectedException()
        : base("O arquivo foi recusado pelo scanner de seguranca.")
    {
    }
}

public sealed class FileStorageIntegrityException : IOException
{
    public FileStorageIntegrityException()
        : base("O estado persistido do armazenamento nao pode ser reconciliado com seguranca.")
    {
    }
}
