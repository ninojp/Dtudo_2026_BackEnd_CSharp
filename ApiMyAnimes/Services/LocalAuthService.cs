using System.Security.Cryptography;
using System.Text.Json;
using ApiMyAnimes.Configuration;
using LibDtudo.Shared.Dtos.Auth;
using LibDtudo.Shared.Utils;
using Microsoft.Extensions.Options;

namespace ApiMyAnimes.Services;

/// <summary>Servico local de autenticacao com hash de senha e armazenamento em JSON.</summary>
public sealed class LocalAuthService(IOptions<AuthOptions> options, IWebHostEnvironment environment)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _usersFilePath = ResolveUsersFilePath(options.Value.UsersFilePath, environment.ContentRootPath);

    /// <summary>Cadastra um novo usuario local.</summary>
    public async Task<AuthResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(name))
            return Fail("Informe o nome do usuario.");
        if (string.IsNullOrWhiteSpace(email))
            return Fail("Informe um e-mail valido.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return Fail("A senha deve ter pelo menos 8 caracteres.");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadUsersAsync(cancellationToken);
            if (users.Any(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)))
                return Fail("Usuario ja existe com este e-mail.");

            var user = new StoredAuthUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                CreatedAt = DateTimeOffset.UtcNow
            };

            users.Add(user);
            await SaveUsersAsync(users, cancellationToken);
            return Success(user);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Realiza login de usuario local.</summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var login = NormalizeEmail(request.Login);
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(request.Password))
            return Fail("Login e senha sao obrigatorios.");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadUsersAsync(cancellationToken);
            var user = users.FirstOrDefault(item =>
                string.Equals(item.Email, login, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Name, request.Login.Trim(), StringComparison.OrdinalIgnoreCase));

            if (user is null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
                return Fail("Usuario ou senha invalidos.");

            user.LastLoginAt = DateTimeOffset.UtcNow;
            await SaveUsersAsync(users, cancellationToken);
            return Success(user);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Obtem um usuario pelo ID.</summary>
    public async Task<AuthUserDto?> GetUserAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadUsersAsync(cancellationToken);
            return users.FirstOrDefault(user => user.Id == id)?.ToDto();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<StoredAuthUser>> LoadUsersAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_usersFilePath))
            return [];

        await using var stream = File.OpenRead(_usersFilePath);
        return await JsonSerializer.DeserializeAsync<List<StoredAuthUser>>(stream, _jsonOptions, cancellationToken) ?? [];
    }

    private async Task SaveUsersAsync(List<StoredAuthUser> users, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_usersFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await using var stream = File.Create(_usersFilePath);
        await JsonSerializer.SerializeAsync(stream, users.OrderBy(user => user.Email).ToList(), _jsonOptions, cancellationToken);
    }

    private static string ResolveUsersFilePath(string configuredPath, string contentRoot)
        => Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(contentRoot, configuredPath);

    private static string NormalizeEmail(string? email)
        => email?.Trim().ToLowerInvariant() ?? string.Empty;

    private static AuthResponse Fail(string message) => new() { Success = false, Message = message };

    private static AuthResponse Success(StoredAuthUser user) => new()
    {
        Success = true,
        Message = "Autenticado com sucesso.",
        User = user.ToDto(),
        Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    };

    private sealed class StoredAuthUser
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastLoginAt { get; set; }

        public AuthUserDto ToDto() => new()
        {
            Id = Id,
            Name = Name,
            Email = Email
        };
    }
}
