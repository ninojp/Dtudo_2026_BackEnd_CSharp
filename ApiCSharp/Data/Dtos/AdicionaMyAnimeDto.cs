using System.ComponentModel.DataAnnotations;

namespace ApiCSharp.Data.Dtos;

public class AdicionaMyAnimeDto
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Lista com o algum MalId é obrigatória.")]
    public List<int> AnimesMalId { get; set; } = new();
}
