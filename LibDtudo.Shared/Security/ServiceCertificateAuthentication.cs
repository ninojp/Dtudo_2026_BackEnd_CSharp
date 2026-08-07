using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http.Json;

namespace LibDtudo.Shared.Security;

public sealed class ServiceClientCertificateBinding
{
    public string ClientId { get; set; } = string.Empty;

    public string[] CertificateThumbprints { get; set; } = [];

    public DateTimeOffset? PreviousCertificateAcceptedUntilUtc { get; set; }

    public string[] AllowedScopes { get; set; } = [];

    public string[] AllowedAudiences { get; set; } = [];
}

public sealed class ServiceCertificateStoreOptions
{
    public string StoreName { get; set; } = nameof(System.Security.Cryptography.X509Certificates.StoreName.My);

    public string StoreLocation { get; set; } = nameof(System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser);
}

public sealed class ServiceTokenIssuerOptions
{
    public const string SectionName = "ServiceAuthentication";

    public bool Enabled { get; set; }

    public int AccessTokenLifetimeSeconds { get; set; } = 300;

    public ServiceClientCertificateBinding[] Clients { get; set; } = [];

    public ServiceClientCertificateBinding? FindClient(string? clientId) =>
        Clients.SingleOrDefault(client =>
            string.Equals(client.ClientId, clientId, StringComparison.Ordinal));
}

public sealed class ServiceClientCredentialsOptions
{
    public const string SectionName = "ServiceAuthentication:ApiMyAnimeList";

    public bool Enabled { get; set; }

    public string TokenEndpoint { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string[] Scopes { get; set; } = [];

    public string[] CertificateThumbprints { get; set; } = [];

    public DateTimeOffset? PreviousCertificateAcceptedUntilUtc { get; set; }

    public ServiceCertificateStoreOptions CertificateStore { get; set; } = new();

    public ServiceClientCertificateBinding ToBinding() => new()
    {
        ClientId = ClientId,
        CertificateThumbprints = CertificateThumbprints,
        PreviousCertificateAcceptedUntilUtc = PreviousCertificateAcceptedUntilUtc,
        AllowedScopes = Scopes,
        AllowedAudiences = [Audience]
    };
}

public sealed record ServiceTokenRequest(
    string ClientId,
    string Audience,
    IReadOnlyCollection<string> Scopes);

public sealed record ServiceTokenValidationResult(
    bool Succeeded,
    string? ClientId = null,
    string? FailureReason = null);

public sealed record ServiceCertificateValidationResult(
    bool Succeeded,
    string? ClientId = null,
    string? FailureReason = null);

public sealed class ServiceCertificateValidator
{
    public const string ClientAuthenticationEku = "1.3.6.1.5.5.7.3.2";

    public ServiceCertificateValidationResult Validate(
        X509Certificate2? certificate,
        string clientId,
        ServiceClientCertificateBinding binding,
        DateTimeOffset now)
    {
        if (certificate is null)
        {
            return Failure("client-certificate-required");
        }

        if (!string.Equals(clientId, binding.ClientId, StringComparison.Ordinal))
        {
            return Failure("client-id-certificate-mismatch");
        }

        if (certificate.NotBefore.ToUniversalTime() > now.UtcDateTime
            || certificate.NotAfter.ToUniversalTime() < now.UtcDateTime)
        {
            return Failure("client-certificate-expired-or-not-yet-valid");
        }

        if (!HasClientAuthenticationEku(certificate))
        {
            return Failure("client-certificate-eku-invalid");
        }

        var thumbprint = NormalizeThumbprint(certificate.Thumbprint);
        var activeThumbprint = binding.CertificateThumbprints
            .Select(NormalizeThumbprint)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        var previousThumbprint = binding.CertificateThumbprints
            .Select(NormalizeThumbprint)
            .Skip(1)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.Equals(thumbprint, activeThumbprint, StringComparison.Ordinal))
        {
            return Success(binding.ClientId);
        }

        if (string.Equals(thumbprint, previousThumbprint, StringComparison.Ordinal)
            && binding.PreviousCertificateAcceptedUntilUtc is { } acceptedUntil
            && now <= acceptedUntil)
        {
            return Success(binding.ClientId);
        }

        return Failure("client-certificate-not-registered");
    }

    private static bool HasClientAuthenticationEku(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions
            .OfType<X509EnhancedKeyUsageExtension>()
            .SingleOrDefault();

        return extension?.EnhancedKeyUsages
            .Cast<Oid>()
            .Any(oid => string.Equals(oid.Value, ClientAuthenticationEku, StringComparison.Ordinal)) == true;
    }

    private static string NormalizeThumbprint(string? thumbprint) =>
        string.IsNullOrWhiteSpace(thumbprint)
            ? string.Empty
            : thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace(":", string.Empty, StringComparison.Ordinal)
                .ToUpperInvariant();

    private static ServiceCertificateValidationResult Success(string clientId) =>
        new(true, clientId);

