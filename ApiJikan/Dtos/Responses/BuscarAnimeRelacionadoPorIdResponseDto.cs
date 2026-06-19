namespace ApiJikan.Dtos.Responses;

/// <summary>
/// Grupo público de relações de um anime.
/// </summary>
public class AnimeRelationGroupDto
{
    public string? Relation { get; set; }
    public List<AnimeRelationEntryDto> Entry { get; set; } = new();
}

/// <summary>
/// Entrada pública de anime relacionado.
/// </summary>
public class AnimeRelationEntryDto
{
    public int MalId { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
}
