namespace ApiJikan.Dtos.External;

/// <summary>
/// Resposta bruta da Jikan para busca por nome.
/// </summary>
public class JikanAnimeSearchResponseDto
{
    public JikanPaginationDto? Pagination { get; set; }
    public List<JikanAnimeSearchItemDto>? Data { get; set; }
}

/// <summary>
/// Item bruto retornado pela Jikan na busca por nome.
/// </summary>
public class JikanAnimeSearchItemDto
{
    public int Mal_Id { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, JikanImageVariantDto>? Images { get; set; }
    public string? Title { get; set; }
    public string? Title_English { get; set; }
    public string? Title_Japanese { get; set; }
    public string? Type { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public double? Score { get; set; }
    public int? Year { get; set; }
    public JikanAiredDto? Aired { get; set; }
    public List<JikanNamedItemDto>? Genres { get; set; }
}
