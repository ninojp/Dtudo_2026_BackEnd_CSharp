using System.ComponentModel.DataAnnotations;

namespace ApiCSharp.Data.Dtos;

public class AtualizaAnimeDto
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O número de episódios é obrigatório.")]
    [Range(1, 3000, ErrorMessage = "O número de episódios deve ser entre 1 e 3000.")]
    public int Episodios { get; set; } = 1;

    public string MalUrl { get; set; } = string.Empty;

    public List<string> ImagensUrlMal { get; set; } = new();

    public List<string> SubTitulos { get; set; } = new();
}
