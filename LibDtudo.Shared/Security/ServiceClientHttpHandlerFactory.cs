namespace LibDtudo.Shared.Security;

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
