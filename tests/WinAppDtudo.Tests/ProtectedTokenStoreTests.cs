using System.Text;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class ProtectedTokenStoreTests
{
    [Fact]
    public async Task RoundTripPersistsOnlyDpapiProtectedBytes()
    {
        var filePath = CreateTemporaryPath();
        const string accessToken = "access-token-clear-value-for-test";
        const string refreshToken = "refresh-token-clear-value-for-test";
        var expected = new WinAppTokenSet(
            accessToken,
            refreshToken,
            DateTimeOffset.UtcNow.AddMinutes(5),
            DateTimeOffset.UtcNow.AddDays(1),
            Guid.NewGuid(),
            Guid.NewGuid());

        try
        {
            var store = new ProtectedTokenStore(filePath);
            await store.SaveAsync(expected);

            var persisted = await File.ReadAllBytesAsync(filePath);
            var persistedText = Encoding.UTF8.GetString(persisted);
            Assert.DoesNotContain(accessToken, persistedText, StringComparison.Ordinal);
            Assert.DoesNotContain(refreshToken, persistedText, StringComparison.Ordinal);
            Assert.False(ContainsSequence(persisted, Encoding.UTF8.GetBytes(accessToken)));
            Assert.False(ContainsSequence(persisted, Encoding.UTF8.GetBytes(refreshToken)));

            var loaded = await store.LoadAsync();
            Assert.Equal(expected, loaded);
        }
        finally
        {
            TryDelete(filePath);
        }
    }

    [Fact]
    public async Task RejectsTamperedProtectedFileAndClearRemovesIt()
    {
        var filePath = CreateTemporaryPath();
        try
        {
            var store = new ProtectedTokenStore(filePath);
            await store.SaveAsync(new WinAppTokenSet(
                "access-token",
                "refresh-token",
                DateTimeOffset.UtcNow.AddMinutes(5),
                DateTimeOffset.UtcNow.AddDays(1),
                Guid.NewGuid(),
                Guid.NewGuid()));

            var bytes = await File.ReadAllBytesAsync(filePath);
            bytes[0] ^= 0xFF;
            await File.WriteAllBytesAsync(filePath, bytes);
            Assert.Null(await store.LoadAsync());

            await store.ClearAsync();
            Assert.False(File.Exists(filePath));
        }
        finally
        {
            TryDelete(filePath);
        }
    }

    private static string CreateTemporaryPath() => Path.Combine(
        Path.GetTempPath(),
        "Dtudo2026",
        "ProtectedTokenStoreTests",
        Guid.NewGuid().ToString("N"),
        "session.bin");

    private static bool ContainsSequence(byte[] bytes, byte[] sequence)
    {
        if (sequence.Length == 0 || sequence.Length > bytes.Length)
        {
            return false;
        }

        for (var start = 0; start <= bytes.Length - sequence.Length; start++)
        {
            if (bytes.AsSpan(start, sequence.Length).SequenceEqual(sequence))
            {
                return true;
            }
        }

        return false;
    }

    private static void TryDelete(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
