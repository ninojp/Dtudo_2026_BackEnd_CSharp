using System.ComponentModel.DataAnnotations;

namespace LibDtudo.Shared.Dtos;

public sealed class EnsureMyAnimeCollectionRequest
{
    [Required]
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
    public List<int> AnimesIgnoradosPorOutraColecao { get; init; } = [];
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
    public int? MyAnimeIdAtual { get; init; }
    public bool IgnoradaPorOutraColecao { get; init; }
    public bool Changed { get; init; }
}

public sealed class RepararAssociacoesOrfasRequest
{
    [Required]
    [MinLength(1)]
    public List<int> MyAnimeIds { get; init; } = [];

    public bool Aplicar { get; init; }
}

public sealed class RepararAssociacoesOrfasResponse
{
    public bool Simulacao { get; init; }
    public List<int> MyAnimeIdsSolicitados { get; init; } = [];
    public List<int> MyAnimeIdsSemColecao { get; init; } = [];
    public List<int> MyAnimeIdsComColecaoPreservados { get; init; } = [];
    public List<int> MalIdsEncontrados { get; init; } = [];
    public List<int> MalIdsCorrigidos { get; init; } = [];
}
