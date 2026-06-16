using System.ComponentModel.DataAnnotations;

namespace ApiCSharp.Data.Dtos;

public class ObterMyAnimeDto
{
    public string Titulo { get; set; } = string.Empty;

    public List<int> AnimesMalId { get; set; } = new();

    public DateTime HoraDaConsulta { get; set; } = DateTime.Now;
}
