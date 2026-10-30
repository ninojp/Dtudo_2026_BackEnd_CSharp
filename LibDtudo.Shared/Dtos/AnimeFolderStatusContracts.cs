namespace LibDtudo.Shared.Dtos;

/// <summary>Situacoes de uma execucao de atualizacao do status de pasta.</summary>
public static class AnimeFolderRefreshStatuses
{
    public const string Running = "Running";
    public const string Completed = "Completed";

    /// <summary>Terminou, mas algum anime falhou ou ficou indisponivel (disco offline, listagem truncada).</summary>
    public const string CompletedWithErrors = "CompletedWithErrors";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";

    /// <summary>A execucao constava como em andamento, mas o processo que a executava nao existe mais.</summary>
    public const string Interrupted = "Interrupted";

    public static bool IsFinal(string? status) =>
        !string.Equals(status, Running, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Resultado de um anime dentro de uma execucao.</summary>
public static class AnimeFolderRefreshOutcomes
{
    /// <summary>Pasta encontrada e contada; status gravado.</summary>
    public const string Updated = "Updated";

    /// <summary>Colecao lida por inteiro e nenhuma pasta corresponde ao anime; status <c>none</c> gravado.</summary>
    public const string NotFound = "NotFound";

    /// <summary>A colecao nao tem pasta vinculada; status <c>none</c> gravado.</summary>
    public const string NoLocation = "NoLocation";

    /// <summary>Disco/pasta inacessivel ou listagem truncada: o status anterior foi preservado.</summary>
    public const string Unavailable = "Unavailable";

    /// <summary>Falha ao consultar a ApiFileStorage: o status anterior foi preservado.</summary>
    public const string Failed = "Failed";

    /// <summary>O status anterior foi preservado (nada novo foi gravado para o anime).</summary>
    public static bool PreservesPreviousStatus(string? outcome) =>
        string.Equals(outcome, Unavailable, StringComparison.OrdinalIgnoreCase)
        || string.Equals(outcome, Failed, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Execucao de atualizacao do status de pasta (progresso consultavel). Nunca contem caminho fisico.</summary>
/// <param name="Id">Identificador da execucao.</param>
/// <param name="Status">Ver <see cref="AnimeFolderRefreshStatuses"/>.</param>
/// <param name="StartedAtUtc">Inicio da execucao.</param>
/// <param name="FinishedAtUtc">Fim da execucao; <c>null</c> enquanto em andamento.</param>
/// <param name="MyAnimeId">Colecao atualizada; <c>null</c> quando a execucao cobre todas.</param>
/// <param name="Total">Animes planejados.</param>
/// <param name="Processed">Animes ja resolvidos (soma dos demais contadores).</param>
/// <param name="Updated">Pasta encontrada e status gravado.</param>
/// <param name="NotFound">Sem pasta (colecao sem vinculo ou nenhuma subpasta corresponde); status <c>none</c> gravado.</param>
/// <param name="Unavailable">Pasta/disco indisponivel ou listagem truncada; status anterior preservado.</param>
/// <param name="Failed">Falha ao consultar a ApiFileStorage; status anterior preservado.</param>
/// <param name="Error">Motivo da falha da execucao inteira, quando houver.</param>
/// <param name="Items">Relatorio por anime (paginado por <c>afterId</c>); <c>null</c> quando nao solicitado.</param>
public sealed record AnimeFolderRefreshRunDto(
    Guid Id,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    int? MyAnimeId,
    int Total,
    int Processed,
    int Updated,
    int NotFound,
    int Unavailable,
    int Failed,
    string? Error,
    IReadOnlyList<AnimeFolderRefreshItemDto>? Items);

/// <summary>Resultado de um anime na execucao.</summary>
/// <param name="Id">Cursor de paginacao (use como <c>afterId</c>).</param>
/// <param name="MalId">Id do anime no MyAnimeList.</param>
/// <param name="MyAnimeId">Colecao MyAnime do anime.</param>
/// <param name="Outcome">Ver <see cref="AnimeFolderRefreshOutcomes"/>.</param>
/// <param name="State">Estado gravado (<c>none</c>/<c>partial</c>/<c>complete</c>); <c>null</c> quando preservado.</param>
/// <param name="Videos">Videos contados; <c>null</c> quando nao houve contagem.</param>
/// <param name="Detail">Explicacao curta, sem caminho fisico.</param>
/// <param name="OccurredAtUtc">Momento em que o anime foi resolvido.</param>
public sealed record AnimeFolderRefreshItemDto(
    long Id,
    int MalId,
    int MyAnimeId,
    string Outcome,
    string? State,
    int? Videos,
    string Detail,
    DateTimeOffset OccurredAtUtc);

/// <summary>Corpo da requisicao do status em lote (<c>POST apiLocal/Anime/status</c>).</summary>
public sealed class AnimeStatusRequestDto
{
    /// <summary>Quantidade maxima de ids aceitos por requisicao.</summary>
    public const int MaxMalIdsPerRequest = 200;

    /// <summary>Ids do MyAnimeList (maximo de <see cref="MaxMalIdsPerRequest"/>).</summary>
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
