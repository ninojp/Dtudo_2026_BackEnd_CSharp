namespace ApiJikan.Dtos.Responses;

/// <summary>
/// Resposta pública do endpoint de busca por nome.
/// </summary>
public class BuscarAnimePorNomeResponseDto
{
    public List<AnimeBuscaResumoDto> Results { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public int TotalResults { get; set; }
}

/// <summary>
/// Resumo público de anime retornado na listagem de busca.
/// </summary>
public class AnimeBuscaResumoDto
{
    public int MalId { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? TitleEnglish { get; set; }
    public string? TitleJapanese { get; set; }
    public string? ImageUrl { get; set; }
    public string? Type { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public double? Score { get; set; }
    public int? Year { get; set; }
    public List<string> Genres { get; set; } = new();
}
