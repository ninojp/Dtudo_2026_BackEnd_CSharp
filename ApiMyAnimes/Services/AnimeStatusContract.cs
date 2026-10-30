namespace ApiMyAnimes.Services;

/// <summary>
/// Estados compartilhados pelos indicadores de status do anime (olho e pasta) nos cards do site.
/// </summary>
public static class AnimeStatusStates
{
    /// <summary>Nada visto / pasta inexistente, nao encontrada ou nunca verificada.</summary>
    public const string None = "none";

    /// <summary>Parcialmente visto / pasta encontrada com episodios faltando ou total desconhecido.</summary>
    public const string Partial = "partial";

    /// <summary>Todos os episodios vistos / todos os videos encontrados na pasta.</summary>
    public const string Complete = "complete";
}

/// <summary>
/// Regras do contrato de status de anime que nao dependem de banco ou disco.
/// </summary>
public static class AnimeStatusRules
{
    /// <summary>Quantidade maxima de ids aceitos por requisicao em lote.</summary>
    public const int MaxMalIdsPerRequest = 200;

    /// <summary>
    /// Total de episodios: <c>Episodes</c> (MAL) se maior que 0; senao <c>Episodios</c> se maior que 1; senao desconhecido (<c>null</c>).
    /// </summary>
    /// <param name="episodes">Total informado pelo MyAnimeList.</param>
    /// <param name="episodios">Total local (padrao 1, que significa "nao informado").</param>
    public static int? ResolveTotalEpisodes(int? episodes, int episodios)
    {
        if (episodes is > 0) return episodes;
        return episodios > 1 ? episodios : null;
    }

    /// <summary>
    /// Estado do olho: <c>none</c> sem episodios vistos; <c>complete</c> se o total e conhecido e todos foram vistos; senao <c>partial</c>.
    /// </summary>
    /// <param name="watched">Episodios vistos (ja limitados ao total, quando conhecido).</param>
    /// <param name="total">Total de episodios, ou <c>null</c> se desconhecido.</param>
    public static string ResolveWatchState(int watched, int? total)
    {
        if (watched <= 0) return AnimeStatusStates.None;
        return total is > 0 && watched >= total ? AnimeStatusStates.Complete : AnimeStatusStates.Partial;
    }

    /// <summary>
    /// Pasta exibida no card a partir do ultimo resultado persistido. Sem verificacao registrada
    /// (<paramref name="checkedAtUtc"/> nulo) a pasta e <c>none</c> e nunca verificada; estado desconhecido vira <c>none</c>.
    /// O total esperado e o da verificacao (coerente com o estado); sem verificacao, o total atual do catalogo.
    /// </summary>
    /// <param name="storedState">Estado gravado, ou <c>null</c> sem registro.</param>
    /// <param name="storedVideos">Videos contados na verificacao.</param>
    /// <param name="storedExpected">Total esperado usado na verificacao.</param>
    /// <param name="checkedAtUtc">Momento da verificacao, ou <c>null</c> se nunca verificada.</param>
    /// <param name="currentTotal">Total atual do catalogo (usado quando nunca verificada).</param>
    public static AnimeFolderStatusDto ToFolderStatus(
        string? storedState, int storedVideos, int? storedExpected, DateTime? checkedAtUtc, int? currentTotal)
    {
        if (checkedAtUtc is null) return new AnimeFolderStatusDto(AnimeStatusStates.None, 0, currentTotal, null);
        var state = storedState is AnimeStatusStates.Partial or AnimeStatusStates.Complete ? storedState : AnimeStatusStates.None;
        return new AnimeFolderStatusDto(
            state,
            state == AnimeStatusStates.None ? 0 : Math.Max(0, storedVideos),
            storedExpected ?? currentTotal,
            DateTime.SpecifyKind(checkedAtUtc.Value, DateTimeKind.Utc));
    }

    /// <summary>
    /// Normaliza a lista recebida:
    /// </summary>
    /// <param name="malIds">Ids recebidos no corpo da requisicao.</param>
    public static IReadOnlyList<int> NormalizeMalIds(IEnumerable<int> malIds)
    {
        var seen = new HashSet<int>();
        var result = new List<int>();
        foreach (var malId in malIds)
        {
            if (malId > 0 && seen.Add(malId)) result.Add(malId);
        }

        return result;
    }
}

/// <summary>Corpo da requisicao do status em lote.</summary>
public sealed class AnimeStatusRequestDto
{
    /// <summary>Ids do MyAnimeList (maximo de <see cref="AnimeStatusRules.MaxMalIdsPerRequest"/>).</summary>
    public int[]? MalIds { get; set; }
}

/// <summary>Resposta do status em lote.</summary>
/// <param name="Items">Status por anime existente no banco local.</param>
public sealed record AnimeStatusResponseDto(IReadOnlyList<AnimeStatusItemDto> Items);

/// <summary>Status de um anime para o usuario autenticado.</summary>
/// <param name="MalId">Id do anime no MyAnimeList.</param>
/// <param name="Watch">Progresso de episodios vistos.</param>
/// <param name="Folder">Situacao da pasta no disco.</param>
/// <param name="Like">Reservado; sempre <c>null</c> ate a fase correspondente.</param>
public sealed record AnimeStatusItemDto(
    int MalId,
    AnimeWatchStatusDto Watch,
    AnimeFolderStatusDto Folder,
    string? Like);

/// <summary>Progresso de visualizacao.</summary>
/// <param name="State">none, partial ou complete.</param>
/// <param name="Watched">Episodios vistos pelo usuario.</param>
/// <param name="Total">Total de episodios, ou <c>null</c> se desconhecido.</param>
public sealed record AnimeWatchStatusDto(string State, int Watched, int? Total);

/// <summary>Situacao da pasta do anime.</summary>
/// <param name="State">none, partial ou complete.</param>
/// <param name="Videos">Videos encontrados na pasta.</param>
/// <param name="Expected">Episodios esperados, ou <c>null</c> se desconhecido.</param>
/// <param name="CheckedAtUtc">Ultima verificacao; <c>null</c> significa nunca verificado.</param>
public sealed record AnimeFolderStatusDto(string State, int Videos, int? Expected, DateTime? CheckedAtUtc);
