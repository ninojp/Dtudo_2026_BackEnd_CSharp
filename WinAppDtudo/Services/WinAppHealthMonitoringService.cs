using System.Net;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Text.Json;

namespace WinAppDtudo.Services;

public enum WinAppHealthState
{
    Healthy,
    Warning,
    Critical,
    Unavailable
}

public sealed record WinAppHealthItem(
    string Category,
    string Name,
    WinAppHealthState State,
    string Summary,
    DateTimeOffset CheckedAtUtc,
    long? AvailableBytes = null,
    long? TotalBytes = null,
    int? PendingCount = null,
    int? ThreatCount = null)
{
    public bool RequiresNotification => State == WinAppHealthState.Critical;
}

public sealed record WinAppHealthSnapshot(
    DateTimeOffset CheckedAtUtc,
    IReadOnlyList<WinAppHealthItem> Items)
{
    public IReadOnlyList<WinAppHealthItem> CriticalItems =>
        Items.Where(item => item.State == WinAppHealthState.Critical).ToArray();
}

public sealed record WinAppHealthCertificateTarget(string Name, Uri BaseUrl);

public sealed class WinAppHealthMonitoringOptions
{
    private static readonly Uri UnavailableBaseUrl = new("https://127.0.0.1:1/", UriKind.Absolute);

    public Uri IdentityBaseUrl { get; init; } = UnavailableBaseUrl;
    public Uri ApiMyAnimesBaseUrl { get; init; } = UnavailableBaseUrl;
    public Uri ApiMusicXBaseUrl { get; init; } = UnavailableBaseUrl;
    public Uri ApiMyAnimeListBaseUrl { get; init; } = UnavailableBaseUrl;
    public Uri ApiFileStorageBaseUrl { get; init; } = UnavailableBaseUrl;
    public IReadOnlyList<WinAppHealthCertificateTarget> CertificateTargets { get; init; } = [];
    public string? BackupRoot { get; init; } = AppConfigurationService.BackupRoot;
    public TimeSpan ProbeTimeout { get; init; } = AppConfigurationService.HealthProbeTimeout;

    public static WinAppHealthMonitoringOptions CreateDefault() => new()
    {
        IdentityBaseUrl = CreateUriOrUnavailable(AppConfigurationService.ApiIdentityBaseUrl),
        ApiMyAnimesBaseUrl = CreateUriOrUnavailable(AppConfigurationService.ApiMyAnimesBaseUrl),
        ApiMusicXBaseUrl = CreateUriOrUnavailable(AppConfigurationService.ApiMusicXBaseUrl),
        ApiMyAnimeListBaseUrl = CreateUriOrUnavailable(AppConfigurationService.ApiMyAnimeListBaseUrl),
        ApiFileStorageBaseUrl = CreateUriOrUnavailable(AppConfigurationService.ApiFileStorageBaseUrl),
        CertificateTargets =
        [
            new("ApiIdentity", CreateUriOrUnavailable(AppConfigurationService.ApiIdentityBaseUrl)),
            new("ApiMyAnimes", CreateUriOrUnavailable(AppConfigurationService.ApiMyAnimesBaseUrl)),
            new("ApiMusicX", CreateUriOrUnavailable(AppConfigurationService.ApiMusicXBaseUrl)),
            new("ApiMyAnimeList", CreateUriOrUnavailable(AppConfigurationService.ApiMyAnimeListBaseUrl)),
            new("ApiFileStorage", CreateUriOrUnavailable(AppConfigurationService.ApiFileStorageBaseUrl))
        ],
        BackupRoot = AppConfigurationService.BackupRoot,
        ProbeTimeout = AppConfigurationService.HealthProbeTimeout
    };

