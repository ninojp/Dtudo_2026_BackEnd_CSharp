using Microsoft.AspNetCore.WebUtilities;

namespace ApiIdentity.Configuration;

public sealed class LocalProvisioningOptions
{
    public const string SectionName = "LocalProvisioning";

    public string AdministrationSecret { get; set; } = string.Empty;

    public int InitialSecretLifetimeMinutes { get; set; } = 60;

    public static bool HasValidAdministrationSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            return WebEncoders.Base64UrlDecode(value.Trim()).Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
