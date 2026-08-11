using ApiMusicX.Dtos;

namespace ApiMusicX.Services;

/// <summary>
/// Importa conjuntos normalizados enviados pelo WinAppDtudo.
/// </summary>
public interface IMusicCollectionImportService
{
    /// <summary>
    /// Executa uma importacao transacional, idempotente e nao destrutiva.
    /// </summary>
    Task<ImportMusicCollectionResponse> ImportAsync(
        ImportMusicCollectionRequest request,
        CancellationToken cancellationToken);
}
