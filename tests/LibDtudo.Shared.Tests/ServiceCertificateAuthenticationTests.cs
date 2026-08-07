using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LibDtudo.Shared.Security;

namespace LibDtudo.Shared.Tests;

public sealed class ServiceCertificateAuthenticationTests
{
    [Fact]
    public void AcceptsTheActiveCertificateForItsClient()
    {
        using var certificate = CreateCertificate("active");
        var binding = CreateBinding(certificate, "service-a");

        var result = new ServiceCertificateValidator().Validate(
            certificate,
            "service-a",
            binding,
            DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded);
        Assert.Equal("service-a", result.ClientId);
    }

    [Fact]
    public void AcceptsThePreviousCertificateOnlyDuringTheOverlapWindow()
    {
        using var active = CreateCertificate("active");
        using var previous = CreateCertificate("previous");
        var now = DateTimeOffset.UtcNow;
        var binding = CreateBinding(active, "service-a");
        binding.CertificateThumbprints = [active.Thumbprint!, previous.Thumbprint!];
        binding.PreviousCertificateAcceptedUntilUtc = now.AddMinutes(5);

        var validator = new ServiceCertificateValidator();
        var duringOverlap = validator.Validate(previous, "service-a", binding, now);
        var afterOverlap = validator.Validate(previous, "service-a", binding, now.AddMinutes(6));

        Assert.True(duringOverlap.Succeeded);
        Assert.False(afterOverlap.Succeeded);
        Assert.Equal("client-certificate-not-registered", afterOverlap.FailureReason);
    }

    [Fact]
    public void RejectsAValidCertificateWhenTheClientIdentityIsDifferent()
    {
        using var certificate = CreateCertificate("service-a");
        var binding = CreateBinding(certificate, "service-a");

        var result = new ServiceCertificateValidator().Validate(
            certificate,
            "service-b",
            binding,
            DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal("client-id-certificate-mismatch", result.FailureReason);
    }

    [Fact]
    public void RejectsAnUnregisteredCertificateAndMissingCertificate()
    {
        using var registered = CreateCertificate("registered");
        using var unregistered = CreateCertificate("unregistered");
        var binding = CreateBinding(registered, "service-a");
        var validator = new ServiceCertificateValidator();

        var wrongCertificate = validator.Validate(
            unregistered,
            "service-a",
            binding,
            DateTimeOffset.UtcNow);
        var missingCertificate = validator.Validate(
            null,
            "service-a",
            binding,
            DateTimeOffset.UtcNow);

        Assert.False(wrongCertificate.Succeeded);
        Assert.Equal("client-certificate-not-registered", wrongCertificate.FailureReason);
        Assert.False(missingCertificate.Succeeded);
        Assert.Equal("client-certificate-required", missingCertificate.FailureReason);
    }

    [Fact]
    public void RejectsACertificateWithoutClientAuthenticationEku()
    {
        using var certificate = CreateCertificate("server", includeClientAuthenticationEku: false);
        var binding = CreateBinding(certificate, "service-a");

        var result = new ServiceCertificateValidator().Validate(
            certificate,
            "service-a",
            binding,
            DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal("client-certificate-eku-invalid", result.FailureReason);
    }

    [Fact]
    public void LoadsTheActiveClientCertificateFromTheCertificateStore()
    {
        using var certificate = CreateCertificate("store-client");
        using var storedCertificate = X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx),
            password: (string?)null,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        store.Add(storedCertificate);

        try
        {
            var binding = CreateBinding(certificate, "service-a");
            var loaded = new ServiceCertificateStore(new ServiceCertificateValidator())
                .LoadClientCertificate(
                    new ServiceCertificateStoreOptions(),
                    binding,
                    DateTimeOffset.UtcNow);

            Assert.NotNull(loaded);
            Assert.True(loaded.HasPrivateKey);
            loaded.Dispose();
        }
        finally
        {
            store.Remove(storedCertificate);
        }
    }

    [Fact]
    public void RejectsAnAudienceOutsideTheClientBinding()
    {
        using var certificate = CreateCertificate("service-a");
        var binding = CreateBinding(certificate, "service-a");
        var request = new ServiceTokenRequest(
            "service-a",
            "different-audience",
            ["service.scope"]);

        var result = new ServiceTokenRequestValidator(new ServiceCertificateValidator())
            .Validate(certificate, request, binding, DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal("audience-not-allowed", result.FailureReason);
    }

    [Fact]
    public void RejectsAScopeOutsideTheClientBinding()
    {
        using var certificate = CreateCertificate("service-a");
        var binding = CreateBinding(certificate, "service-a");
        var request = new ServiceTokenRequest(
            "service-a",
            "service-audience",
            ["service.scope", "catalog.write"]);

        var result = new ServiceTokenRequestValidator(new ServiceCertificateValidator())
            .Validate(certificate, request, binding, DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal("scope-not-allowed", result.FailureReason);
    }

    private static ServiceClientCertificateBinding CreateBinding(
        X509Certificate2 certificate,
        string clientId) => new()
        {
            ClientId = clientId,
            CertificateThumbprints = [certificate.Thumbprint!],
            AllowedScopes = ["service.scope"],
            AllowedAudiences = ["service-audience"]
        };

    private static X509Certificate2 CreateCertificate(
        string name,
        bool includeClientAuthenticationEku = true)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={name}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        if (includeClientAuthenticationEku)
        {
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new(ServiceCertificateValidator.ClientAuthenticationEku) },
                critical: true));
        }

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(30));
    }
}
