using ApiIdentity.Authorization;
using ApiIdentity.Configuration;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Logging;
using LibDtudo.Shared.Security;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Privacy;

/// <summary>Regra de allowlist da permissao de servico de privacidade: so vale para a audiencia da ApiMyAnimes.</summary>
public static class PrivacyServiceGrantRules
{
    public const string Scope = AuthorizationCatalog.Permissions.ServicePrivacyMyAnimes;

    public const string Audience = MyAnimesPrivacyContract.ServiceAudience;

    /// <summary>O escopo <c>service.privacy.myanimes</c> nunca pode ser concedido para outra audiencia.</summary>
    public static bool IsAllowed(ServiceClientCertificateBinding client) =>
        client.GetEffectiveGrants()
            .Where(grant => grant.Scopes.Contains(Scope, StringComparer.Ordinal))
            .All(grant => string.Equals(grant.Audience, Audience, StringComparison.Ordinal));

    /// <summary>Credenciais de saida da ApiIdentity para a ApiMyAnimes: audiencia e escopo fixos quando habilitadas.</summary>
    public static bool AreOutboundCredentialsValid(ServiceClientCredentialsOptions options) =>
        ServiceClientHttpHandlerFactory.AreValid(options)
        && (!options.Enabled
            || (string.Equals(options.Audience, Audience, StringComparison.Ordinal)
                && options.Scopes.Length == 1
                && string.Equals(options.Scopes[0], Scope, StringComparison.Ordinal)));
}

/// <summary>Handler primario de fallback: a conexao so falha quando usada, sem derrubar a resolucao do servico de privacidade.</summary>
internal sealed class UnavailableServiceHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Certificado de cliente do servico indisponivel.");
}

/// <summary>Registro do cliente da ApiMyAnimes (mTLS + token de servico) e do worker de exclusao.</summary>
public static class PrivacyIntegrationServiceCollectionExtensions
{
    /// <summary>Nome das opcoes de credenciais e da chave do provedor de token do destino ApiMyAnimes.</summary>
    public const string MyAnimesServiceName = "ApiMyAnimes";

    /// <summary>Secao das credenciais de servico da ApiIdentity para a ApiMyAnimes.</summary>
    public const string MyAnimesCredentialsSection = "ServiceAuthentication:ApiMyAnimes";

    public static IServiceCollection AddPrivacyIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MyAnimesPrivacyClientOptions>()
            .Bind(configuration.GetSection(MyAnimesPrivacyClientOptions.SectionName))
            .Validate(MyAnimesPrivacyClientOptions.IsValid,
                "PrivacyIntegration:MyAnimes: BaseUrl deve ser vazia ou HTTPS absoluta, RequestTimeoutSeconds 1-60, MaxAttempts 1-5 e RetryBaseDelayMilliseconds 0-5000.")
            .ValidateOnStart();
        services.AddOptions<PrivacyDeletionWorkerOptions>()
            .Bind(configuration.GetSection(PrivacyDeletionWorkerOptions.SectionName))
            .Validate(PrivacyDeletionWorkerOptions.IsValid,
                "PrivacyDeletionWorker: PollIntervalSeconds 10-86400, InitialDelaySeconds 0-3600, BatchSize 1-100 e RetryDelayMinutes 1-1440.")
            .ValidateOnStart();
        services.AddOptions<ServiceClientCredentialsOptions>(MyAnimesServiceName)
            .Bind(configuration.GetSection(MyAnimesCredentialsSection))
            .Validate(PrivacyServiceGrantRules.AreOutboundCredentialsValid,
                $"ServiceAuthentication:ApiMyAnimes deve ter endpoint HTTPS, client ID, thumbprints validos, audience {PrivacyServiceGrantRules.Audience} e somente o escopo {PrivacyServiceGrantRules.Scope} quando habilitado.")
            .ValidateOnStart();

        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddKeyedSingleton(MyAnimesServiceName, (serviceProvider, _) => new ServiceAccessTokenProvider(
            serviceProvider.GetRequiredService<IOptionsMonitor<ServiceClientCredentialsOptions>>().Get(MyAnimesServiceName),
            serviceProvider.GetRequiredService<ServiceCertificateStore>(),
            serviceProvider.GetRequiredService<TimeProvider>()));

        services.AddHttpClient<IMyAnimesPrivacyClient, MyAnimesPrivacyClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<MyAnimesPrivacyClientOptions>>().Value;
            if (options.TryGetBaseUri(out var baseUri)) client.BaseAddress = baseUri;
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                try
                {
                    return (HttpMessageHandler)ServiceClientHttpHandlerFactory.Create(
                        serviceProvider.GetRequiredService<IOptionsMonitor<ServiceClientCredentialsOptions>>().Get(MyAnimesServiceName),
                        serviceProvider.GetRequiredService<ServiceCertificateStore>());
                }
                catch (InvalidOperationException)
                {
                    return new UnavailableServiceHandler();
                }
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddHttpMessageHandler(serviceProvider => new ServiceAccessTokenHandler(
                serviceProvider.GetRequiredKeyedService<ServiceAccessTokenProvider>(MyAnimesServiceName)));

        services.AddHostedService<PrivacyDeletionWorker>();
        return services;
    }
}
