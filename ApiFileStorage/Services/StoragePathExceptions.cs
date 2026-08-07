namespace ApiFileStorage.Services;

public enum StoragePathRejectionReason
{
    InvalidRootId,
    UnknownRoot,
    EmptyPath,
    AbsolutePath,
    UncPath,
    DevicePath,
    EncodedPath,
    Traversal,
    InvalidSyntax,
    RootBoundary,
    ReparsePoint,
    HardLink,
    CanonicalizationFailure,
    AccessDenied,
    ReservedPath
}

public sealed class StoragePathRejectedException : InvalidOperationException
{
    public StoragePathRejectedException(StoragePathRejectionReason reason)
        : base("A operacao de armazenamento foi recusada por uma regra de seguranca.")
    {
        Reason = reason;
    }

    public StoragePathRejectionReason Reason { get; }
}

public sealed class StorageObjectNotFoundException : FileNotFoundException
{
    public StorageObjectNotFoundException()
        : base("O objeto de armazenamento nao foi encontrado.")
    {
    }
}

public sealed class StorageAccessDeniedException : UnauthorizedAccessException
{
    public StorageAccessDeniedException()
        : base("O acesso ao objeto de armazenamento foi negado.")
    {
    }
}

public sealed class StorageRootConfigurationException : InvalidOperationException
{
    public StorageRootConfigurationException(string message)
        : base(message)
    {
    }
}
