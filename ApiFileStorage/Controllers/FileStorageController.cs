using ApiFileStorage.Contracts;
using ApiFileStorage.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiFileStorage.Controllers;

[ApiController]
[Route("api/file-storage")]
[Authorize(Policy = "permission:filesystem.command")]
public sealed class FileStorageController(
    IStoragePathResolver pathResolver,
    IFileStorageLifecycleService lifecycleService) : ControllerBase
{
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

    [HttpPost("reconcile")]
    public async Task<ActionResult<ReconcileStorageResult>> Reconcile(CancellationToken cancellationToken)
        => Ok(await lifecycleService.ReconcileAsync(cancellationToken));
}
