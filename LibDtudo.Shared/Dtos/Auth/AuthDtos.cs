namespace LibDtudo.Shared.Dtos.Auth;

/// <summary>Requisicao para cadastro de usuario.</summary>
public sealed class RegisterUserRequest
{
    /// <summary>Nome de exibicao do usuario.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>E-mail utilizado para login.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Senha em texto puro recebida somente na requisicao.</summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>Requisicao para login.</summary>
public sealed class LoginRequest
{
    /// <summary>E-mail ou nome de usuario.</summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>Senha em texto puro recebida somente na requisicao.</summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>Usuario autenticado sem dados sensiveis.</summary>
public sealed class AuthUserDto
{
    /// <summary>ID do usuario.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Nome de exibicao.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>E-mail.</summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>Resposta de autenticacao.</summary>
public sealed class AuthResponse
{
    /// <summary>Indica sucesso da operacao.</summary>
    public bool Success { get; set; }

    /// <summary>Mensagem de erro ou status.</summary>
    public string? Message { get; set; }

    /// <summary>Usuario autenticado.</summary>
    public AuthUserDto? User { get; set; }

    /// <summary>Token de sessao local.</summary>
    public string? Token { get; set; }
}
