using System.Text;

namespace ApiFileStorage.Tests;

internal static class UncheckedStorageObjectId
{
    public static string Create(string rootId, string relativePath)
    {
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(rootId + '\0' + relativePath))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return "v1." + payload;
    }
}
