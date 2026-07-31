namespace LibDtudo.Shared.Dtos;

/// <summary>
/// Identifica um anime local que possui um título equivalente ao anime consultado.
/// </summary>
public sealed class ConflitoTituloAnimeDto
{
    public int MalId { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string TituloEmConflito { get; init; } = string.Empty;
}
