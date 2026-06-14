namespace ApiCSharp.Models;

public class JikanAnimeSearchResult
{
    public int MalId { get; set; }
    public string? Title { get; set; }
    public string? TitleEnglish { get; set; }
    public string? TitleJapanese { get; set; }
    public string? ImageUrl { get; set; }
    public string? Type { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public double? Score { get; set; }
    public int? Year { get; set; }
    public string? Synopsis { get; set; }
    public List<string>? Genres { get; set; }
    public string? Url { get; set; }
}

public class AnimeSearchResponse
{
    public List<JikanAnimeSearchResult> Results { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public int TotalResults { get; set; }
}
