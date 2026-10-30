namespace ApiMyAnimes.Configuration;

/// <summary>Destino das chamadas servico-a-servico da ApiMyAnimes para a ApiFileStorage (<c>ApiFileStorage</c>).</summary>
public sealed class ApiFileStorageOptions
{
    public const string SectionName = "ApiFileStorage";

    /// <summary>URL HTTPS base da ApiFileStorage; vazia significa "nao configurada" (a atualizacao de pastas responde 503).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Tempo maximo de cada chamada (descoberta ou contagem).</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    public bool IsConfigured => TryGetBaseUri(out _);

    public bool TryGetBaseUri(out Uri uri)
    {
        if (Uri.TryCreate(BaseUrl, UriKind.Absolute, out var parsed) && parsed.Scheme == Uri.UriSchemeHttps)
        {
            uri = parsed.AbsoluteUri.EndsWith('/') ? parsed : new Uri(parsed.AbsoluteUri + "/");
            return true;
        }
        uri = null!;
        return false;
    }

    public static bool IsValid(ApiFileStorageOptions options) =>
        (string.IsNullOrWhiteSpace(options.BaseUrl) || options.IsConfigured)
        && options.RequestTimeoutSeconds is >= 1 and <= 300;
}

/// <summary>Limites da atualizacao do status de pasta (<c>FolderStatus:Refresh</c>).</summary>
public sealed class AnimeFolderRefreshOptions
{
    public const string SectionName = "FolderStatus:Refresh";

    /// <summary>Chamadas simultaneas a ApiFileStorage (descobertas ou contagens). A ApiFileStorage tem o proprio limite.</summary>
    public int MaxConcurrency { get; set; } = 4;

    public static bool IsValid(AnimeFolderRefreshOptions options) => options.MaxConcurrency is >= 1 and <= 16;
}
