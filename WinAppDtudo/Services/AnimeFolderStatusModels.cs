namespace WinAppDtudo.Services;

/// <summary>
/// Ultimo status de pasta persistido de um anime (campo <c>folder</c> do endpoint em lote <c>POST apiLocal/Anime/status</c>).
/// Os contratos de refresh e de status em lote vem de <c>LibDtudo.Shared.Dtos</c> (<c>AnimeFolderStatusContracts.cs</c>).
/// </summary>
public sealed record AnimeFolderStatusInfo(
    int MalId,
    string State,
    int Videos,
    int? Expected,
    DateTime? CheckedAtUtc);

/// <summary>Tipos de falha ao consultar a ApiMyAnimes no fluxo de atualizacao das pastas.</summary>
public enum FolderRefreshFailureKind
{
    /// <summary>ApiMyAnimes fora do ar, rede indisponivel ou tempo esgotado.</summary>
    ApiUnavailable,

    /// <summary>Sessao do WinApp ausente, expirada ou recusada (401).</summary>
    Unauthorized,

    /// <summary>Usuario sem permissao administrativa (403).</summary>
    Forbidden,

    /// <summary>Colecao ou execucao inexistente (404).</summary>
    NotFound,

    /// <summary>Ja existe uma verificacao em andamento (409).</summary>
    Busy,

    /// <summary>ApiMyAnimes sem ApiFileStorage configurada (503).</summary>
    FileStorageNotConfigured,

    /// <summary>Resposta fora do contrato.</summary>
    InvalidResponse,

    /// <summary>Outra falha HTTP.</summary>
    Other
}

/// <summary>Falha com mensagem clara em portugues, pronta para exibir ao proprietario.</summary>
public sealed class FolderRefreshException : Exception
{
    public FolderRefreshException(
        FolderRefreshFailureKind kind,
        string message,
        System.Net.HttpStatusCode? statusCode = null,
        Guid? activeRunId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
        StatusCode = statusCode;
        ActiveRunId = activeRunId;
    }

    public FolderRefreshFailureKind Kind { get; }

    public System.Net.HttpStatusCode? StatusCode { get; }

    /// <summary>Execucao ja em andamento, quando <see cref="Kind"/> e <see cref="FolderRefreshFailureKind.Busy"/>.</summary>
    public Guid? ActiveRunId { get; }

    /// <summary>Falhas que podem desaparecer sozinhas e merecem nova tentativa durante o acompanhamento.</summary>
    public bool IsTransient => Kind == FolderRefreshFailureKind.ApiUnavailable;
}
