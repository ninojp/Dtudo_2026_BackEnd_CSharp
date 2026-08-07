using System.ComponentModel.DataAnnotations;

namespace LibDtudo.Shared.Dtos;

public sealed class EnsureMyAnimeCollectionRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Titulo { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<int> AnimesMalId { get; init; } = [];
}

public sealed class EnsureMyAnimeCollectionResponse
{
    public int Id { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public List<int> AnimesMalId { get; init; } = [];
    public bool Created { get; init; }
    public bool Changed { get; init; }
}

public sealed class EnsureAnimeAssociationRequest
{
    [Range(1, int.MaxValue)]
    public int MyAnimeId { get; init; }
}

public sealed class EnsureAnimeAssociationResponse
{
    public int MalId { get; init; }
    public int MyAnimeId { get; init; }
    public bool Changed { get; init; }
}
