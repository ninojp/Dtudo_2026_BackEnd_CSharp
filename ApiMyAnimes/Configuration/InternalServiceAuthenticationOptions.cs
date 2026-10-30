using LibDtudo.Shared.Security;

namespace ApiMyAnimes.Configuration;

/// <summary>Cliente de servico autorizado a chamar as rotas internas (certificado de cliente registrado).</summary>
public sealed class InternalServiceClient
{
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Thumbprint ativo e, opcionalmente, o anterior (rotacao com prazo em <see cref="PreviousCertificateAcceptedUntilUtc"/>).</summary>
    public string[] CertificateThumbprints { get; set; } = [];

    public DateTimeOffset? PreviousCertificateAcceptedUntilUtc { get; set; }

    internal ServiceClientCertificateBinding ToBinding() => new()
    {
        ClientId = ClientId,
        CertificateThumbprints = CertificateThumbprints,
        PreviousCertificateAcceptedUntilUtc = PreviousCertificateAcceptedUntilUtc
    };
}

/// <summary>
/// Autenticacao de entrada servico-a-servico das rotas internas (<c>permission:service.privacy.myanimes</c>).
/// Desabilitada por padrao: sem <see cref="Enabled"/> as rotas respondem <c>403</c> (falha fechada).
/// </summary>
public sealed class InternalServiceAuthenticationOptions
{
    public const string SectionName = "InternalServiceAuthentication";

    public bool Enabled { get; set; }

    public InternalServiceClient[] Clients { get; set; } = [];

    public InternalServiceClient? FindClient(string? clientId) =>
        string.IsNullOrWhiteSpace(clientId)
            ? null
            : Clients.SingleOrDefault(client => string.Equals(client.ClientId, clientId, StringComparison.Ordinal));

    public static bool IsValid(InternalServiceAuthenticationOptions options) =>
        !options.Enabled
        || (options.Clients.Length > 0
            && options.Clients.All(client =>
                !string.IsNullOrWhiteSpace(client.ClientId)
                && client.CertificateThumbprints.Length is >= 1 and <= 2
                && client.CertificateThumbprints.All(value => value.Length == 40 && value.All(char.IsAsciiHexDigit))
                && client.CertificateThumbprints.Distinct(StringComparer.OrdinalIgnoreCase).Count() == client.CertificateThumbprints.Length)
            && options.Clients.Select(client => client.ClientId).Distinct(StringComparer.Ordinal).Count() == options.Clients.Length
            && options.Clients.SelectMany(client => client.CertificateThumbprints)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count()
                == options.Clients.SelectMany(client => client.CertificateThumbprints).Count());
}
