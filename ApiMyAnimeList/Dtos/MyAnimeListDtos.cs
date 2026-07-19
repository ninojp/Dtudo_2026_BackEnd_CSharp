using System.Text.Json.Serialization;

namespace ApiMyAnimeList.Dtos;

public sealed class MalPagedResponse<T>
{
    [JsonPropertyName("data")]
    public List<MalListItem<T>> Data { get; set; } = [];

    [JsonPropertyName("paging")]
    public MalPaging? Paging { get; set; }
}

public sealed class MalListItem<T>
{
    [JsonPropertyName("node")]
    public T? Node { get; set; }
}

public sealed class MalPaging
{
    [JsonPropertyName("previous")]
    public string? Previous { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

public sealed class MalAnimeNode
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("main_picture")] public MalPicture? MainPicture { get; set; }
    [JsonPropertyName("alternative_titles")] public MalAlternativeTitles? AlternativeTitles { get; set; }
    [JsonPropertyName("start_date")] public string? StartDate { get; set; }
    [JsonPropertyName("end_date")] public string? EndDate { get; set; }
    [JsonPropertyName("synopsis")] public string? Synopsis { get; set; }
    [JsonPropertyName("mean")] public double? Mean { get; set; }
    [JsonPropertyName("rank")] public int? Rank { get; set; }
    [JsonPropertyName("popularity")] public int? Popularity { get; set; }
    [JsonPropertyName("num_list_users")] public int? NumListUsers { get; set; }
    [JsonPropertyName("num_scoring_users")] public int? NumScoringUsers { get; set; }
    [JsonPropertyName("num_episodes")] public int? NumEpisodes { get; set; }
    [JsonPropertyName("average_episode_duration")] public int? AverageEpisodeDuration { get; set; }
    [JsonPropertyName("media_type")] public string? MediaType { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("genres")] public List<MalNamedItem> Genres { get; set; } = [];
    [JsonPropertyName("start_season")] public MalSeason? StartSeason { get; set; }
    [JsonPropertyName("broadcast")] public MalBroadcast? Broadcast { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }
    [JsonPropertyName("rating")] public string? Rating { get; set; }
    [JsonPropertyName("background")] public string? Background { get; set; }
    [JsonPropertyName("studios")] public List<MalNamedItem> Studios { get; set; } = [];
    [JsonPropertyName("related_anime")] public List<MalRelatedAnime> RelatedAnime { get; set; } = [];
}

public sealed class MalRelatedAnime
{
    [JsonPropertyName("node")] public MalAnimeNode? Node { get; set; }
    [JsonPropertyName("relation_type")] public string? RelationType { get; set; }
}

public sealed class MalPicture
{
    [JsonPropertyName("medium")] public string? Medium { get; set; }
    [JsonPropertyName("large")] public string? Large { get; set; }
}

public sealed class MalAlternativeTitles
{
    [JsonPropertyName("en")] public string? English { get; set; }
    [JsonPropertyName("ja")] public string? Japanese { get; set; }
    [JsonPropertyName("synonyms")] public List<string> Synonyms { get; set; } = [];
}

public sealed class MalNamedItem
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public sealed class MalSeason
{
    [JsonPropertyName("year")] public int? Year { get; set; }
    [JsonPropertyName("season")] public string? Season { get; set; }
}

public sealed class MalBroadcast
{
    [JsonPropertyName("day_of_the_week")] public string? DayOfTheWeek { get; set; }
    [JsonPropertyName("start_time")] public string? StartTime { get; set; }
}
