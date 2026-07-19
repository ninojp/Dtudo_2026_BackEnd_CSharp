namespace ApiMyAnimeList.Dtos;

public sealed class CompatibleSearchResponse
{
    public List<CompatibleSearchItem> Results { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public int TotalResults { get; set; }
}

public sealed class CompatibleSearchItem
{
    public int MalId { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? TitleEnglish { get; set; }
    public string? TitleJapanese { get; set; }
    public List<string> TitleSynonyms { get; set; } = [];
    public string? ImageUrl { get; set; }
    public string? Type { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public double? Score { get; set; }
    public int? Year { get; set; }
    public List<string> Genres { get; set; } = [];
}

public sealed class CompatibleDetails
{
    public int MalId { get; set; }
    public string? Url { get; set; }
    public CompatibleImages? Images { get; set; }
    public string? Trailer { get; set; }
    public int MyAnimeID { get; set; }
    public bool Approved { get; set; }
    public string? Title { get; set; }
    public string? TitleEnglish { get; set; }
    public string? TitleJapanese { get; set; }
    public List<string> TitleSynonyms { get; set; } = [];
    public string? Type { get; set; }
    public string? Source { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public bool Airing { get; set; }
    public string? Aired { get; set; }
    public string? Duration { get; set; }
    public string? Rating { get; set; }
    public double? Score { get; set; }
    public int? ScoredBy { get; set; }
    public int? Rank { get; set; }
    public int? Popularity { get; set; }
    public int? Members { get; set; }
    public int? Favorites { get; set; }
    public string? Synopsis { get; set; }
    public string? Background { get; set; }
    public string? Season { get; set; }
    public int? Year { get; set; }
    public List<string> Producers { get; set; } = [];
    public List<string> Licensors { get; set; } = [];
    public List<string> Studios { get; set; } = [];
    public List<string> Genres { get; set; } = [];
    public List<string> ExplicitGenres { get; set; } = [];
    public List<string> Themes { get; set; } = [];
    public List<string> Demographics { get; set; } = [];
}

public sealed class CompatibleImages
{
    public CompatibleImageVariant? Jpg { get; set; }
}

public sealed class CompatibleImageVariant
{
    public string? ImageUrl { get; set; }
    public string? SmallImageUrl { get; set; }
    public string? LargeImageUrl { get; set; }
}

public sealed class CompatibleRelationGroup
{
    public string? Relation { get; set; }
    public List<CompatibleRelationEntry> Entry { get; set; } = [];
}

public sealed class CompatibleRelationEntry
{
    public int MalId { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
}
