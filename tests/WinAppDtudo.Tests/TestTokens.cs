using System.Text.Json;

namespace WinAppDtudo.Tests;

internal static class TestTokens
{
    public static string SuperAdministratorAccessToken { get; } = CreateAccessToken("Superadministrador");
    public static string CommonUserAccessToken { get; } = CreateAccessToken("Usuario do Site");

    private static string CreateAccessToken(string role)
    {
        var header = Encode(new { alg = "none", typ = "JWT" });
        var payload = Encode(new
        {
            aud = new[]
            {
                "urn:dtudo:api-my-animes",
                "urn:dtudo:api-my-animelist",
                "urn:dtudo:api-file-storage"
            },
            role
        });

        return $"{header}.{payload}.test-signature";
    }

    private static string Encode(object value) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
