namespace LibDtudo.Shared.Dtos.MyAnimeList;

/// <summary>
/// Resultado paginado de busca de animes na API local ApiMyAnimeList.
/// </summary>
public sealed class AnimeSearchResult
{
    /// <summary>Animes encontrados na pagina atual.</summary>
    public List<AnimeSearchCard> Results { get; set; } = [];

    /// <summary>Pagina atual, iniciando em 1.</summary>
    public int CurrentPage { get; set; }

    /// <summary>Total estimado de paginas disponiveis.</summary>
    public int TotalPages { get; set; }

    /// <summary>Indica se existe proxima pagina.</summary>
    public bool HasNextPage { get; set; }

    /// <summary>Total estimado de resultados.</summary>
    public int TotalResults { get; set; }
}

/// <summary>
/// Resumo de anime retornado na busca por nome.
/// </summary>
public sealed class AnimeSearchCard
{
    /// <summary>ID do anime no MyAnimeList.</summary>
    public int MalId { get; set; }

    /// <summary>URL publica do anime no MyAnimeList.</summary>
    public string? Url { get; set; }

    /// <summary>Titulo principal.</summary>
    public string? Title { get; set; }

    /// <summary>Titulo em ingles, quando disponivel.</summary>
    public string? TitleEnglish { get; set; }

    /// <summary>Titulo japones, quando disponivel.</summary>
    public string? TitleJapanese { get; set; }

    /// <summary>Titulos alternativos.</summary>
    public List<string> TitleSynonyms { get; set; } = [];

    /// <summary>URL da imagem principal.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Tipo da obra.</summary>
    public string? Type { get; set; }

    /// <summary>Quantidade de episodios.</summary>
    public int? Episodes { get; set; }

    /// <summary>Status de exibicao.</summary>
    public string? Status { get; set; }

    /// <summary>Pontuacao media.</summary>
    public double? Score { get; set; }

    /// <summary>Ano de lancamento.</summary>
    public int? Year { get; set; }

    /// <summary>Generos associados.</summary>
    public List<string> Genres { get; set; } = [];
}

/// <summary>
/// Detalhes completos de anime retornado por ID.
/// </summary>
public sealed class AnimeDetails
{
    /// <summary>ID do anime no MyAnimeList.</summary>
    public int MalId { get; set; }
    /// <summary>URL publica do anime no MyAnimeList.</summary>
    public string? Url { get; set; }
    /// <summary>Colecao de imagens.</summary>
    public AnimeImages? Images { get; set; }
    /// <summary>URL de trailer.</summary>
    public string? Trailer { get; set; }
    /// <summary>ID local de MyAnime, quando relacionado.</summary>
    public int MyAnimeID { get; set; }
    /// <summary>Indica se o registro foi aprovado pela fonte externa.</summary>
    public bool Approved { get; set; }
    /// <summary>Titulo principal.</summary>
    public string? Title { get; set; }
    /// <summary>Titulo em ingles.</summary>
    public string? TitleEnglish { get; set; }
    /// <summary>Titulo japones.</summary>
    public string? TitleJapanese { get; set; }
    /// <summary>Titulos alternativos.</summary>
    public List<string> TitleSynonyms { get; set; } = [];
    /// <summary>Tipo da obra.</summary>
    public string? Type { get; set; }
    /// <summary>Fonte/origem da obra.</summary>
    public string? Source { get; set; }
    /// <summary>Quantidade de episodios.</summary>
    public int? Episodes { get; set; }
    /// <summary>Status de exibicao.</summary>
    public string? Status { get; set; }
    /// <summary>Indica se esta em exibicao.</summary>
    public bool Airing { get; set; }
    /// <summary>Periodo de exibicao formatado.</summary>
    public string? Aired { get; set; }
    /// <summary>Duracao formatada.</summary>
    public string? Duration { get; set; }
    /// <summary>Classificacao indicativa.</summary>
    public string? Rating { get; set; }
    /// <summary>Pontuacao media.</summary>
    public double? Score { get; set; }
    /// <summary>Quantidade de usuarios que pontuaram.</summary>
    public int? ScoredBy { get; set; }
    /// <summary>Ranking na fonte externa.</summary>
    public int? Rank { get; set; }
    /// <summary>Popularidade na fonte externa.</summary>
    public int? Popularity { get; set; }
    /// <summary>Quantidade de membros/listas.</summary>
    public int? Members { get; set; }
    /// <summary>Quantidade de favoritos.</summary>
    public int? Favorites { get; set; }
    /// <summary>Sinopse.</summary>
    public string? Synopsis { get; set; }
    /// <summary>Contexto adicional.</summary>
    public string? Background { get; set; }
    /// <summary>Temporada.</summary>
    public string? Season { get; set; }
    /// <summary>Ano de lancamento.</summary>
    public int? Year { get; set; }
    /// <summary>Produtoras.</summary>
    public List<string> Producers { get; set; } = [];
    /// <summary>Licenciadores.</summary>
    public List<string> Licensors { get; set; } = [];
    /// <summary>Estudios.</summary>
    public List<string> Studios { get; set; } = [];
    /// <summary>Generos.</summary>
    public List<string> Genres { get; set; } = [];
    /// <summary>Generos explicitos.</summary>
    public List<string> ExplicitGenres { get; set; } = [];
    /// <summary>Temas.</summary>
    public List<string> Themes { get; set; } = [];
    /// <summary>Demografia/publico-alvo.</summary>
    public List<string> Demographics { get; set; } = [];
}

/// <summary>Colecao de imagens do anime.</summary>
public sealed class AnimeImages
{
    /// <summary>Variante JPG.</summary>
    public AnimeImageVariant? Jpg { get; set; }
}

/// <summary>Variante de imagem do anime.</summary>
public sealed class AnimeImageVariant
{
    /// <summary>URL padrao.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>URL pequena.</summary>
    public string? SmallImageUrl { get; set; }
    /// <summary>URL grande.</summary>
    public string? LargeImageUrl { get; set; }
}

/// <summary>Grupo de relacoes de anime.</summary>
public sealed class AnimeRelationGroup
{
    /// <summary>Tipo de relacao.</summary>
    public string? Relation { get; set; }
    /// <summary>Animes relacionados neste grupo.</summary>
    public List<AnimeRelationEntry> Entry { get; set; } = [];
}

/// <summary>Entrada de um anime relacionado.</summary>
public sealed class AnimeRelationEntry
{
    /// <summary>ID do anime no MyAnimeList.</summary>
    public int MalId { get; set; }
    /// <summary>Tipo da entrada.</summary>
    public string? Type { get; set; }
    /// <summary>Nome/titulo da entrada.</summary>
    public string? Name { get; set; }
    /// <summary>URL publica.</summary>
    public string? Url { get; set; }
    /// <summary>URL da imagem.</summary>
    public string? ImageUrl { get; set; }
}
