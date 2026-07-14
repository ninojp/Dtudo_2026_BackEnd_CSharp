using ApiMyAnimeList.Dtos;

namespace ApiMyAnimeList.Mappers;

public static class MyAnimeListMapper
{
    public static CompatibleSearchResponse MapSearch(MalPagedResponse<MalAnimeNode> source, int page, int limit)
    {
        var results = source.Data.Where(x => x.Node is not null).Select(x =>
        {
            var anime = x.Node!;
            return new CompatibleSearchItem
            {
                MalId = anime.Id,
                Url = Url(anime.Id),
                Title = anime.Title,
                TitleEnglish = anime.AlternativeTitles?.English,
                TitleJapanese = anime.AlternativeTitles?.Japanese,
                ImageUrl = anime.MainPicture?.Large ?? anime.MainPicture?.Medium,
                Type = anime.MediaType,
                Episodes = anime.NumEpisodes,
                Status = anime.Status,
                Score = anime.Mean,
                Year = anime.StartSeason?.Year,
                Genres = Names(anime.Genres)
            };
        }).ToList();

        var hasNext = !string.IsNullOrWhiteSpace(source.Paging?.Next);
        return new CompatibleSearchResponse
        {
            Results = results,
            CurrentPage = page,
            TotalPages = hasNext ? page + 1 : page,
            HasNextPage = hasNext,
            TotalResults = (page - 1) * limit + results.Count + (hasNext ? 1 : 0)
        };
    }

    public static CompatibleDetails MapDetails(MalAnimeNode anime) => new()
    {
        MalId = anime.Id,
        Url = Url(anime.Id),
        Images = new CompatibleImages { Jpg = new CompatibleImageVariant { ImageUrl = anime.MainPicture?.Medium, SmallImageUrl = anime.MainPicture?.Medium, LargeImageUrl = anime.MainPicture?.Large } },
        Approved = true,
        Title = anime.Title,
        TitleEnglish = anime.AlternativeTitles?.English,
        TitleJapanese = anime.AlternativeTitles?.Japanese,
        TitleSynonyms = anime.AlternativeTitles?.Synonyms ?? [],
        Type = anime.MediaType,
        Source = anime.Source,
        Episodes = anime.NumEpisodes,
        Status = anime.Status,
        Airing = string.Equals(anime.Status, "currently_airing", StringComparison.OrdinalIgnoreCase),
        Aired = FormatDates(anime.StartDate, anime.EndDate),
        Rating = anime.Rating,
        Score = anime.Mean,
        ScoredBy = anime.NumScoringUsers,
        Rank = anime.Rank,
        Popularity = anime.Popularity,
        Members = anime.NumListUsers,
        Synopsis = anime.Synopsis,
        Background = anime.Background,
        Season = anime.StartSeason?.Season,
        Year = anime.StartSeason?.Year,
        Studios = Names(anime.Studios),
        Genres = Names(anime.Genres)
    };

    public static List<CompatibleRelationGroup> MapRelations(MalAnimeNode anime) => anime.RelatedAnime
        .Where(x => x.Node is not null && !string.IsNullOrWhiteSpace(x.RelationType))
        .GroupBy(x => x.RelationType!, StringComparer.OrdinalIgnoreCase)
        .Select(x => new CompatibleRelationGroup
        {
            Relation = x.Key,
            Entry = x.Select(item => item.Node!).Select(item => new CompatibleRelationEntry
            {
                MalId = item.Id,
                Type = "anime",
                Name = item.Title,
                Url = Url(item.Id),
                ImageUrl = item.MainPicture?.Large ?? item.MainPicture?.Medium
            }).ToList()
        }).ToList();

    private static string Url(int id) => $"https://myanimelist.net/anime/{id}";
    private static List<string> Names(IEnumerable<MalNamedItem>? items) => items?.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList() ?? [];
    private static string? FormatDates(string? start, string? end) => start is null && end is null ? null : $"{start ?? "?"} to {end ?? "?"}";
}
