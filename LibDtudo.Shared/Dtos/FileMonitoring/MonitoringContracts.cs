namespace LibDtudo.Shared.Dtos.FileMonitoring;

/// <summary>Raiz autorizada para leitura de colecoes.</summary>
public sealed record MonitoringRootDto(string Key, string Path);
/// <summary>Abre uma sessao geral ou limitada a uma localizacao.</summary>
public sealed record StartMonitoringRequest(string? RootKey = null, string RelativePath = "");
/// <summary>Inventario persistido mais recente de uma localizacao.</summary>
public sealed record MonitoringLocationDto(Guid Id, string RootKey, string RelativePath, Guid? SnapshotId,
    string Completion, long Files, long Directories, long Bytes, DateTimeOffset? InventoriedAtUtc);
/// <summary>Estado da sessao; renovacao explicita mantem a coleta ativa.</summary>
public sealed record MonitoringSessionDto(Guid Id, string State, string? Error, bool InventoryOutdated,
    IReadOnlyList<MonitoringLocationDto> Locations);
/// <summary>Entrada do inventario; nao representa acesso ao conteudo do arquivo.</summary>
public sealed record MonitoringEntryDto(long Id, string RelativePath, bool IsDirectory, long Bytes, DateTimeOffset ModifiedAtUtc);
/// <summary>Ocorrencia observada ou diferenca detectada entre inventarios.</summary>
public sealed record MonitoringEventDto(long Id, Guid LocationId, DateTimeOffset ObservedAtUtc,
    string Source, string Code, string RelativePath, string? PreviousRelativePath, string Detail);
/// <summary>Associacao explicita da colecao com sua estrutura local.</summary>
public sealed record MyAnimeMonitoringLocationDto(string RootKey, string RelativePath);
