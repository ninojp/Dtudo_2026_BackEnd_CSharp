using System.ComponentModel.DataAnnotations;

namespace LibDtudo.Shared.Dtos;

public class AdicionaMyAnimeDto
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Lista com o algum MalId é obrigatória.")]
    public List<int> AnimesMalId { get; set; } = new();
}
