using ApiMyAnimes.Services;
using LibDtudo.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Mvc;

namespace ApiMyAnimes.Controllers;

/// <summary>Endpoints de autenticacao local para Dtudo2026.</summary>
[ApiController]
[Route("apiLocal/[controller]")]
public sealed class AuthController(LocalAuthService authService) : ControllerBase
{
    /// <summary>Cadastra um usuario local com senha hasheada.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, cancellationToken);
        if (!response.Success)
        {
            var statusCode = response.Message?.Contains("ja existe", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
            return StatusCode(statusCode, response);
        }

        return CreatedAtAction(nameof(Me), new { id = response.User!.Id }, response);
    }

    /// <summary>Realiza login local.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return response.Success ? Ok(response) : Unauthorized(response);
    }

    /// <summary>Obtem usuario por ID sem dados sensiveis.</summary>
    [HttpGet("me/{id}")]
    [ProducesResponseType(typeof(AuthUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthUserDto>> Me(string id, CancellationToken cancellationToken)
    {
        var user = await authService.GetUserAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }
}
