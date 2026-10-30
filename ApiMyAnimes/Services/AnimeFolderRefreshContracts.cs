using ApiMyAnimes.Data;

namespace ApiMyAnimes.Services;

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

    public static bool IsFinal(string status) => status != Running;
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
}

/// <summary>Execucao de atualizacao do status de pasta (progresso consultavel). Nunca contem caminho fisico.</summary>
/// <param name="Id">Identificador da execucao.</param>
/// <param name="Status">Ver <see cref="AnimeFolderRefreshStatuses"/>.</param>
/// <param name="MyAnimeId">Colecao atualizada; <c>null</c> quando a execucao cobre todas.</param>
/// <param name="Total">Animes planejados.</param>
/// <param name="Processed">Animes ja resolvidos (soma dos demais contadores).</param>
/// <param name="Updated">Pasta encontrada e status gravado.</param>
/// <param name="NotFound">Sem pasta (colecao sem vinculo ou nenhuma subpasta corresponde); status <c>none</c> gravado.</param>
/// <param name="Unavailable">Pasta/disco indisponivel ou listagem truncada; status anterior preservado.</param>
/// <param name="Failed">Falha ao consultar a ApiFileStorage; status anterior preservado.</param>
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
    IReadOnlyList<AnimeFolderRefreshItemDto>? Items)
{
    public static AnimeFolderRefreshRunDto From(AnimeFolderRefreshRun run, IReadOnlyList<AnimeFolderRefreshItemDto>? items = null) =>
        new(run.Id, run.Status, run.StartedAtUtc, run.FinishedAtUtc, run.MyAnimeId, run.TotalAnimes, run.ProcessedAnimes,
            run.Updated, run.NotFound, run.Unavailable, run.Failed, run.Error, items);
}

/// <summary>Resultado de um anime na execucao.</summary>
/// <param name="Id">Cursor de paginacao (use como <c>afterId</c>).</param>
/// <param name="Outcome">Ver <see cref="AnimeFolderRefreshOutcomes"/>.</param>
/// <param name="State">Estado gravado (<c>none</c>/<c>partial</c>/<c>complete</c>); <c>null</c> quando preservado.</param>
/// <param name="Videos">Videos contados; <c>null</c> quando nao houve contagem.</param>
/// <param name="Detail">Explicacao curta, sem caminho fisico.</param>
public sealed record AnimeFolderRefreshItemDto(
    long Id,
    int MalId,
    int MyAnimeId,
    string Outcome,
    string? State,
    int? Videos,
    string Detail,
    DateTimeOffset OccurredAtUtc)
{
    public static AnimeFolderRefreshItemDto From(AnimeFolderRefreshItem item) =>
        new(item.Id, item.MalId, item.MyAnimeId, item.Outcome, item.State, item.VideoCount, item.Detail, item.OccurredAtUtc);
}
