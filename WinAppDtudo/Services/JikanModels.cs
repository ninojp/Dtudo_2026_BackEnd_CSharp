namespace WinAppDtudo.Services;

/// <summary>Resultado paginado de busca de animes na API Jikan local.</summary>
public class JikanBuscaResult
{
    public List<JikanAnimeCard> Results { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public int TotalResults { get; set; }
}

/// <summary>Resumo de anime retornado na busca por nome.</summary>
public class JikanAnimeCard
{
    public int MalId { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? TitleEnglish { get; set; }
    public string? TitleJapanese { get; set; }
    public List<string> TitleSynonyms { get; set; } = new();
    public string? ImageUrl { get; set; }
    public string? Type { get; set; }
    public int? Episodes { get; set; }
    public string? Status { get; set; }
    public double? Score { get; set; }
    public int? Year { get; set; }
    public List<string> Genres { get; set; } = new();
}

/// <summary>Detalhes completos de anime retornado por ID.</summary>
public class JikanAnimeDetalhes
{
    public int MalId { get; set; }
    public string? Url { get; set; }
    public JikanAnimeImages? Images { get; set; }
    public string? Trailer { get; set; }
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
    public List<string> Producers { get; set; } = new();
    public List<string> Licensors { get; set; } = new();
    public List<string> Studios { get; set; } = new();
    public List<string> Genres { get; set; } = new();
    public List<string> ExplicitGenres { get; set; } = new();
    public List<string> Themes { get; set; } = new();
    public List<string> Demographics { get; set; } = new();
}

/// <summary>Coleção de imagens do anime.</summary>
public class JikanAnimeImages
{
    public JikanImageVariant? Jpg { get; set; }
}

/// <summary>Variante de imagem do anime.</summary>
public class JikanImageVariant
{
    public string? ImageUrl { get; set; }
    public string? SmallImageUrl { get; set; }
    public string? LargeImageUrl { get; set; }
}

/// <summary>Grupo de relações de anime (ex: Prequel, Sequel, Side Story).</summary>
public class JikanAnimeRelacaoGroup
{
    public string? Relation { get; set; }
    public List<JikanRelacaoEntry> Entry { get; set; } = [];
}

/// <summary>Entrada de um anime relacionado.</summary>
public class JikanRelacaoEntry
{
    public int MalId { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
}
