using LibDtudo.Shared.Models;

namespace ApiMyAnimes.Services;

public static class PublicCatalogPolicy
{
    public static bool IsAdult(Anime anime)
    {
        return IsAdultValue(anime.Rating)
            || anime.Genres.Concat(anime.ExplicitGenres)
                .Concat(anime.Themes)
                .Concat(anime.Demographics)
                .Any(IsAdultValue);
    }

    private static bool IsAdultValue(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized.Contains("hentai", StringComparison.Ordinal)
            || normalized.StartsWith("rx", StringComparison.Ordinal)
            || normalized.StartsWith("r+", StringComparison.Ordinal)
            || normalized.Contains("adult", StringComparison.Ordinal)
            || normalized.Contains("pornographic", StringComparison.Ordinal);
    }
}
