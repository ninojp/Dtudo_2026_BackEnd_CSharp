namespace ApiCSharp.Data.Dtos;

public class ObterAnimeDto
{
    public int MalId { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public int Episodios { get; set; }

    public string MalUrl { get; set; } = string.Empty;

    public List<string> ImagensUrlMal { get; set; } = new();

    public List<string> SubTitulos { get; set; } = new();

    public DateTime HoraDaConsulta { get; set; } = DateTime.Now;
}
