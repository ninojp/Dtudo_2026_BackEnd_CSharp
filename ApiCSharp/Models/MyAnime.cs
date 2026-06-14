using System.ComponentModel.DataAnnotations;

namespace ApiCSharp.Models;

public class MyAnime
{
    [Key]
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Lista com o algum MalId é obrigatória.")]
    public List<int> AnimesMalId { get; set; } = new();

}
