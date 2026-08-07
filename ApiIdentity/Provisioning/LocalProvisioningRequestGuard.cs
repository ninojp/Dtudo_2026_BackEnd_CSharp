using ApiIdentity.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ApiIdentity.Provisioning;

public sealed class LocalProvisioningRequestGuard
{
    public const string AdministrationSecretHeader = "X-Dtudo-Administration-Secret";

    private readonly byte[] _expectedSecret;

    public LocalProvisioningRequestGuard(IOptions<LocalProvisioningOptions> options)
    {
        var administrationSecret = options.Value.AdministrationSecret.Trim();
        _expectedSecret = Encoding.UTF8.GetBytes(administrationSecret);
    }

    public bool IsAuthorized(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Connection.RemoteIpAddress is not { } remoteAddress || !IPAddress.IsLoopback(remoteAddress))
        {
            return false;
        }

        if (!context.Request.Headers.TryGetValue(AdministrationSecretHeader, out var suppliedValues)
            || suppliedValues.Count != 1)
        {
            return false;
        }

        var suppliedSecret = Encoding.UTF8.GetBytes(suppliedValues[0]!);
        try
        {
            return suppliedSecret.Length == _expectedSecret.Length
                && CryptographicOperations.FixedTimeEquals(suppliedSecret, _expectedSecret);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(suppliedSecret);
        }
    }
}
