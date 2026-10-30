namespace ApiMyAnimes.Services;

/// <summary>
/// Regras puras do progresso de episodios vistos (sem banco).
/// </summary>
public static class AnimeWatchRules
{
    /// <summary>Quantidade maxima de numeros aceitos no corpo de uma requisicao (lista bruta, antes de remover repetidos).</summary>
    public const int MaxEpisodesPerRequest = 5000;

    /// <summary>Maior numero de episodio aceito quando o total do anime e desconhecido (e ao desmarcar).</summary>
    public const int MaxEpisodeNumber = 10000;

    /// <summary>
    /// Valida e normaliza os episodios a marcar: todos devem ser maiores ou iguais a 1 e, com total conhecido, menores ou iguais ao total;
    /// sem total, ate <see cref="MaxEpisodeNumber"/>. O resultado e distinto e ordenado.
    /// </summary>
    /// <param name="episodes">Numeros recebidos no corpo.</param>
    /// <param name="total">Total de episodios do anime, ou <c>null</c> se desconhecido.</param>
    /// <param name="normalized">Lista distinta e ordenada quando valida.</param>
    /// <param name="error">Mensagem de erro quando invalida.</param>
    public static bool TryNormalizeToMark(
        IReadOnlyCollection<int>? episodes,
        int? total,
        out IReadOnlyList<int> normalized,
        out string? error)
        => TryNormalize(episodes, total ?? MaxEpisodeNumber, out normalized, out error);

    /// <summary>
    /// Valida e normaliza os episodios a desmarcar: maiores ou iguais a 1 e ate <see cref="MaxEpisodeNumber"/>.
    /// O total nao limita a remocao, para que registros acima de um total reduzido depois possam ser removidos.
    /// </summary>
    public static bool TryNormalizeToUnmark(
        IReadOnlyCollection<int>? episodes,
        out IReadOnlyList<int> normalized,
        out string? error)
        => TryNormalize(episodes, MaxEpisodeNumber, out normalized, out error);

    private static bool TryNormalize(
        IReadOnlyCollection<int>? episodes,
        int upperBound,
        out IReadOnlyList<int> normalized,
        out string? error)
    {
        normalized = [];
        error = null;

        if (episodes is null)
        {
            error = "O corpo deve informar a lista episodes.";
            return false;
        }

        if (episodes.Count > MaxEpisodesPerRequest)
        {
            error = $"episodes aceita no maximo {MaxEpisodesPerRequest} itens.";
            return false;
        }

        foreach (var episode in episodes)
        {
            if (episode < 1 || episode > upperBound)
            {
                error = $"Cada episodio deve estar entre 1 e {upperBound}.";
                return false;
            }
        }

        normalized = episodes.Distinct().Order().ToArray();
        return true;
    }
}

/// <summary>Corpo das rotas <c>watch/episodes</c>.</summary>
public sealed class AnimeWatchEpisodesRequestDto
{
    /// <summary>Numeros dos episodios (a partir de 1).</summary>
    public int[]? Episodes { get; set; }
}

/// <summary>Progresso de um anime para o usuario autenticado.</summary>
/// <param name="MalId">Id do anime no MyAnimeList.</param>
/// <param name="Total">Total de episodios, ou <c>null</c> se desconhecido.</param>
/// <param name="Watched">Quantidade de episodios vistos (limitada ao total, quando conhecido).</param>
/// <param name="State">none, partial ou complete.</param>
/// <param name="Episodes">Numeros dos episodios vistos, em ordem crescente.</param>
public sealed record AnimeWatchDetailDto(
    int MalId,
    int? Total,
    int Watched,
    string State,
    IReadOnlyList<int> Episodes);

/// <summary>Resultado de uma operacao do <see cref="AnimeWatchService"/>.</summary>
public enum AnimeWatchOutcome
{
    /// <summary>Operacao concluida.</summary>
    Success,

    /// <summary>O anime nao existe no banco local.</summary>
    AnimeNotFound,

    /// <summary>A operacao exige o total de episodios, que e desconhecido.</summary>
    TotalUnknown,

    /// <summary>Episodios fora dos limites ou lista invalida.</summary>
    InvalidEpisodes
}

/// <summary>Resultado com o progresso atualizado ou o motivo da recusa.</summary>
/// <param name="Outcome">Desfecho da operacao.</param>
/// <param name="Watch">Progresso apos a operacao (somente em sucesso).</param>
/// <param name="Error">Mensagem para o chamador (somente em recusa de validacao).</param>
public sealed record AnimeWatchResult(AnimeWatchOutcome Outcome, AnimeWatchDetailDto? Watch = null, string? Error = null)
{
    internal static AnimeWatchResult Success(AnimeWatchDetailDto watch) => new(AnimeWatchOutcome.Success, watch);

    internal static readonly AnimeWatchResult NotFound = new(AnimeWatchOutcome.AnimeNotFound);
}
