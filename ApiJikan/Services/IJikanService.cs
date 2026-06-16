using ApiJikan.Models;

namespace ApiJikan.Services;

public interface IJikanService
{
    Task<AnimeSearchResponse> SearchAnimeAsync(string query, int page = 1);
    Task<AnimeData?> GetAnimeByIdAsync(int malId);
}
