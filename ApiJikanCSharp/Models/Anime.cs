using System.ComponentModel.DataAnnotations;

namespace ApiJikanCSharp.Models;

public class Anime
{
    [Key]
    [Required]
    public int MalId { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    public string Title { get; set; } = "Titulo";
    public string MalUrl { get; set; } = "URL";
    public int Episodios { get; set; }
    public List<string> Imagens { get; set; } = new List<string>();
    public List<string> Titles { get; set; } = new List<string>();

}
