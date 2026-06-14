using System.ComponentModel.DataAnnotations;

namespace ApiCSharp.Models;

public class Anime
{
    //Único, corresponde ao ID do MyAnimeList
    [Key]
    [Required]
    public int MalId { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    public string Titulo { get; set; } = "Titulo";

    [Required(ErrorMessage = "O número de episódios é obrigatório.")]
    [Range(1, 3000, ErrorMessage = "O número de episódios deve ser entre 1 e 3000.")]
    //não pode ser negativo, e tem que ser um número razoável para um anime
    [RegularExpression(@"^\d+$", ErrorMessage = "O número de episódios deve ser um número inteiro positivo (entre 1 e 3000).")]
    public int Episodios { get; set; } = 1;

    public string MalUrl { get; set; } = "URL";

    public List<string> ImagensUrlMal { get; set; } = new();

    public List<string> SubTitulos { get; set; } = new();

}
