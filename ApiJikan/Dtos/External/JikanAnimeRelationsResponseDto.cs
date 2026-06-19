namespace ApiJikan.Dtos.External;

/// <summary>
/// Resposta bruta da Jikan para o endpoint de relações por ID.
/// </summary>
public class JikanAnimeRelationsResponseDto
{
    public List<JikanAnimeRelationGroupDto>? Data { get; set; }
}

/// <summary>
/// Grupo de relações retornado pela Jikan.
/// </summary>
public class JikanAnimeRelationGroupDto
{
    public string? Relation { get; set; }
    public List<JikanRelationEntryDto>? Entry { get; set; }
}
