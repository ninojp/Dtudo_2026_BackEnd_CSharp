using System.ComponentModel.DataAnnotations;

namespace LibDtudo.Shared.Models;
/// <summary>
/// Representa um anime com suas propriedades e informações relacionadas.
/// </summary>
public class Anime
{
    // Único, corresponde ao ID do MyAnimeList
    [Key]
    [Required]
    public int MalId { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    public string Titulo { get; set; } = "Titulo";

    [Required(ErrorMessage = "O número de episódios é obrigatório.")]
    [Range(1, 3000, ErrorMessage = "O número de episódios deve ser entre 1 e 3000.")]
    public int Episodios { get; set; } = 1;

    public string MalUrl { get; set; } = "URL";

    public List<string> ImagensUrlMal { get; set; } = new();

    public List<string> SubTitulos { get; set; } = new();
    public int MyAnimeID { get; set; }
    public List<int> AnimesRelacionadosIds { get; set; } = new();
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
