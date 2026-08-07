using ApiFileStorage.Contracts;
using ApiFileStorage.Configuration;
using ApiFileStorage.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApiFileStorage.Controllers;

[ApiController]
[Route("api/file-storage")]
[Authorize(Policy = "permission:filesystem.command")]
public sealed class FileStorageController(
    IStoragePathResolver pathResolver,
    IFileStorageLifecycleService lifecycleService,
    IFileStorageCommandService commandService,
    FileStorageDeletePreviewStore previewStore,
    IFileStorageStepUpValidator stepUpValidator,
    IOptions<FileStorageOptions> options) : ControllerBase
{
    [HttpPost("export/plan")]
    public ActionResult<PrepareStorageExportResponse> PrepareExport(
        [FromBody] PrepareStorageExportRequest? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var result = commandService.PrepareExport(new PrepareStorageExportCommand(
                request.MyAnimeId,
                request.MalIds ?? []));
            return Ok(new PrepareStorageExportResponse(
                result.MyAnimeId,
                result.Items
                    .Select(item => new PreparedStorageObjectResponse(item.MalId, item.ObjectId))
                    .ToArray()));
        }
        catch (FileStorageValidationException)
        {
            return BadRequest();
        }
        catch (StoragePathRejectedException)
        {
            return BadRequest();
        }
    }

    [HttpPost("resolve")]
    public ActionResult<ResolveStorageObjectResponse> Resolve([FromBody] ResolveStorageObjectRequest? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var metadata = pathResolver.ResolveExisting(request.ObjectId);
            return Ok(new ResolveStorageObjectResponse(
                StorageObjectId.Create(metadata.RootId, metadata.CanonicalRelativePath),
                metadata.Kind.ToString(),
                metadata.Length,
                metadata.LastWriteTimeUtc));
        }
        catch (StorageObjectNotFoundException)
        {
            return NotFound();
        }
        catch (StorageAccessDeniedException)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (StoragePathRejectedException)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Caminho recusado.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://dtudo.local/problems/storage-path-rejected"
            });
        }
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<ActionResult<ImportStorageFileResult>> Import(
        [FromForm] string? objectId,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectId) || file is null)
        {
            return BadRequest();
        }

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        await using var content = file.OpenReadStream();
        try
        {
            var result = await lifecycleService.ImportAsync(
                new ImportStorageFileCommand(
                    objectId,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    content,
                    idempotencyKey ?? string.Empty),
                cancellationToken);
            return Ok(result);
        }
        catch (FileStorageValidationException)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Importacao recusada.");
        }
        catch (StoragePathRejectedException)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Importacao recusada.");
        }
        catch (FileStorageThreatDetectedException)
        {
            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Arquivo recusado pelo scanner.");
        }
        catch (FileStorageScannerUnavailableException)
        {
            Response.Headers.RetryAfter = "60";
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Scanner indisponivel.");
        }
        catch (FileStorageInsufficientSpaceException)
        {
            return Problem(statusCode: StatusCodes.Status507InsufficientStorage, title: "Espaco insuficiente.");
        }
        catch (FileStorageConflictException)
        {
            return Conflict();
        }
        catch (FileStorageIntegrityException)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Estado de armazenamento inconsistente.");
        }
    }

    [HttpPost("delete")]
    public async Task<ActionResult<DeleteStorageFileResult>> Delete(
        [FromBody] DeleteStorageFileRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        try
        {
            var result = await lifecycleService.DeleteAsync(
                new DeleteStorageFileCommand(request.ObjectId, idempotencyKey ?? string.Empty),
                cancellationToken);
            return Ok(result);
        }
        catch (StorageObjectNotFoundException)
        {
            return NotFound();
        }
        catch (StoragePathRejectedException)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Operacao recusada.");
        }
        catch (FileStorageValidationException)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Operacao recusada.");
        }
        catch (FileStorageConflictException)
        {
            return Conflict();
        }
        catch (FileStorageIntegrityException)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Estado de armazenamento inconsistente.");
        }
    }

    [HttpPost("delete/preview")]
    public ActionResult<BulkDeletePreviewResponse> PreviewDelete(
        [FromBody] BulkDeletePreviewRequest? request)
    {
        if (request?.ObjectIds is null
            || !TryGetRequestContext(out var sessionId, out var deviceId))
        {
            return BadRequest();
        }

        var objectIds = request.ObjectIds
            .Where(objectId => !string.IsNullOrWhiteSpace(objectId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (objectIds.Length == 0
            || objectIds.Length > options.Value.Limits.MaxBulkDeleteItems)
        {
            return BadRequest();
        }

        var items = new List<BulkDeletePreviewItem>(objectIds.Length);
        try
        {
            foreach (var objectId in objectIds)
            {
                var metadata = pathResolver.ResolveExisting(objectId);
                if (metadata.Kind != StorageObjectKind.File)
                {
                    return BadRequest();
                }

                items.Add(new BulkDeletePreviewItem(
                    objectId,
                    metadata.Kind.ToString(),
                    metadata.Length,
                    metadata.LastWriteTimeUtc));
            }
        }
        catch (StorageObjectNotFoundException)
        {
            return NotFound();
        }
        catch (StorageAccessDeniedException)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (StoragePathRejectedException)
        {
            return BadRequest();
        }

        var preview = previewStore.Create(User, sessionId, deviceId, objectIds);
        return Ok(new BulkDeletePreviewResponse(preview.PreviewId, preview.ExpiresAtUtc, items));
    }

    [HttpPost("delete/batch")]
    public async Task<ActionResult<BulkDeleteResponse>> DeleteBatch(
        [FromBody] BulkDeleteRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null
            || !TryGetRequestContext(out var sessionId, out var deviceId)
            || !previewStore.TryGet(request.PreviewId, User, sessionId, deviceId, out var preview)
            || preview is null)
        {
            return NotFound();
        }

        if (!await stepUpValidator.IsAllowedAsync(cancellationToken))
        {
            return Forbid();
        }

        var results = new List<BulkDeleteItemResponse>(preview.ObjectIds.Count);
        for (var index = 0; index < preview.ObjectIds.Count; index++)
        {
            var objectId = preview.ObjectIds[index];
            try
            {
                var result = await lifecycleService.DeleteAsync(
                    new DeleteStorageFileCommand(
                        objectId,
                        $"bulk-delete-{request.PreviewId:N}-{index}"),
                    cancellationToken);
                results.Add(new BulkDeleteItemResponse(
                    objectId,
                    result.Replayed ? "replayed" : "deleted",
                    result.Sha256,
                    result.PurgeAtUtc));
            }
            catch (StorageObjectNotFoundException)
            {
                results.Add(new BulkDeleteItemResponse(objectId, "not-found", null, null));
            }
            catch (StoragePathRejectedException)
            {
                results.Add(new BulkDeleteItemResponse(objectId, "rejected", null, null));
            }
            catch (FileStorageValidationException)
            {
                results.Add(new BulkDeleteItemResponse(objectId, "rejected", null, null));
            }
            catch (FileStorageConflictException)
            {
                results.Add(new BulkDeleteItemResponse(objectId, "conflict", null, null));
            }
        }

        return Ok(new BulkDeleteResponse(request.PreviewId, results));
    }

    private bool TryGetRequestContext(out string sessionId, out string deviceId)
    {
        sessionId = Request.Headers[FileStorageRequestHeaders.SessionId].FirstOrDefault() ?? string.Empty;
        deviceId = Request.Headers[FileStorageRequestHeaders.DeviceId].FirstOrDefault() ?? string.Empty;
        return Guid.TryParse(sessionId, out _) && Guid.TryParse(deviceId, out _);
    }

    [HttpPost("reconcile")]
    public async Task<ActionResult<ReconcileStorageResult>> Reconcile(CancellationToken cancellationToken)
        => Ok(await lifecycleService.ReconcileAsync(cancellationToken));
}
