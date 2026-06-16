using System.ComponentModel.DataAnnotations;

namespace ApiCSharp.Shared.Dtos;

public class AdicionaAnimeDto
{
    [Required(ErrorMessage = "O MalId é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "O MalId deve ser um número positivo.")]
    public int MalId { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O número de episódios é obrigatório.")]
    [Range(1, 3000, ErrorMessage = "O número de episódios deve ser entre 1 e 3000.")]
    public int Episodios { get; set; } = 1;

    public string MalUrl { get; set; } = string.Empty;

    public List<string> ImagensUrlMal { get; set; } = new();

    public List<string> SubTitulos { get; set; } = new();
}
