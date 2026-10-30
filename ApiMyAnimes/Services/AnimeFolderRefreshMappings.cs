using ApiMyAnimes.Data;
using LibDtudo.Shared.Dtos;

namespace ApiMyAnimes.Services;

/// <summary>
/// Converte as entidades da execucao de atualizacao de pasta nos contratos compartilhados com o WinApp
/// (<see cref="AnimeFolderRefreshRunDto"/> e <see cref="AnimeFolderRefreshItemDto"/>, em <c>LibDtudo.Shared</c>).
/// </summary>
public static class AnimeFolderRefreshMappings
{
    public static AnimeFolderRefreshRunDto ToDto(this AnimeFolderRefreshRun run, IReadOnlyList<AnimeFolderRefreshItemDto>? items = null) =>
        new(run.Id, run.Status, run.StartedAtUtc, run.FinishedAtUtc, run.MyAnimeId, run.TotalAnimes, run.ProcessedAnimes,
            run.Updated, run.NotFound, run.Unavailable, run.Failed, run.Error, items);

    public static AnimeFolderRefreshItemDto ToDto(this AnimeFolderRefreshItem item) =>
        new(item.Id, item.MalId, item.MyAnimeId, item.Outcome, item.State, item.VideoCount, item.Detail, item.OccurredAtUtc);
}
