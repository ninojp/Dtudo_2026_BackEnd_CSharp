using LibDtudo.Shared.Dtos;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class CriadorDeEstruturasTests
{
    [Fact]
    public async Task ExportUsesObjectIdsAndReportsUploadProgressWithoutLocalPath()
    {
        var storage = new FakeStorageClient();
        var downloader = new FakeCoverDownloader();
        var progress = new RecordingProgress<ProgressoExportacao>();
        var creator = new CriadorDeEstruturas(storage, downloader);

        var result = await creator.CriarEstruturaAsync(
            new ObterMyAnimeDto { Id = 7, Titulo = "Colecao" },
            [
                new ObterAnimeDto { MalId = 42, Titulo = "Anime 42" },
                new ObterAnimeDto { MalId = 12, Titulo = "Anime 12" }
            ],
            progress);

        Assert.Equal(2, result.TotalPastasCriadas);
        Assert.Equal(2, result.TotalImagensSalvas);
        Assert.Empty(result.Erros);
        Assert.Equal([12, 42], storage.PreparedMalIds);
        Assert.Equal([12, 42], storage.Uploads.Select(upload => upload.MalId));
        Assert.All(storage.Uploads, upload =>
        {
            Assert.StartsWith("v1.", upload.ObjectId, StringComparison.Ordinal);
            Assert.DoesNotContain("Colecao", upload.ObjectId, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("image/jpeg", upload.ContentType);
            Assert.Equal([1, 2, 3], upload.Content);
        });
        Assert.Contains(progress.Items, item => item.Mensagem.Contains("Enviando capa", StringComparison.Ordinal));
        Assert.Equal(100, progress.Items[^1].PercentualConcluido);
    }

    private sealed class FakeStorageClient : IFileStorageApiClient
    {
        public List<int> PreparedMalIds { get; } = [];

        public List<Upload> Uploads { get; } = [];

        public Task<WinAppStorageExportPlan> PrepareExportAsync(
            int myAnimeId,
            IReadOnlyCollection<int> malIds,
            CancellationToken cancellationToken = default)
        {
            PreparedMalIds.AddRange(malIds);
            return Task.FromResult(new WinAppStorageExportPlan(
                myAnimeId,
                malIds.Select(malId => new WinAppStorageExportPlanItem(
                    malId,
                    $"v1.logical-{malId}"))
                    .ToArray()));
        }

        public Task<WinAppStorageImportResult> ImportAsync(
            string objectId,
            string fileName,
            string contentType,
            ReadOnlyMemory<byte> content,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            Uploads.Add(new Upload(
                int.Parse(Path.GetFileNameWithoutExtension(fileName)),
                objectId,
                contentType,
                content.ToArray()));
            return Task.FromResult(new WinAppStorageImportResult(
                objectId,
                new string('a', 64),
                content.Length,
                DateTimeOffset.UtcNow,
                false));
        }

        public Task<WinAppStorageDeletePreview> PreviewDeleteAsync(
            IReadOnlyCollection<string> objectIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WinAppStepUpGrant> GrantDeleteStepUpAsync(
            string totpToken,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WinAppStorageDeleteBatch> DeleteBatchAsync(
            Guid previewId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public sealed record Upload(int MalId, string ObjectId, string ContentType, byte[] Content);
    }

    private sealed class FakeCoverDownloader : IAnimeCoverDownloader
    {
        public Task<byte[]?> DownloadJpegAsync(
            string? primaryUrl,
            int malId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<byte[]?>([1, 2, 3]);
    }

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public List<T> Items { get; } = [];

        public void Report(T value) => Items.Add(value);
    }
}
