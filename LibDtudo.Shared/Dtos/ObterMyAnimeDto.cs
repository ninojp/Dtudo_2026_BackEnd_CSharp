using System.ComponentModel.DataAnnotations;

namespace LibDtudo.Shared.Dtos;
/// <summary>
/// DTO para obter informações de um anime específico, incluindo o título, uma lista de IDs de animes relacionados (MalId) e a hora da consulta.
/// </summary>
public class ObterMyAnimeDto
{
    public string Titulo { get; set; } = string.Empty;

    public List<int> AnimesMalId { get; set; } = new();

    public DateTime HoraDaConsulta { get; set; } = DateTime.Now;
}