    private static Uri CreateUriOrUnavailable(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme is "https" or "http"
            ? uri
            : UnavailableBaseUrl;
}

public interface IWinAppCertificateHealthProbe
{
    Task<WinAppHealthItem> CheckAsync(
        WinAppHealthCertificateTarget target,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

public sealed class TlsCertificateHealthProbe : IWinAppCertificateHealthProbe
{
    public async Task<WinAppHealthItem> CheckAsync(
        WinAppHealthCertificateTarget target,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.UtcNow;
        X509Certificate2? certificate = null;
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, serverCertificate, _, errors) =>
            {
                if (serverCertificate is not null)
                {
                    certificate = new X509Certificate2(serverCertificate);
                }

#if DEBUG
                return errors == SslPolicyErrors.None || AppConfigurationService.AllowInvalidCertificates;
#else
                return errors == SslPolicyErrors.None;
#endif
            }
        };
        using var client = new HttpClient(handler) { Timeout = timeout };

        try
        {
            using var response = await client.GetAsync(
                target.BaseUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            certificate?.Dispose();
            return new WinAppHealthItem(
                "Certificados",
                target.Name,
                WinAppHealthState.Unavailable,
                "Certificado indisponivel para consulta.",
                checkedAt);
        }
        catch (HttpRequestException)
        {
            certificate?.Dispose();
            return new WinAppHealthItem(
                "Certificados",
                target.Name,
                WinAppHealthState.Unavailable,
                "Certificado indisponivel para consulta.",
                checkedAt);
        }
        catch (CryptographicException)
        {
            certificate?.Dispose();
            return new WinAppHealthItem(
                "Certificados",
                target.Name,
                WinAppHealthState.Unavailable,
                "Certificado indisponivel para consulta.",
                checkedAt);
        }
        catch (AuthenticationException)
        {
            certificate?.Dispose();
            return new WinAppHealthItem(
                "Certificados",
                target.Name,
                WinAppHealthState.Unavailable,
                "Certificado indisponivel para consulta.",
                checkedAt);
        }

        if (certificate is null)
        {
            return new WinAppHealthItem(
                "Certificados",
                target.Name,
                WinAppHealthState.Unavailable,
                "O servidor nao apresentou certificado.",
                checkedAt);
        }

        using (certificate)
        {
            var now = DateTimeOffset.UtcNow;
            var notBefore = new DateTimeOffset(certificate.NotBefore);
            var notAfter = new DateTimeOffset(certificate.NotAfter);
            if (notAfter <= now || notBefore > now)
            {
                return new WinAppHealthItem(
                    "Certificados",
                    target.Name,
                    WinAppHealthState.Critical,
                    "Certificado fora do periodo de validade.",
                    checkedAt);
            }

            var daysRemaining = (notAfter - now).TotalDays;
            var state = daysRemaining <= 30
                ? WinAppHealthState.Warning
                : WinAppHealthState.Healthy;
            return new WinAppHealthItem(
                "Certificados",
                target.Name,
                state,
                daysRemaining <= 30
                    ? $"Certificado expira em {Math.Max(0, (int)Math.Floor(daysRemaining))} dia(s)."
                    : "Certificado valido.",
                checkedAt);
        }
    }
}

public sealed class WinAppHealthMonitoringService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly WinAppAuthenticationService _authenticationService;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly WinAppHealthMonitoringOptions _options;
    private readonly IWinAppCertificateHealthProbe _certificateProbe;
    private bool _disposed;

