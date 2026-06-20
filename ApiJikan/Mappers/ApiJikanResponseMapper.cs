using ApiJikan.Dtos.External;
using ApiJikan.Dtos.Responses;

namespace ApiJikan.Mappers;

/// <summary>
/// Centraliza o mapeamento entre DTOs externos da Jikan e DTOs públicos da API.
/// </summary>
public static class ApiJikanResponseMapper
{
    /// <summary>
    /// BuscarAnimePorNomeResponseDto...
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static BuscarAnimePorNomeResponseDto Map(JikanAnimeSearchResponseDto source)
    {
        var results = source.Data?
            .Select(anime => new AnimeBuscaResumoDto
            {
                MalId = anime.Mal_Id,
                Url = anime.Url,
                Title = anime.Title,
                TitleEnglish = anime.Title_English,
                TitleJapanese = anime.Title_Japanese,
                ImageUrl = anime.Images?.GetValueOrDefault("jpg")?.Large_Image_Url
                    ?? anime.Images?.GetValueOrDefault("jpg")?.Image_Url,
                Type = anime.Type,
                Episodes = anime.Episodes,
                Status = anime.Status,
                Score = anime.Score,
                Year = anime.Year,
                Genres = anime.Genres?
                    .Select(g => g.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Cast<string>()
                    .ToList() ?? new List<string>()
            })
            .ToList() ?? new List<AnimeBuscaResumoDto>();

        return new BuscarAnimePorNomeResponseDto
        {
            Results = results,
            CurrentPage = source.Pagination?.Current_Page ?? 1,
            TotalPages = source.Pagination?.Last_Visible_Page ?? 1,
            HasNextPage = source.Pagination?.Has_Next_Page ?? false,
            TotalResults = source.Pagination?.Items?.Total ?? results.Count
        };
    }

    public static BuscarAnimePorIdResponseDto Map(JikanAnimeDetailsDto source)
    {
        return new BuscarAnimePorIdResponseDto
        {
            MalId = source.Mal_Id,
            Url = source.Url,
            Images = MapImages(source.Images),
            Trailer = MapTrailer(source.Trailer),
            Approved = source.Approved,
            Title = source.Title,
            TitleEnglish = source.Title_English,
            TitleJapanese = source.Title_Japanese,
            TitleSynonyms = source.Title_Synonyms ?? new List<string>(),
            Type = source.Type,
            Source = source.Source,
            Episodes = source.Episodes,
            Status = source.Status,
            Airing = source.Airing,
            Aired = MapAired(source.Aired),
            Duration = source.Duration,
            Rating = source.Rating,
            Score = source.Score,
            ScoredBy = source.Scored_By,
            Rank = source.Rank,
            Popularity = source.Popularity,
            Members = source.Members,
            Favorites = source.Favorites,
            Synopsis = source.Synopsis,
            Background = source.Background,
            Season = source.Season,
            Year = source.Year,
            Producers = MapNamedItems(source.Producers),
            Licensors = MapNamedItems(source.Licensors),
            Studios = MapNamedItems(source.Studios),
            Genres = MapNamedItems(source.Genres),
            ExplicitGenres = MapNamedItems(source.Explicit_Genres),
            Themes = MapNamedItems(source.Themes),
            Demographics = MapNamedItems(source.Demographics)
        };
    }

    public static List<AnimeRelationGroupDto> Map(List<JikanAnimeRelationGroupDto>? source)
    {
        return source?.Select(group => new AnimeRelationGroupDto
            {
                Relation = group.Relation,
                Entry = group.Entry?.Select(entry => new AnimeRelationEntryDto
                    {
                        MalId = entry.Mal_Id,
                        Type = entry.Type,
                        Name = entry.Name,
                        Url = entry.Url
                    }).ToList() ?? new List<AnimeRelationEntryDto>()
            }).ToList() ?? new List<AnimeRelationGroupDto>();
    }
    /// <summary>
    /// Mapea as imagens...
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static AnimeImagesDto? MapImages(Dictionary<string, JikanImageVariantDto>? source)
    {
        if (source == null) return null;
        source.TryGetValue("jpg", out var jpg);
        return new AnimeImagesDto { Jpg = MapImageVariant(jpg) };
    }
    private static AnimeImageVariantDto? MapImageVariant(JikanImageVariantDto? source)
    {
        if (source == null) return null;
        return new AnimeImageVariantDto
        {
            ImageUrl = source.Image_Url,
            SmallImageUrl = source.Small_Image_Url,
            LargeImageUrl = source.Large_Image_Url
        };
    }
    private static string? MapTrailer(JikanTrailerDto? source)
    {
        return source?.Embed_Url;
    }
    private static string? MapAired(JikanAiredDto? source)
    {
        return source?.String;
    }
    private static List<string> MapNamedItems(List<JikanNamedItemDto>? source)
    {
        return source?
            .Select(item => item.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList() ?? new List<string>();
    }
}
