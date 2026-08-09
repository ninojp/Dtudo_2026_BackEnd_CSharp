using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WinAppDtudo.Services;

public sealed record IdentityAdminContext(Guid SessionId, Guid DeviceId);

public sealed record WinAppAdminAccount(
    string Id,
    string? UserName,
    string? Email,
    bool IsActivationCompleted,
    bool IsLocked,
    DateTimeOffset? LockoutEndUtc,
    IReadOnlyList<string> Roles);

public sealed record WinAppAdminRole(string Id, string Name, IReadOnlyList<string> PermissionKeys);

public sealed record WinAppAdminPermission(string Key, string Description);

public sealed record WinAppAdminDevice(
    string AccountId,
    Guid DeviceId,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset TrustedUntilUtc,
    bool IsRevoked);

public sealed record WinAppAdminSession(
    string AccountId,
    Guid SessionId,
    Guid DeviceId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool IsRevoked);

public sealed record WinAppAdminProvisionResult(
    bool Succeeded,
    WinAppInitialSecretDelivery? Delivery);

public sealed record WinAppInitialSecretDelivery(
    Guid ActivationId,
    string InitialSecret,
    DateTimeOffset ExpiresAtUtc);

public sealed class IdentityAdministrationApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly WinAppAuthenticationService _authenticationService;

    public IdentityAdministrationApiClient(WinAppAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<IReadOnlyList<WinAppAdminAccount>> GetAccountsAsync(
        IdentityAdminContext context,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WinAppAdminAccount>>(
            BuildQuery("identity/admin/accounts", context),
            cancellationToken);

    public Task<IReadOnlyList<WinAppAdminRole>> GetRolesAsync(
        IdentityAdminContext context,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WinAppAdminRole>>(
            BuildQuery("identity/admin/roles", context),
            cancellationToken);

    public Task<IReadOnlyList<WinAppAdminPermission>> GetPermissionsAsync(
        IdentityAdminContext context,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WinAppAdminPermission>>(
            BuildQuery("identity/admin/permissions", context),
            cancellationToken);

    public Task<IReadOnlyList<WinAppAdminDevice>> GetDevicesAsync(
        IdentityAdminContext context,
        bool includeRevoked = true,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WinAppAdminDevice>>(
            BuildQuery("identity/admin/devices", context, ("includeRevoked", includeRevoked.ToString())),
            cancellationToken);

    public Task<IReadOnlyList<WinAppAdminSession>> GetSessionsAsync(
        IdentityAdminContext context,
        bool includeRevoked = true,
        CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<WinAppAdminSession>>(
            BuildQuery("identity/admin/sessions", context, ("includeRevoked", includeRevoked.ToString())),
            cancellationToken);

    public Task<WinAppAdminProvisionResult?> ProvisionAsync(
        string userName,
        string email,
        string password,
        string roleName,
        IdentityAdminContext context,
        CancellationToken cancellationToken = default) =>
        SendAsync<WinAppAdminProvisionResult>(
            HttpMethod.Post,
            "identity/admin/accounts",
            new
            {
                UserName = userName,
                Email = email,
                RoleName = roleName,
                Password = password,
                SessionId = context.SessionId,
                DeviceId = context.DeviceId
            },
            cancellationToken);

    public Task AssignRoleAsync(
        string accountId,
        string roleName,
        bool assign,
        IdentityAdminContext context,
        CancellationToken cancellationToken = default) =>
        SendNoContentAsync(
            HttpMethod.Post,
            $"identity/admin/accounts/{Uri.EscapeDataString(accountId)}/roles",
            new
            {
                RoleName = roleName,
                Assign = assign,
                SessionId = context.SessionId,
                DeviceId = context.DeviceId
            },
            cancellationToken);

    public Task SetLockAsync(
        string accountId,
        bool lockAccount,
        IdentityAdminContext context,
        CancellationToken cancellationToken = default) =>
        SendNoContentAsync(
            HttpMethod.Post,
            $"identity/admin/accounts/{Uri.EscapeDataString(accountId)}/lock",
            new
            {
                Lock = lockAccount,
                SessionId = context.SessionId,
                DeviceId = context.DeviceId
            },
            cancellationToken);

    public Task RevokeSessionAsync(
        Guid sessionId,
        IdentityAdminContext context,
        CancellationToken cancellationToken = default) =>
        SendNoContentAsync(
            HttpMethod.Delete,
            BuildQuery($"identity/admin/sessions/{sessionId:D}", context),
            null,
            cancellationToken);

    public Task RevokeDeviceAsync(
        Guid deviceId,
        IdentityAdminContext context,
        CancellationToken cancellationToken = default) =>
        SendNoContentAsync(
            HttpMethod.Delete,
            BuildQuery($"identity/admin/devices/{deviceId:D}", context),
            null,
            cancellationToken);

    public Task GrantProvisionStepUpAsync(
        string token,
        IdentityAdminContext context,
        CancellationToken cancellationToken = default) =>
        SendNoContentAsync(
            HttpMethod.Post,
            "identity/security/totp/step-up",
            new
            {
                Action = "identity.provision",
                Token = token,
                SessionId = context.SessionId,
                DeviceId = context.DeviceId
            },
            cancellationToken);

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _authenticationService.SendAuthenticatedAsync(
            _ => new HttpRequestMessage(HttpMethod.Get, path),
            cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken)
            ?? throw new WinAppAuthenticationException("O Identity retornou uma resposta vazia.");
    }

    private async Task<T?> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken)
    {
        using var response = await _authenticationService.SendAuthenticatedAsync(
            _ => new HttpRequestMessage(method, path)
            {
                Content = payload is null ? null : JsonContent.Create(payload)
            },
            cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    private async Task SendNoContentAsync(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken cancellationToken)
    {
        using var response = await _authenticationService.SendAuthenticatedAsync(
            _ => new HttpRequestMessage(method, path)
            {
                Content = payload is null ? null : JsonContent.Create(payload)
            },
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    private static async Task<T?> ReadResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private static async Task<WinAppAuthenticationException> CreateExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = response.StatusCode switch
        {
            HttpStatusCode.Forbidden => "A operacao administrativa exige permissao, sessao ativa e, quando aplicavel, step-up MFA valido.",
            HttpStatusCode.Unauthorized => "A sessao do Identity expirou ou foi revogada.",
            _ => $"O Identity rejeitou a operacao. Status {(int)response.StatusCode}."
        };
        var details = ReadErrorDetails(detail);
        return new WinAppAuthenticationException(
            details.Count == 0 ? message : $"{message}\n\n{string.Join("\n", details)}");
    }

    private static IReadOnlyList<string> ReadErrorDetails(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return Array.Empty<string>();
        }

        try
        {
            var error = JsonSerializer.Deserialize<IdentityErrorResponse>(detail, JsonOptions);
            return error?.Errors is { Count: > 0 }
                ? error.Errors
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
                : Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private sealed record IdentityErrorResponse(
        string? Error,
        IReadOnlyList<string>? Errors);

    private static string BuildQuery(
        string path,
        IdentityAdminContext context,
        params (string Name, string Value)[] extra)
    {
        var values = new List<string>
        {
            $"sessionId={Uri.EscapeDataString(context.SessionId.ToString("D"))}",
            $"deviceId={Uri.EscapeDataString(context.DeviceId.ToString("D"))}"
        };
        values.AddRange(extra.Select(item =>
            $"{Uri.EscapeDataString(item.Name)}={Uri.EscapeDataString(item.Value)}"));
        return $"{path}?{string.Join('&', values)}";
    }
}
