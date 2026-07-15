using LibDtudo.Shared.Dtos;

namespace WinAppDtudo.Services;

public class ApiMyColecoesBuscaResult
{
    public List<ObterMyAnimeDto> Results { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public int TotalResults { get; set; }
}

public class ApiAnimesBuscaResult
{
    public List<ObterAnimeDto> Results { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public int TotalResults { get; set; }
}
