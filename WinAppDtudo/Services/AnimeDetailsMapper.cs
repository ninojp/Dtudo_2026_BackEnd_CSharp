using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.MyAnimeList;

namespace WinAppDtudo.Services;

public static class AnimeDetailsMapper
{
    public static AnimeDetails FromLocal(ObterAnimeDto anime)
    {
        ArgumentNullException.ThrowIfNull(anime);

        return new AnimeDetails
        {
            MalId = anime.MalId,
            Url = anime.MalUrl,
            Trailer = anime.Trailer,
            MyAnimeID = anime.MyAnimeID,
            Approved = anime.Approved,
            Title = Preferir(anime.Titulo, anime.Title),
            TitleEnglish = anime.TitleEnglish,
            TitleJapanese = anime.TitleJapanese,
            TitleSynonyms = anime.TitleSynonyms ?? [],
            Type = anime.Type,
            Source = anime.Source,
            Episodes = anime.Episodes ?? (anime.Episodios > 0 ? anime.Episodios : null),
            Status = anime.Status,
            Airing = anime.Airing,
            Aired = anime.Aired,
            Duration = anime.Duration,
            Rating = anime.Rating,
            Score = anime.Score,
            ScoredBy = anime.ScoredBy,
            Rank = anime.Rank,
            Popularity = anime.Popularity,
            Members = anime.Members,
            Favorites = anime.Favorites,
            Synopsis = anime.Synopsis,
            Background = anime.Background,
            Season = anime.Season,
            Year = anime.Year,
            Producers = anime.Producers ?? [],
            Licensors = anime.Licensors ?? [],
            Studios = anime.Studios ?? [],
            Genres = anime.Genres ?? [],
            ExplicitGenres = anime.ExplicitGenres ?? [],
            Themes = anime.Themes ?? [],
            Demographics = anime.Demographics ?? [],
            Images = CriarImagens(anime.ImagensUrlMal)
        };
    }

    private static string? Preferir(string? tituloLocal, string? tituloDetalhado)
        => !string.IsNullOrWhiteSpace(tituloLocal) ? tituloLocal : tituloDetalhado;

    private static AnimeImages? CriarImagens(IEnumerable<string>? urls)
    {
        var url = urls?.FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
        return string.IsNullOrWhiteSpace(url)
            ? null
            : new AnimeImages
            {
                Jpg = new AnimeImageVariant
                {
                    ImageUrl = url,
                    SmallImageUrl = url,
                    LargeImageUrl = url
                }
            };
    }
}
