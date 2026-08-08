using ApiFileStorage.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiFileStorage.Controllers;

[ApiController]
[Route("api/file-storage/health")]
[Authorize(Policy = "permission:health.read")]
public sealed class FileStorageHealthController(FileStorageHealthService healthService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(FileStorageHealthStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<FileStorageHealthStatus> Get() => Ok(healthService.GetStatus());
}
