using System.ComponentModel.DataAnnotations;

namespace LibDtudo.Shared.Models;

/// <summary>
/// Representa uma coleção de animes, onde o título é nome do anime principal. E uma lista de IDs de animes (MalId) RELACIONADOS ao título principal.
/// </summary>
public class MyAnime
{
    [Key]
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Lista com o algum MalId é obrigatória.")]
    public List<int> AnimesMalId { get; set; } = new();

}
