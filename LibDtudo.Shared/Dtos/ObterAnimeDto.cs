namespace LibDtudo.Shared.Dtos;
/// <summary>
/// DTO para obter informações detalhadas de um anime, incluindo dados do MyAnime e informações adicionais da MyAnimeList API.
/// </summary>
public class ObterAnimeDto
{
    public int MalId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public int Episodios { get; set; }

    public string MalUrl { get; set; } = string.Empty;

    public List<string> ImagensUrlMal { get; set; } = new();

    public List<string> SubTitulos { get; set; } = new();
    public List<int> AnimesRelacionadosIds { get; set; } = new();
    public int MyAnimeID { get; set; }

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

    public DateTime HoraDaConsulta { get; set; } = DateTime.Now;
}
