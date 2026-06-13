using System.ComponentModel.DataAnnotations;

namespace ApiJikanCSharp.Models
{
    public class MyAnime
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public ICollection<Anime> Animes { get; set; } = new List<Anime>();
    }
}
