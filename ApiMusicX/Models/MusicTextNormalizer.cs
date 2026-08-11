using System.Globalization;
using System.Text;

namespace ApiMusicX.Models;

public static class MusicTextNormalizer
{
    public static string NormalizeSearchText(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasWhitespace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
            previousWasWhitespace = false;
        }

        var normalized = builder.ToString().Trim();
        return normalized.Length > 0
            ? normalized
            : throw new ArgumentException("O texto nao pode ficar vazio apos a normalizacao.", nameof(value));
    }

    public static string NormalizeExternalValue(string value, string parameterName = "value")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();
        if (normalized.Any(char.IsControl))
        {
            throw new ArgumentException("O identificador externo nao pode conter caracteres de controle.", parameterName);
        }

        return normalized;
    }

    public static string NormalizeRelativePath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        if (trimmed.StartsWith("/", StringComparison.Ordinal)
            || trimmed.StartsWith("\\", StringComparison.Ordinal)
            || (trimmed.Length >= 2 && char.IsAsciiLetter(trimmed[0]) && trimmed[1] == ':'))
        {
            throw new ArgumentException("A referencia local deve ser um caminho relativo.", nameof(value));
        }

        var segments = trimmed
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException("A referencia local nao pode conter segmentos de navegacao.", nameof(value));
        }

        if (segments.Any(segment => segment.Any(char.IsControl)))
        {
            throw new ArgumentException("A referencia local nao pode conter caracteres de controle.", nameof(value));
        }

        return string.Join('/', segments);
    }
}
