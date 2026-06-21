using System.ComponentModel.DataAnnotations;

namespace ApiCSharp.Shared.Dtos;

public class AdicionaMyAnimeDto
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    public string Titulo { get; set; } 

    [Required(ErrorMessage = "A Lista com o algum MalId é obrigatória.")]
    public List<int> AnimesMalId { get; set; } = new();
}
