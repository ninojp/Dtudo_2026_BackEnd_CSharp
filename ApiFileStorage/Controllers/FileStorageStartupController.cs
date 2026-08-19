using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiFileStorage.Controllers;

public sealed record FileStorageStartupResponse(
    string Service,
    int ContractVersion);

[ApiController]
[Route("api/file-storage/startup")]
[AllowAnonymous]
public sealed class FileStorageStartupController : ControllerBase
{
    public const int CurrentContractVersion = 2;

    [HttpGet]
    [ProducesResponseType(typeof(FileStorageStartupResponse), StatusCodes.Status200OK)]
    public ActionResult<FileStorageStartupResponse> Get() =>
        Ok(new FileStorageStartupResponse(
            "ApiFileStorage",
            CurrentContractVersion));
}
