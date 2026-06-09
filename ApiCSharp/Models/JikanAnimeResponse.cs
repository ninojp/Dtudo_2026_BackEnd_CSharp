namespace ApiCSharp.Models;

public class JikanAnimeResponse
{
    public Pagination? Pagination { get; set; }
    public List<AnimeData>? Data { get; set; }
}

public class Pagination
{
    public int Last_Visible_Page { get; set; }
    public bool Has_Next_Page { get; set; }
    public int Current_Page { get; set; }
    public Items? Items { get; set; }
}

public class Items
{
    public int Count { get; set; }
    public int Total { get; set; }
    public int Per_Page { get; set; }
}

public class AnimeData
{
    public int Mal_Id { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, ImageUrl>? Images { get; set; }
    public Trailer? Trailer { get; set; }
    public bool Approved { get; set; }
    public List<Title>? Titles { get; set; }
    public string? Title { get; set; }
    public string? Title_English { get; set; }
    public string? Title_Japanese { get; set; }
    public List<string>? Title_Synonyms { get; set; }
    public string? Type { get; set; }
    public string? Source { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public bool Airing { get; set; }
    public Aired? Aired { get; set; }
    public string? Duration { get; set; }
    public string? Rating { get; set; }
    public double? Score { get; set; }
    public int? Scored_By { get; set; }
    public int? Rank { get; set; }
    public int? Popularity { get; set; }
    public int? Members { get; set; }
    public int? Favorites { get; set; }
    public string? Synopsis { get; set; }
    public string? Background { get; set; }
    public string? Season { get; set; }
    public int? Year { get; set; }
    public List<MalItem>? Producers { get; set; }
    public List<MalItem>? Licensors { get; set; }
    public List<MalItem>? Studios { get; set; }
    public List<MalItem>? Genres { get; set; }
    public List<MalItem>? Explicit_Genres { get; set; }
    public List<MalItem>? Themes { get; set; }
    public List<MalItem>? Demographics { get; set; }
}

public class ImageUrl
{
    public string? Image_Url { get; set; }
    public string? Small_Image_Url { get; set; }
    public string? Large_Image_Url { get; set; }
}

public class Trailer
{
    public string? Youtube_Id { get; set; }
    public string? Url { get; set; }
    public string? Embed_Url { get; set; }
    public Dictionary<string, ImageUrl>? Images { get; set; }
}

public class Title
{
    public string? Type { get; set; }
    public string? TitleValue { get; set; }
}

public class Aired
{
    public string? From { get; set; }
    public string? To { get; set; }
    public Prop? Prop { get; set; }
    public string? String { get; set; }
}

public class Prop
{
    public DateInfo? From { get; set; }
    public DateInfo? To { get; set; }
}

public class DateInfo
{
    public int? Day { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}

public class MalItem
{
    public int Mal_Id { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
}
