namespace ApiIdentity.Configuration;

/// <summary>Cliente HTTP da ApiIdentity para as rotas internas de privacidade da ApiMyAnimes (seção <c>PrivacyIntegration:MyAnimes</c>).</summary>
public sealed class MyAnimesPrivacyClientOptions
{
    public const string SectionName = "PrivacyIntegration:MyAnimes";

    /// <summary>Raiz HTTPS da ApiMyAnimes. Vazia = integração não configurada (exportação falha com 503 e a exclusão permanece pendente).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Timeout de CADA tentativa.</summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>Total de tentativas por chamada (1 = sem retry), apenas para falhas transitórias.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Atraso base do backoff exponencial entre tentativas.</summary>
    public int RetryBaseDelayMilliseconds { get; set; } = 250;

    public bool TryGetBaseUri(out Uri baseUri)
    {
        if (Uri.TryCreate(BaseUrl, UriKind.Absolute, out var parsed) && parsed.Scheme == Uri.UriSchemeHttps)
        {
            baseUri = parsed;
            return true;
        }

        baseUri = null!;
        return false;
    }

    public static bool IsValid(MyAnimesPrivacyClientOptions options) =>
        (string.IsNullOrWhiteSpace(options.BaseUrl) || options.TryGetBaseUri(out _))
        && options.RequestTimeoutSeconds is >= 1 and <= 60
        && options.MaxAttempts is >= 1 and <= 5
        && options.RetryBaseDelayMilliseconds is >= 0 and <= 5_000;
}

/// <summary>Worker que processa exclusões LGPD vencidas (seção <c>PrivacyDeletionWorker</c>). Desabilitado por padrão.</summary>
public sealed class PrivacyDeletionWorkerOptions
{
    public const string SectionName = "PrivacyDeletionWorker";

    public bool Enabled { get; set; }

    /// <summary>Intervalo entre varreduras.</summary>
    public int PollIntervalSeconds { get; set; } = 300;

    /// <summary>Espera antes da primeira varredura (a ApiIdentity precisa estar atendendo o próprio endpoint de token).</summary>
    public int InitialDelaySeconds { get; set; } = 30;

    /// <summary>Máximo de pedidos por varredura (processados um por vez).</summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>Espera mínima antes de repetir um pedido que falhou.</summary>
    public int RetryDelayMinutes { get; set; } = 15;

    public static bool IsValid(PrivacyDeletionWorkerOptions options) =>
        options.PollIntervalSeconds is >= 10 and <= 86_400
        && options.InitialDelaySeconds is >= 0 and <= 3_600
        && options.BatchSize is >= 1 and <= 100
        && options.RetryDelayMinutes is >= 1 and <= 1_440;
}
