using System.Globalization;
using System.Net;
using System.Text;

namespace LibDtudo.Shared.Search;

/// <summary>
/// Normaliza textos de títulos para comparações consistentes, sem diferenciar
/// maiúsculas, acentos, entidades HTML, pontuação ou espaços repetidos.
/// </summary>
public static class AnimeSearchTextNormalizer
{
    public static AnimeSearchText Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return AnimeSearchText.Empty;

        var decoded = WebUtility.HtmlDecode(value).Trim().ToLowerInvariant();
        var decomposed = decoded.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = true;

        foreach (var character in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSpace = false;
                continue;
            }

            if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }

        var normalized = builder.ToString().Trim().Normalize(NormalizationForm.FormC);
        if (normalized.Length == 0) return AnimeSearchText.Empty;

        var compact = normalized.Replace(" ", string.Empty, StringComparison.Ordinal);
        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new AnimeSearchText(normalized, compact, tokens);
    }
}
