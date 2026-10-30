using System.Collections.Frozen;

namespace LibDtudo.Shared.Dtos.FileMonitoring;

/// <summary>
/// Politica unica para decidir o que e um video de episodio. Usada pela ApiFileStorage (contagem da pasta do anime)
/// e pelo WinAppDtudo (classificacao visual do inventario); nao ha copia local dessas regras.
/// </summary>
public static class EpisodeVideoPolicy
{
    /// <summary>Extensoes (com ponto, sem diferenciar maiusculas) tratadas como video.</summary>
    public static IReadOnlySet<string> VideoExtensions { get; } = new[]
    {
        ".3g2", ".3gp", ".asf", ".avi", ".divx", ".flv", ".m2ts", ".m2v", ".m4v", ".mkv", ".mov", ".mp4", ".mpe",
        ".mpeg", ".mpg", ".mts", ".ogv", ".rm", ".rmvb", ".ts", ".vob", ".webm", ".wmv"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Palavras que identificam extras (aberturas/encerramentos limpos, trailers, comerciais etc.).</summary>
    public static IReadOnlySet<string> ExtraNames => extraNames;

    private static readonly FrozenSet<string> extraNames = new[]
    {
        "NCOP", "NCED", "Creditless", "PV", "CM", "Trailer", "Extras", "Featurettes"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Indica se a extensao (por exemplo <c>.mkv</c>) e de video.</summary>
    public static bool IsVideoExtension(string? extension) =>
        !string.IsNullOrEmpty(extension) && VideoExtensions.Contains(extension);

    /// <summary>Indica se o nome do arquivo tem extensao de video, independentemente de tamanho ou extras.</summary>
    public static bool IsVideoFileName(string? fileName) =>
        !string.IsNullOrEmpty(fileName) && IsVideoExtension(Path.GetExtension(fileName));

    /// <summary>
    /// Indica se o nome de pasta ou de arquivo (sem extensao) e de extra. A comparacao e por palavra inteira:
    /// separadores nao alfanumericos dividem o nome, numeros finais (<c>NCOP2</c>) e o plural simples (<c>Trailers</c>) sao aceitos.
    /// </summary>
    public static bool IsExtraName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var span = name.AsSpan();
        var start = -1;
        for (var index = 0; index <= span.Length; index++)
        {
            var isWordChar = index < span.Length && char.IsLetterOrDigit(span[index]);
            if (isWordChar)
            {
                if (start < 0) start = index;
                continue;
            }
            if (start >= 0 && IsExtraWord(span[start..index])) return true;
            start = -1;
        }
        return false;
    }

    /// <summary>Indica se o nome de uma pasta indica extras; essas pastas nao sao percorridas.</summary>
    public static bool IsExtraDirectoryName(string? name) => IsExtraName(name);

    /// <summary>Video de episodio: extensao de video, mais de 0 byte e nome que nao seja de extra.</summary>
    public static bool IsEpisodeVideoFile(string? fileName, long lengthBytes) =>
        lengthBytes > 0 && IsVideoFileName(fileName) && !IsExtraName(Path.GetFileNameWithoutExtension(fileName));

    /// <summary>
    /// Mesma regra aplicada a um caminho relativo a pasta do anime (separadores <c>\</c> ou <c>/</c>):
    /// qualquer pasta intermediaria de extras tambem exclui o arquivo.
    /// </summary>
    public static bool IsEpisodeVideoPath(string? relativePath, long lengthBytes)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return false;
        var segments = relativePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return false;
        for (var index = 0; index < segments.Length - 1; index++)
            if (IsExtraDirectoryName(segments[index])) return false;
        return IsEpisodeVideoFile(segments[^1], lengthBytes);
    }

    private static bool IsExtraWord(ReadOnlySpan<char> word)
    {
        var candidate = word.TrimEnd("0123456789");
        if (candidate.IsEmpty) return false;
        var lookup = extraNames.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.Contains(candidate)) return true;
        return candidate.Length > 1 && (candidate[^1] is 's' or 'S') && lookup.Contains(candidate[..^1]);
    }
}