    public WinAppHealthMonitoringService(
        WinAppAuthenticationService authenticationService,
        HttpClient? httpClient = null,
        WinAppHealthMonitoringOptions? options = null,
        IWinAppCertificateHealthProbe? certificateProbe = null)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _httpClient = httpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler());
        _ownsHttpClient = httpClient is null;
        _options = options ?? WinAppHealthMonitoringOptions.CreateDefault();
        _certificateProbe = certificateProbe ?? new TlsCertificateHealthProbe();
    }

    public async Task<WinAppHealthSnapshot> CheckAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var checkedAt = DateTimeOffset.UtcNow;
        var remoteChecks = await Task.WhenAll(
            CheckServiceAsync("ApiIdentity", _options.IdentityBaseUrl, "health/ready", cancellationToken),
            CheckServiceAsync("ApiMyAnimes", _options.ApiMyAnimesBaseUrl, "apiLocal/Health", cancellationToken),
            CheckServiceAsync("ApiMusicX", _options.ApiMusicXBaseUrl, "apiLocal/Health", cancellationToken),
            CheckServiceAsync("ApiMyAnimeList", _options.ApiMyAnimeListBaseUrl, "ApiMyAnimeList/health", cancellationToken),
            CheckFileStorageAsync(cancellationToken));

        var items = new List<WinAppHealthItem>(remoteChecks.Sum(result => result.Count) + 8);
        foreach (var result in remoteChecks)
        {
            items.AddRange(result);
        }

        items.Add(CheckSecurity(checkedAt));
        var certificateChecks = await Task.WhenAll(_options.CertificateTargets.Select(certificateTarget =>
            _certificateProbe.CheckAsync(
                certificateTarget,
                NormalizeTimeout(_options.ProbeTimeout),
                cancellationToken)));
        items.AddRange(certificateChecks);

        items.Add(WinAppBackupHealthProbe.Check(_options.BackupRoot, checkedAt));
        return new WinAppHealthSnapshot(checkedAt, items);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<IReadOnlyList<WinAppHealthItem>> CheckFileStorageAsync(CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.UtcNow;
        using var response = await SendAsync(
            _options.ApiFileStorageBaseUrl,
            "api/file-storage/health",
            cancellationToken);
        if (response is null)
        {
            return [CreateUnavailableItem("ApiFileStorage", checkedAt)];
        }

        if (!response.IsSuccessStatusCode)
        {
            return [CreateResponseStatusItem("ApiFileStorage", response.StatusCode, checkedAt)];
        }

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<FileStorageHealthPayload>(JsonOptions, cancellationToken);
            if (payload is null || !string.Equals(payload.Status, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return [new WinAppHealthItem(
                    "Servicos",
                    "ApiFileStorage",
                    WinAppHealthState.Critical,
                    "O servico nao confirmou o estado operacional.",
                    checkedAt)];
            }

            var items = new List<WinAppHealthItem>
            {
                new("Servicos", "ApiFileStorage", WinAppHealthState.Healthy, "Servico operacional.", checkedAt)
            };
            items.Add(CreateStorageSpaceItem(payload, checkedAt));
            items.Add(CreateQuarantineItem(payload, checkedAt));
            if (payload.Scanner is not null)
            {
                items.Add(CreateScannerItem(payload.Scanner, checkedAt));
            }

            return items;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return [new WinAppHealthItem(
                "Servicos",
                "ApiFileStorage",
                WinAppHealthState.Critical,
                "A resposta de monitoramento e invalida.",
                checkedAt)];
        }
    }

    private async Task<IReadOnlyList<WinAppHealthItem>> CheckServiceAsync(
        string serviceName,
        Uri baseUrl,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var checkedAt = DateTimeOffset.UtcNow;
        using var response = await SendAsync(baseUrl, relativePath, cancellationToken);
        return response is null
            ? [CreateUnavailableItem(serviceName, checkedAt)]
            : [CreateResponseStatusItem(serviceName, response.StatusCode, checkedAt)];
    }

    private async Task<HttpResponseMessage?> SendAsync(
        Uri baseUrl,
        string relativePath,
        CancellationToken cancellationToken)
    {
        if (baseUrl is null || !baseUrl.IsAbsoluteUri || baseUrl.Scheme is not ("https" or "http"))
        {
            return null;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(NormalizeTimeout(_options.ProbeTimeout));
        try
        {
            return await _authenticationService.SendAuthenticatedAsync(
                _httpClient,
                _ => new HttpRequestMessage(HttpMethod.Get, new Uri(baseUrl, relativePath)),
                timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (WinAppAuthenticationException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private WinAppHealthItem CheckSecurity(DateTimeOffset checkedAt)
    {
        var session = _authenticationService.CurrentSession;
        if (!_authenticationService.IsAuthenticated || session is null)
        {
            return new WinAppHealthItem(
                "Seguranca",
                "Sessao administrativa",
                WinAppHealthState.Unavailable,
                "Conecte o WinApp para consultar a saude protegida.",
                checkedAt);
        }

        var sessionRemaining = session.SessionExpiresAtUtc - checkedAt;
        if (sessionRemaining <= TimeSpan.Zero)
        {
            return new WinAppHealthItem(
                "Seguranca",
                "Sessao administrativa",
                WinAppHealthState.Critical,
                "A sessao administrativa expirou.",
                checkedAt);
        }

        var accessTokenRemaining = session.AccessTokenExpiresAtUtc - checkedAt;
        if (accessTokenRemaining <= TimeSpan.Zero)
        {
            return new WinAppHealthItem(
                "Seguranca",
                "Sessao administrativa",
                WinAppHealthState.Warning,
                "O token de acesso expirou; a renovacao sera tentada.",
                checkedAt);
        }

        var sessionExpiring = sessionRemaining <= TimeSpan.FromMinutes(5);
        var accessTokenExpiring = accessTokenRemaining <= TimeSpan.FromMinutes(5);
        var state = sessionExpiring || accessTokenExpiring
            ? WinAppHealthState.Warning
            : WinAppHealthState.Healthy;
        var summary = sessionExpiring
            ? "A sessao administrativa expira em breve."
            : accessTokenExpiring
                ? "O token de acesso expira em breve; a renovacao sera tentada."
                : "Sessao administrativa ativa.";
        return new WinAppHealthItem(
            "Seguranca",
            "Sessao administrativa",
            state,
            summary,
            checkedAt);
    }

    private static WinAppHealthItem CreateResponseStatusItem(
        string serviceName,
        HttpStatusCode statusCode,
        DateTimeOffset checkedAt)
    {
        if ((int)statusCode is >= 200 and <= 299)
        {
            return new WinAppHealthItem("Servicos", serviceName, WinAppHealthState.Healthy, "Servico operacional.", checkedAt);
        }

        return new WinAppHealthItem(
            "Servicos",
            serviceName,
            statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? WinAppHealthState.Warning
                : WinAppHealthState.Unavailable,
            statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? statusCode == HttpStatusCode.Forbidden
                    ? "O servico respondeu, mas a conta nao possui a permissao health.read."
                    : "O servico respondeu, mas a sessao nao foi aceita."
                : "Servico indisponivel para consulta.",
            checkedAt);
    }

    private static WinAppHealthItem CreateUnavailableItem(string serviceName, DateTimeOffset checkedAt) =>
        new("Servicos", serviceName, WinAppHealthState.Unavailable, "Fonte indisponivel para consulta.", checkedAt);

    private static WinAppHealthItem CreateStorageSpaceItem(
        FileStorageHealthPayload payload,
        DateTimeOffset checkedAt)
    {
        var roots = payload.Roots ?? [];
        if (roots.Count == 0
            || roots.Any(root => string.Equals(root.Status, "unavailable", StringComparison.OrdinalIgnoreCase)))
        {
            return new WinAppHealthItem(
                "Espaco",
                "Armazenamento",
                WinAppHealthState.Unavailable,
                "Nenhuma raiz de armazenamento confirmou disponibilidade.",
                checkedAt);
        }

        var available = roots.Sum(root => Math.Max(0, root.AvailableBytes));
        var total = roots.Sum(root => Math.Max(0, root.TotalBytes));
        var critical = roots.Any(root =>
            string.Equals(root.Status, "critical", StringComparison.OrdinalIgnoreCase)
            || root.AvailableBytes < root.MinimumFreeSpaceBytes);
        var warning = !critical && total > 0 && available / (double)total <= 0.15;
        var state = critical
            ? WinAppHealthState.Critical
            : warning
                ? WinAppHealthState.Warning
                : WinAppHealthState.Healthy;
        var summary = critical
            ? "A reserva minima de espaco nao foi atendida."
            : $"{FormatBytes(available)} livres de {FormatBytes(total)}.";
        return new WinAppHealthItem("Espaco", "Armazenamento", state, summary, checkedAt, available, total);
    }

    private static WinAppHealthItem CreateQuarantineItem(
        FileStorageHealthPayload payload,
        DateTimeOffset checkedAt)
    {
        var quarantine = payload.Quarantine;
        if (quarantine is null || !string.Equals(quarantine.Status, "ok", StringComparison.OrdinalIgnoreCase))
        {
            return new WinAppHealthItem(
                "Quarentena",
                "Ciclo de arquivos",
                WinAppHealthState.Unavailable,
                "O estado da quarentena nao esta disponivel.",
                checkedAt);
        }

        var pending = quarantine.PendingCount;
        var threats = quarantine.ThreatCount;
        var state = threats > 0
            ? WinAppHealthState.Critical
            : pending > 0
                ? WinAppHealthState.Warning
                : WinAppHealthState.Healthy;
        var summary = threats > 0
            ? $"{threats} arquivo(s) recusado(s) aguardam retencao."
            : pending > 0
                ? $"{pending} operacao(oes) aguardam reconciliacao."
                : "Quarentena sem pendencias criticas.";
        return new WinAppHealthItem("Quarentena", "Ciclo de arquivos", state, summary, checkedAt, PendingCount: pending, ThreatCount: threats);
    }

    private static WinAppHealthItem CreateScannerItem(
        FileStorageScannerHealthPayload scanner,
        DateTimeOffset checkedAt)
    {
        var state = scanner.Status switch
        {
            "ok" => WinAppHealthState.Healthy,
            "warning" => WinAppHealthState.Warning,
            _ => WinAppHealthState.Unavailable
        };
        return new WinAppHealthItem(
            "Seguranca",
            "Scanner de arquivos",
            state,
            scanner.Status switch
            {
                "ok" => "Scanner obrigatorio configurado.",
                "warning" => "Scanner requer verificacao operacional.",
                _ => "Scanner indisponivel; a promocao deve permanecer bloqueada."
            },
            checkedAt);
    }

    private static TimeSpan NormalizeTimeout(TimeSpan timeout) =>
        timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(5) : timeout;

    private static string FormatBytes(long value)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var amount = (double)Math.Max(0, value);
        var unit = 0;
        while (amount >= 1024 && unit < units.Length - 1)
        {
            amount /= 1024;
            unit++;
        }

        return $"{amount:0.##} {units[unit]}";
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed record FileStorageHealthPayload(
        string? Status,
        IReadOnlyList<FileStorageRootHealthPayload>? Roots,
        FileStorageScannerHealthPayload? Scanner,
        FileStorageQuarantineHealthPayload? Quarantine);

    private sealed record FileStorageRootHealthPayload(
        string? Status,
        long AvailableBytes,
        long TotalBytes,
        long MinimumFreeSpaceBytes);

    private sealed record FileStorageScannerHealthPayload(string? Status);

    private sealed record FileStorageQuarantineHealthPayload(
        string? Status,
        int PendingCount,
        int ThreatCount);
}

public static class WinAppBackupHealthProbe
{
    private static readonly TimeSpan MaximumBackupAge = TimeSpan.FromHours(24);

    public static WinAppHealthItem Check(string? backupRoot, DateTimeOffset checkedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(backupRoot))
        {
            return new WinAppHealthItem(
                "Backup",
                "Backup diario",
                WinAppHealthState.Unavailable,
                "Nenhuma raiz de backup foi configurada.",
                checkedAtUtc);
        }

        try
        {
            if (!Directory.Exists(backupRoot))
            {
                return Unavailable(checkedAtUtc);
            }

            var latest = Directory.EnumerateDirectories(backupRoot)
                .Select(path => new { Path = path, Name = Path.GetFileName(path) })
                .Where(item => item.Name is not null && item.Name.Length == 8)
                .Select(item => DateTime.TryParseExact(
                    item.Name,
                    "yyyyMMdd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var date)
                    ? new { item.Path, Date = new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)) }
                    : null)
                .Where(item => item is not null)
                .OrderByDescending(item => item!.Date)
                .FirstOrDefault();
            if (latest is null)
            {
                return new WinAppHealthItem(
                    "Backup",
                    "Backup diario",
                    WinAppHealthState.Critical,
                    "Nenhum backup diario valido foi encontrado.",
                    checkedAtUtc);
            }

            var manifestPath = Path.Combine(latest.Path, "manifest.json");
            var hashPath = Path.Combine(latest.Path, "manifest.sha256");
            if (!File.Exists(manifestPath) || !File.Exists(hashPath))
            {
                return new WinAppHealthItem(
                    "Backup",
                    "Backup diario",
                    WinAppHealthState.Critical,
                    "O backup mais recente nao possui manifesto completo.",
                    checkedAtUtc);
            }

            var expectedHash = File.ReadLines(hashPath).FirstOrDefault()?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            var actualHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(manifestPath)));
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                return new WinAppHealthItem(
                    "Backup",
                    "Backup diario",
                    WinAppHealthState.Critical,
                    "O manifesto do backup possui hash divergente.",
                    checkedAtUtc);
            }

            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!document.RootElement.TryGetProperty("BackupType", out var backupType)
                || !string.Equals(backupType.GetString(), "Dtudo", StringComparison.Ordinal)
                || !document.RootElement.TryGetProperty("CreatedUtc", out var createdUtcElement)
                || !DateTimeOffset.TryParse(createdUtcElement.GetString(), out var createdUtc))
            {
                return new WinAppHealthItem(
                    "Backup",
                    "Backup diario",
                    WinAppHealthState.Critical,
                    "O manifesto do backup possui formato invalido.",
                    checkedAtUtc);
            }

            var age = checkedAtUtc - createdUtc.ToUniversalTime();
            var state = age > MaximumBackupAge
                ? WinAppHealthState.Critical
                : age > TimeSpan.FromHours(20)
                    ? WinAppHealthState.Warning
                    : WinAppHealthState.Healthy;
            return new WinAppHealthItem(
                "Backup",
                "Backup diario",
                state,
                state == WinAppHealthState.Healthy
                    ? "Backup recente e manifesto verificado."
                    : $"Ultimo backup ha {Math.Max(0, (int)age.TotalHours)} hora(s).",
                checkedAtUtc);
        }
        catch (IOException)
        {
            return Unavailable(checkedAtUtc);
        }
        catch (UnauthorizedAccessException)
        {
            return Unavailable(checkedAtUtc);
        }
        catch (ArgumentException)
        {
            return Unavailable(checkedAtUtc);
        }
        catch (JsonException)
        {
            return new WinAppHealthItem(
                "Backup",
                "Backup diario",
                WinAppHealthState.Critical,
                "O manifesto do backup nao pode ser lido.",
                checkedAtUtc);
        }
    }

    private static WinAppHealthItem Unavailable(DateTimeOffset checkedAtUtc) =>
        new(
            "Backup",
            "Backup diario",
            WinAppHealthState.Unavailable,
            "A raiz de backup nao esta disponivel para consulta.",
            checkedAtUtc);
}
