namespace ApiJikan.Dtos.Responses;

/// <summary>
/// Resposta pública do endpoint de detalhes de anime por ID.
/// </summary>
public class BuscarAnimePorIdResponseDto
{
    public int MalId { get; set; }
    public string? Url { get; set; }
    public AnimeImagesDto? Images { get; set; }
    public AnimeTrailerDto? Trailer { get; set; }
    public bool Approved { get; set; }
    public string? Title { get; set; }
    public string? TitleEnglish { get; set; }
    public string? TitleJapanese { get; set; }
    public List<string> TitleSynonyms { get; set; } = new();
    public string? Type { get; set; }
    public string? Source { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public bool Airing { get; set; }
    public AnimeAiredDto? Aired { get; set; }
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
    public List<AnimeNamedItemDto> Producers { get; set; } = new();
    public List<AnimeNamedItemDto> Licensors { get; set; } = new();
    public List<AnimeNamedItemDto> Studios { get; set; } = new();
    public List<AnimeNamedItemDto> Genres { get; set; } = new();
    public List<AnimeNamedItemDto> ExplicitGenres { get; set; } = new();
    public List<AnimeNamedItemDto> Themes { get; set; } = new();
    public List<AnimeNamedItemDto> Demographics { get; set; } = new();
}

/// <summary>
/// Coleção pública de imagens do anime.
/// </summary>
public class AnimeImagesDto
{
    public AnimeImageVariantDto? Jpg { get; set; }
}

/// <summary>
/// Variante pública de imagem do anime.
/// </summary>
public class AnimeImageVariantDto
{
    public string? ImageUrl { get; set; }
    public string? SmallImageUrl { get; set; }
    public string? LargeImageUrl { get; set; }
}

/// <summary>
/// Trailer público do anime.
/// </summary>
public class AnimeTrailerDto
{
    public string? YoutubeId { get; set; }
    public string? Url { get; set; }
    public string? EmbedUrl { get; set; }
    public AnimeImagesDto? Images { get; set; }
}

/// <summary>
/// Período público de exibição do anime.
/// </summary>
public class AnimeAiredDto
{
    public string? From { get; set; }
    public string? To { get; set; }
    public AnimePropDto? Prop { get; set; }
    public string? String { get; set; }
}

/// <summary>
/// Subpropriedades públicas de datas do anime.
/// </summary>
public class AnimePropDto
{
    public AnimeDateInfoDto? From { get; set; }
    public AnimeDateInfoDto? To { get; set; }
}

/// <summary>
/// Parte pública de data do anime.
/// </summary>
public class AnimeDateInfoDto
{
    public int? Day { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}

/// <summary>
/// Item público nomeado de apoio para gêneros, estúdios, produtores e similares.
/// </summary>
public class AnimeNamedItemDto
{
    public int MalId { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
}
