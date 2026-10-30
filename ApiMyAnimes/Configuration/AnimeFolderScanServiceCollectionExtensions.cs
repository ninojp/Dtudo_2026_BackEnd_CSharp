using ApiMyAnimes.Services;
using LibDtudo.Shared.Logging;
using LibDtudo.Shared.Security;
using Microsoft.Extensions.Options;

namespace ApiMyAnimes.Configuration;

/// <summary>Montagem comum dos clientes servico-a-servico (mTLS por certificado do Certificate Store).</summary>
public static class ServiceClientHttpHandlerFactory
{
    /// <summary>Handler primario com o certificado de cliente; sem autenticacao de servico habilitada, um handler padrao.</summary>
    public static HttpClientHandler Create(ServiceClientCredentialsOptions options, ServiceCertificateStore certificateStore)
    {
        if (!options.Enabled) return new HttpClientHandler();

        var certificate = certificateStore.LoadClientCertificate(options.CertificateStore, options.ToBinding(), DateTimeOffset.UtcNow)
            ?? throw new InvalidOperationException("Certificado de cliente do servico nao encontrado no Certificate Store.");
        var handler = new HttpClientHandler { ClientCertificateOptions = ClientCertificateOption.Manual };
        handler.ClientCertificates.Add(certificate);
        return handler;
    }

    /// <summary>Regra de validacao das credenciais de servico (as mesmas para todos os destinos).</summary>
    public static bool AreValid(ServiceClientCredentialsOptions options) =>
        !options.Enabled
        || (Uri.TryCreate(options.TokenEndpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == Uri.UriSchemeHttps
            && !string.IsNullOrWhiteSpace(options.ClientId)
            && Uri.TryCreate(options.Audience, UriKind.Absolute, out _)
            && options.Scopes.Length > 0
            && options.CertificateThumbprints.Length is >= 1 and <= 2
            && options.CertificateThumbprints.All(value => value.Length == 40 && value.All(char.IsAsciiHexDigit)));
}

/// <summary>Registro do cliente da ApiFileStorage e do servico de atualizacao do status de pasta.</summary>
public static class AnimeFolderScanServiceCollectionExtensions
{
    /// <summary>Nome das opcoes de credenciais e da chave do provedor de token do destino ApiFileStorage.</summary>
    public const string FileStorageServiceName = "ApiFileStorage";

    /// <summary>Secao das credenciais de servico da ApiMyAnimes para a ApiFileStorage.</summary>
    public const string FileStorageCredentialsSection = "ServiceAuthentication:ApiFileStorage";

    public static IServiceCollection AddAnimeFolderStatus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiFileStorageOptions>()
            .Bind(configuration.GetSection(ApiFileStorageOptions.SectionName))
            .Validate(ApiFileStorageOptions.IsValid,
                "ApiFileStorage:BaseUrl deve ser vazia ou uma URL HTTPS absoluta e RequestTimeoutSeconds deve estar entre 1 e 300.")
            .ValidateOnStart();
        services.AddOptions<AnimeFolderRefreshOptions>()
            .Bind(configuration.GetSection(AnimeFolderRefreshOptions.SectionName))
            .Validate(AnimeFolderRefreshOptions.IsValid, "FolderStatus:Refresh:MaxConcurrency deve estar entre 1 e 16.")
            .ValidateOnStart();
        services.AddOptions<ServiceClientCredentialsOptions>(FileStorageServiceName)
            .Bind(configuration.GetSection(FileStorageCredentialsSection))
            .Validate(ServiceClientHttpHandlerFactory.AreValid,
                "ServiceAuthentication:ApiFileStorage deve conter endpoint HTTPS, client ID, audience, escopos e thumbprints validos quando habilitado.")
            .ValidateOnStart();

        services.AddKeyedSingleton(FileStorageServiceName, (serviceProvider, _) => new ServiceAccessTokenProvider(
            serviceProvider.GetRequiredService<IOptionsMonitor<ServiceClientCredentialsOptions>>().Get(FileStorageServiceName),
            serviceProvider.GetRequiredService<ServiceCertificateStore>(),
            TimeProvider.System));

        services.AddHttpClient<IAnimeFolderScanClient, AnimeFolderScanClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ApiFileStorageOptions>>().Value;
            if (options.TryGetBaseUri(out var baseUri)) client.BaseAddress = baseUri;
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider => ServiceClientHttpHandlerFactory.Create(
                serviceProvider.GetRequiredService<IOptionsMonitor<ServiceClientCredentialsOptions>>().Get(FileStorageServiceName),
                serviceProvider.GetRequiredService<ServiceCertificateStore>()))
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddHttpMessageHandler(serviceProvider => new ServiceAccessTokenHandler(
                serviceProvider.GetRequiredKeyedService<ServiceAccessTokenProvider>(FileStorageServiceName)));

        services.AddScoped<AnimeFolderRefreshService>();
        services.AddSingleton<AnimeFolderRefreshRunner>();
        return services;
    }
}
