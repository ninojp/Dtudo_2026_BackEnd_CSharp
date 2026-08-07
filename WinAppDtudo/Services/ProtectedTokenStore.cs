using System.Security.Cryptography;
using System.Text.Json;

namespace WinAppDtudo.Services;

public sealed record WinAppTokenSet(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset? RefreshTokenExpiresAtUtc,
    Guid SessionId,
    Guid DeviceId);

public sealed class ProtectedTokenStore
{
    private static readonly byte[] Entropy =
    [
        0x44, 0x74, 0x75, 0x64, 0x6f, 0x32, 0x30, 0x32,
        0x36, 0x2e, 0x57, 0x69, 0x6e, 0x41, 0x70, 0x70,
        0x2e, 0x53, 0x65, 0x73, 0x73, 0x69, 0x6f, 0x6e
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _filePath;

    public ProtectedTokenStore(string? filePath = null)
    {
        _filePath = filePath ?? AppConfigurationService.IdentitySessionStorePath;
    }

    public async Task SaveAsync(WinAppTokenSet tokenSet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokenSet);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenSet.AccessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenSet.RefreshToken);

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(tokenSet, JsonOptions);
        byte[]? protectedBytes = null;
        var directory = Path.GetDirectoryName(_filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("O armazenamento da sessao deve possuir um diretorio.");
        }

        var temporaryPath = _filePath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            protectedBytes = ProtectedData.Protect(
                plaintext,
                Entropy,
                DataProtectionScope.CurrentUser);

            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(temporaryPath, protectedBytes, cancellationToken);
            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            if (protectedBytes is not null)
            {
                CryptographicOperations.ZeroMemory(protectedBytes);
            }

            TryDelete(temporaryPath);
        }
    }

    public async Task<WinAppTokenSet?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        byte[] protectedBytes = [];
        byte[]? plaintext = null;
        try
        {
            protectedBytes = await File.ReadAllBytesAsync(_filePath, cancellationToken);
            plaintext = ProtectedData.Unprotect(
                protectedBytes,
                Entropy,
                DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<WinAppTokenSet>(plaintext, JsonOptions);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
            if (plaintext is not null)
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
    }

    public Task ClearAsync()
    {
        TryDelete(_filePath);
        return Task.CompletedTask;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
