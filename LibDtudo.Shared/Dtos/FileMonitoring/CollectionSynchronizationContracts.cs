namespace LibDtudo.Shared.Dtos.FileMonitoring;

/// <summary>Estado de uma raiz durante uma descoberta de sincronizacao.</summary>
public sealed record CollectionSyncRootDto(string Key, string Path, bool Available, string? Error);

/// <summary>Pasta candidata encontrada diretamente abaixo de uma raiz autorizada.</summary>
public sealed record CollectionSyncFolderDto(string RootKey, string RelativePath, IReadOnlyList<int> CoverIds);

/// <summary>Problema encontrado durante a descoberta, antes da aplicacao no catalogo.</summary>
public sealed record CollectionSyncIssueDto(string Severity, string Code, string RootKey, string RelativePath, string Detail);

/// <summary>Resultado read-only da descoberta das estruturas locais autorizadas.</summary>
public sealed record CollectionSyncDiscoveryDto(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    IReadOnlyList<CollectionSyncRootDto> Roots,
    IReadOnlyList<CollectionSyncFolderDto> Folders,
    IReadOnlyList<CollectionSyncIssueDto> Issues);

/// <summary>Solicita a validacao e aplicacao de uma descoberta de colecoes.</summary>
public sealed record CollectionSyncRequest(CollectionSyncDiscoveryDto Discovery);

/// <summary>Ocorrencia persistida de uma sincronizacao de colecoes.</summary>
public sealed record CollectionSyncEventDto(
    long Id,
    DateTimeOffset OccurredAtUtc,
    string Severity,
    string Code,
    string RootKey,
    string RelativePath,
    int? MyAnimeId,
    string? PreviousRootKey,
    string? NewRootKey,
    string Detail);

/// <summary>Resultado completo de uma sincronizacao, incluindo todas as ocorrencias.</summary>
public sealed record CollectionSyncResultDto(
    Guid RunId,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    int AuthorizedRoots,
    int AvailableRoots,
    int DiscoveredFolders,
    int AddedMappings,
    int UpdatedMappings,
    int UnchangedMappings,
    int MissingCatalog,
    int AmbiguousFolders,
    int Conflicts,
    int Errors,
    int Warnings,
    IReadOnlyList<CollectionSyncEventDto> Events);
