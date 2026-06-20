namespace ApiJikan.Dtos.External;

/// <summary>
/// Envelope bruto retornado pela Jikan na busca de anime por ID.
/// </summary>
public class JikanAnimeByIdResponseDto
{
    public JikanAnimeDetailsDto? Data { get; set; }
}

/// <summary>
/// Detalhes brutos do anime retornados pela Jikan.
/// </summary>
public class JikanAnimeDetailsDto
{
    public int Mal_Id { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, JikanImageVariantDto>? Images { get; set; }

    public JikanTrailerDto? Trailer { get; set; }
    //public string? Trailer { get; set; }
    public bool Approved { get; set; }
    public string? Title { get; set; }
    public string? Title_English { get; set; }
    public string? Title_Japanese { get; set; }
    public List<string>? Title_Synonyms { get; set; }
    public string? Type { get; set; }
    public string? Source { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public bool Airing { get; set; }
    public JikanAiredDto? Aired { get; set; }
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
    public List<JikanNamedItemDto>? Producers { get; set; }
    public List<JikanNamedItemDto>? Licensors { get; set; }
    public List<JikanNamedItemDto>? Studios { get; set; }
    public List<JikanNamedItemDto>? Genres { get; set; }
    public List<JikanNamedItemDto>? Explicit_Genres { get; set; }
    public List<JikanNamedItemDto>? Themes { get; set; }
    public List<JikanNamedItemDto>? Demographics { get; set; }
}
