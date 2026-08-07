using System.Security.Claims;
using ApiFileStorage.Configuration;
using ApiFileStorage.Contracts;
using ApiFileStorage.Controllers;
using ApiFileStorage.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApiFileStorage.Tests;

public sealed class FileStorageCommandTests : IDisposable
{
    private readonly string _temporaryDirectory;
    private readonly FileStorageOptions _options;

    public FileStorageCommandTests()
    {
        _temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "DtudoFileStorageCommandTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_temporaryDirectory);
        _options = new FileStorageOptions
        {
            Roots =
            [
                new AllowedStorageRootOptions
                {
                    Id = "media",
                    Path = _temporaryDirectory
                }
            ],
            ExportRootId = "media",
            ExportPathPrefix = "my-animes",
            Limits = new FileStorageLimitsOptions
            {
                MaxBulkDeleteItems = 10,
                DeletePreviewLifetimeSeconds = 120
            }
        };
    }

    [Fact]
    public void PrepareExport_ReturnsLogicalIdsWithoutPhysicalPaths()
    {
        var rootCatalog = new StorageRootCatalog(Options.Create(_options));
        var service = new FileStorageCommandService(rootCatalog, Options.Create(_options));

        var result = service.PrepareExport(new PrepareStorageExportCommand(7, [42, 12, 42]));

        Assert.Equal([12, 42], result.Items.Select(item => item.MalId));
        Assert.All(result.Items, item =>
        {
            Assert.StartsWith("v1.", item.ObjectId, StringComparison.Ordinal);
            Assert.DoesNotContain(_temporaryDirectory, item.ObjectId, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/my-animes/7/", item.ObjectId, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task BulkDelete_RequiresStepUpAndUsesTrashLifecycle()
    {
        var objectId = StorageObjectId.Create("media", "my-animes/7/42.jpg");
        var resolver = new FakePathResolver(objectId);
        var lifecycle = new FakeLifecycleService();
        var stepUp = new FakeStepUpValidator();
        var previewStore = new FileStorageDeletePreviewStore(
            TimeProvider.System,
            Options.Create(_options));
        var controller = CreateController(resolver, lifecycle, stepUp, previewStore);

        var previewResult = controller.PreviewDelete(new BulkDeletePreviewRequest([objectId]));
        var preview = Assert.IsType<OkObjectResult>(previewResult.Result).Value as BulkDeletePreviewResponse;
        Assert.NotNull(preview);
        Assert.DoesNotContain(_temporaryDirectory, System.Text.Json.JsonSerializer.Serialize(preview), StringComparison.OrdinalIgnoreCase);

        var denied = await controller.DeleteBatch(
            new BulkDeleteRequest(preview!.PreviewId),
            CancellationToken.None);
        Assert.IsType<ForbidResult>(denied.Result);
        Assert.Equal(0, lifecycle.DeleteCalls);

        stepUp.Allowed = true;
        var accepted = await controller.DeleteBatch(
            new BulkDeleteRequest(preview.PreviewId),
            CancellationToken.None);
        var response = Assert.IsType<OkObjectResult>(accepted.Result).Value as BulkDeleteResponse;

        Assert.NotNull(response);
        Assert.Single(response!.Items);
        Assert.Equal("deleted", response.Items[0].Status);
        Assert.Equal(1, lifecycle.DeleteCalls);
        Assert.StartsWith("bulk-delete-", lifecycle.LastIdempotencyKey, StringComparison.Ordinal);
    }

    private FileStorageController CreateController(
        IStoragePathResolver resolver,
        IFileStorageLifecycleService lifecycle,
        IFileStorageStepUpValidator stepUp,
        FileStorageDeletePreviewStore previewStore)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "account-1"), new Claim("permission", "filesystem.command")],
                "test"))
        };
        httpContext.Request.Headers[FileStorageRequestHeaders.SessionId] = Guid.NewGuid().ToString("D");
        httpContext.Request.Headers[FileStorageRequestHeaders.DeviceId] = Guid.NewGuid().ToString("D");

        return new FileStorageController(
            resolver,
            lifecycle,
            new FakeCommandService(),
            previewStore,
            stepUp,
            Options.Create(_options))
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
            Directory.Delete(_temporaryDirectory, recursive: true);
    }

    private sealed class FakePathResolver(string objectId) : IStoragePathResolver
    {
        public StorageObjectMetadata ResolveExisting(string requestedObjectId)
        {
            Assert.Equal(objectId, requestedObjectId);
            return new StorageObjectMetadata(
                "media",
                "my-animes/7/42.jpg",
                "my-animes/7/42.jpg",
                StorageObjectKind.File,
                10,
                DateTimeOffset.UtcNow);
        }

        public StorageWriteTarget ResolveWriteTarget(string requestedObjectId) =>
            throw new NotSupportedException();
    }

    private sealed class FakeLifecycleService : IFileStorageLifecycleService
    {
        public int DeleteCalls { get; private set; }

        public string LastIdempotencyKey { get; private set; } = string.Empty;

        public Task<ImportStorageFileResult> ImportAsync(
            ImportStorageFileCommand command,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DeleteStorageFileResult> DeleteAsync(
            DeleteStorageFileCommand command,
            CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            LastIdempotencyKey = command.IdempotencyKey;
            return Task.FromResult(new DeleteStorageFileResult(
                command.ObjectId,
                new string('a', 64),
                DateTimeOffset.UtcNow.AddDays(7),
                false));
        }

        public Task<ReconcileStorageResult> ReconcileAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCommandService : IFileStorageCommandService
    {
        public PrepareStorageExportResult PrepareExport(PrepareStorageExportCommand command) =>
            new(command.MyAnimeId, []);
    }

    private sealed class FakeStepUpValidator : IFileStorageStepUpValidator
    {
        public bool Allowed { get; set; }

        public Task<bool> IsAllowedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Allowed);
    }
}
