using System.Net;
using System.Text;
using System.Text.Json;
using LibDtudo.Shared.Dtos.Auth;

namespace WinAppDtudo.Services;

public sealed class AuthApiService
{
    private static readonly HttpClient HttpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    static AuthApiService()
    {
        HttpClient = new HttpClient(AppConfigurationService.CreateHttpClientHandler())
        {
            BaseAddress = new Uri(AppConfigurationService.ApiMyAnimesBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<AuthResponse> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        var request = new LoginRequest { Login = login, Password = password };
        using var response = await HttpClient.PostAsync("apiLocal/Auth/login", Serialize(request), cancellationToken);
        return await DeserializeAsync(response, cancellationToken);
    }

    public async Task<AuthResponse> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default)
    {
        var request = new RegisterUserRequest { Name = name, Email = email, Password = password };
        using var response = await HttpClient.PostAsync("apiLocal/Auth/register", Serialize(request), cancellationToken);
        return await DeserializeAsync(response, cancellationToken);
    }

    private static StringContent Serialize<T>(T value)
        => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static async Task<AuthResponse> DeserializeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(json))
        {
            var result = JsonSerializer.Deserialize<AuthResponse>(json, JsonOptions);
            if (result is not null) return result;
        }

        return new AuthResponse
        {
            Success = false,
            Message = response.StatusCode == HttpStatusCode.Unauthorized
                ? "Usuario ou senha invalidos."
                : $"Falha na autenticacao. Status {(int)response.StatusCode}."
        };
    }
}