    private static ServiceCertificateValidationResult Failure(string reason) =>
        new(false, FailureReason: reason);
}

public sealed class ServiceTokenRequestValidator
{
    private readonly ServiceCertificateValidator _certificateValidator;

    public ServiceTokenRequestValidator(ServiceCertificateValidator certificateValidator)
    {
        _certificateValidator = certificateValidator;
    }

    public ServiceTokenValidationResult Validate(
        X509Certificate2? certificate,
        ServiceTokenRequest request,
        ServiceClientCertificateBinding binding,
        DateTimeOffset now)
    {
        var certificateResult = _certificateValidator.Validate(
            certificate,
            request.ClientId,
            binding,
            now);
        if (!certificateResult.Succeeded)
        {
            return new(false, FailureReason: certificateResult.FailureReason);
        }

        if (!binding.AllowedAudiences.Contains(request.Audience, StringComparer.Ordinal))
        {
            return new(false, FailureReason: "audience-not-allowed");
        }

        if (request.Scopes.Count == 0
            || request.Scopes.Any(scope => !binding.AllowedScopes.Contains(scope, StringComparer.Ordinal)))
        {
            return new(false, FailureReason: "scope-not-allowed");
        }

        return new(true, request.ClientId);
    }
}

public sealed class ServiceCertificateStore
{
    private readonly ServiceCertificateValidator _validator;

    public ServiceCertificateStore(ServiceCertificateValidator validator)
    {
        _validator = validator;
    }

    public X509Certificate2? LoadClientCertificate(
        ServiceCertificateStoreOptions options,
        ServiceClientCertificateBinding binding,
        DateTimeOffset now)
    {
        if (!Enum.TryParse<StoreName>(options.StoreName, ignoreCase: true, out var storeName)
            || !Enum.IsDefined(storeName)
            || !Enum.TryParse<StoreLocation>(options.StoreLocation, ignoreCase: true, out var storeLocation)
            || !Enum.IsDefined(storeLocation))
        {
            return null;
        }

        try
        {
            using var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            foreach (var thumbprint in binding.CertificateThumbprints.Take(2))
            {
                var candidate = store.Certificates
                    .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();
                if (candidate is null || !candidate.HasPrivateKey)
                {
                    continue;
                }

                var validation = _validator.Validate(candidate, binding.ClientId, binding, now);
                if (validation.Succeeded)
                {
                    return new X509Certificate2(candidate);
                }
            }
        }
        catch (CryptographicException)
        {
            return null;
        }

        return null;
    }
}

public sealed class ServiceAccessTokenProvider : IAsyncDisposable
{
    private readonly ServiceClientCredentialsOptions _options;
    private readonly ServiceCertificateStore _certificateStore;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAtUtc;

    public ServiceAccessTokenProvider(
        ServiceClientCredentialsOptions options,
        ServiceCertificateStore certificateStore,
        TimeProvider timeProvider)
    {
        _options = options;
        _certificateStore = certificateStore;
        _timeProvider = timeProvider;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        if (!string.IsNullOrWhiteSpace(_accessToken)
            && _accessTokenExpiresAtUtc > now.AddSeconds(30))
        {
            return _accessToken;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            now = _timeProvider.GetUtcNow();
            if (!string.IsNullOrWhiteSpace(_accessToken)
                && _accessTokenExpiresAtUtc > now.AddSeconds(30))
            {
                return _accessToken;
            }

            if (!_options.Enabled)
            {
                throw new InvalidOperationException("A autenticacao de servico nao esta habilitada.");
            }

            if (!Uri.TryCreate(_options.TokenEndpoint, UriKind.Absolute, out var tokenEndpoint)
                || tokenEndpoint.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("O endpoint de token do servico deve ser HTTPS.");
            }

            using var certificate = _certificateStore.LoadClientCertificate(
                _options.CertificateStore,
                _options.ToBinding(),
                now);
            if (certificate is null)
            {
                throw new InvalidOperationException("Certificado de cliente do servico nao encontrado no Certificate Store.");
            }

            using var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual
            };
            handler.ClientCertificates.Add(certificate);
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["scope"] = string.Join(' ', _options.Scopes),
                    ["resource"] = _options.Audience
                })
            };
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    "O endpoint de token rejeitou as credenciais do servico.",
                    null,
                    response.StatusCode);
            }

            var token = await response.Content.ReadFromJsonAsync<ServiceAccessTokenResponse>(cancellationToken);
            if (token is null
                || string.IsNullOrWhiteSpace(token.AccessToken)
                || token.ExpiresIn <= 0)
            {
                throw new InvalidOperationException("A resposta do endpoint de token nao contem um access token valido.");
            }

            _accessToken = token.AccessToken;
            _accessTokenExpiresAtUtc = now.AddSeconds(token.ExpiresIn);
            return _accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class ServiceAccessTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public int ExpiresIn { get; set; }
    }
}

public sealed class ServiceAccessTokenHandler(ServiceAccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
