using System.Net;
using System.Net.Http.Json;
using LibDtudo.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ApiMyAnimes.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task RegisterAndLogin_ReturnAuthenticatedUserWithoutPassword()
    {
        var usersFile = Path.Combine(Path.GetTempPath(), $"dtudo-auth-{Guid.NewGuid():N}.json");
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Auth:UsersFilePath"] = usersFile,
                        ["ConnectionStrings:LocalDbConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Dtudo2026Tests;Trusted_Connection=True;TrustServerCertificate=True"
                    });
                });
            });

        var client = app.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/apiLocal/Auth/register", new RegisterUserRequest
        {
            Name = "Teste",
            Email = "teste@example.com",
            Password = "SenhaForte123!"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registerPayload);
        Assert.True(registerPayload.Success);
        Assert.Equal("teste@example.com", registerPayload.User?.Email);
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.Token));

        var loginResponse = await client.PostAsJsonAsync("/apiLocal/Auth/login", new LoginRequest
        {
            Login = "teste@example.com",
            Password = "SenhaForte123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loginPayload);
        Assert.True(loginPayload.Success);
        Assert.Equal("Teste", loginPayload.User?.Name);
        Assert.DoesNotContain("SenhaForte123!", await File.ReadAllTextAsync(usersFile), StringComparison.Ordinal);
    }
}
