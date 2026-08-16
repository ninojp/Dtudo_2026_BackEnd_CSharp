using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LibDtudo.Shared.Dtos;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class ApiMyAnimesServiceTests
{
    [Fact]
    public async Task EnsureCollection_UsesAuthorizedIdempotentPut()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new EnsureMyAnimeCollectionResponse
            {
                Id = 7,
                Titulo = "Colecao A",
                AnimesMalId = [1, 2],
                Created = true,
                Changed = true
            })
        });
        using var apiHttpClient = CreateApiHttpClient(handler);
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            {
                var service = new ApiMyAnimesService(authenticationService, apiHttpClient);
                var result = await service.GarantirMyAnimeColecaoAsync(new AdicionaMyAnimeDto
                {
                    Titulo = "Colecao A",
                    AnimesMalId = [1, 2]
                });

                Assert.Equal(7, result.Id);
            }

            Assert.Equal(1, handler.RequestCount);
            Assert.Equal(HttpMethod.Put, handler.Method);
            Assert.Equal(
                "/apiLocal/catalog-migration/my-animes/by-title",
                handler.RequestUri?.AbsolutePath);
            Assert.Equal("Bearer", handler.AuthorizationScheme);
            Assert.False(string.IsNullOrWhiteSpace(handler.AuthorizationParameter));

            var payload = JsonSerializer.Deserialize<EnsureMyAnimeCollectionRequest>(
                handler.Body ?? string.Empty,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(payload);
            Assert.Equal("Colecao A", payload!.Titulo);
            Assert.Equal([1, 2], payload.AnimesMalId);
            Assert.DoesNotContain("ConnectionStrings", handler.Body ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("path", handler.Body ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public async Task EnsureAssociation_UsesDedicatedAuthorizedPut()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new EnsureAnimeAssociationResponse
            {
                MalId = 42,
                MyAnimeId = 7,
                Changed = true
            })
        });
        using var apiHttpClient = CreateApiHttpClient(handler);
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            {
                var service = new ApiMyAnimesService(authenticationService, apiHttpClient);
                var result = await service.AssociarAnimeAoMyAnimeAsync(42, 7);

                Assert.True(result.Changed);
            }

            Assert.Equal(HttpMethod.Put, handler.Method);
            Assert.Equal(
                "/apiLocal/catalog-migration/animes/42/my-anime",
                handler.RequestUri?.AbsolutePath);
            Assert.Equal("Bearer", handler.AuthorizationScheme);
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public async Task UpdateRelatedIds_UsesAuthorizedPatch()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        using var apiHttpClient = CreateApiHttpClient(handler);
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            {
                var service = new ApiMyAnimesService(authenticationService, apiHttpClient);
                await service.AtualizarAnimesRelacionadosIdsAsync(42, [7, -1, 7, 8]);
            }

            Assert.Equal(HttpMethod.Patch, handler.Method);
            Assert.Equal("/apiLocal/Anime/42", handler.RequestUri?.AbsolutePath);
            Assert.Equal("Bearer", handler.AuthorizationScheme);

            using var document = JsonDocument.Parse(handler.Body ?? string.Empty);
            var operation = document.RootElement[0];
            Assert.Equal("replace", operation.GetProperty("op").GetString());
            Assert.Equal("/AnimesRelacionadosIds", operation.GetProperty("path").GetString());
            Assert.Equal([7, 8], operation.GetProperty("value").EnumerateArray().Select(value => value.GetInt32()));
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public async Task ProtectedMutation_WithoutSessionFailsClosed()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var apiHttpClient = CreateApiHttpClient(handler);
        var service = new ApiMyAnimesService(httpClient: apiHttpClient);

        await Assert.ThrowsAsync<WinAppAuthenticationException>(() =>
            service.GarantirMyAnimeColecaoAsync(new AdicionaMyAnimeDto
            {
                Titulo = "Colecao A",
                AnimesMalId = [1]
            }));

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task EnsureCollection_BadRequestIncludesValidationDetails()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(new
            {
                title = "One or more validation errors occurred.",
                errors = new Dictionary<string, string[]>
                {
                    ["AnimesMalId"] = ["Informe pelo menos um MalId."]
                }
            })
        });
        using var apiHttpClient = CreateApiHttpClient(handler);
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            {
                var service = new ApiMyAnimesService(authenticationService, apiHttpClient);
                var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                    service.GarantirMyAnimeColecaoAsync(new AdicionaMyAnimeDto
                    {
                        Titulo = "Colecao A",
                        AnimesMalId = [1]
                    }));

                Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
                Assert.Contains("AnimesMalId: Informe pelo menos um MalId.", exception.Message);
            }
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    private static HttpClient CreateApiHttpClient(RecordingHandler handler)
        => new(handler)
        {
            BaseAddress = new Uri("https://api-my-animes.test/")
        };

    private static async Task<(WinAppAuthenticationService Service, string Directory)> CreateAuthenticationServiceAsync()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "Dtudo2026-WinAppTests",
            Guid.NewGuid().ToString("N"));
        var tokenStore = new ProtectedTokenStore(Path.Combine(temporaryDirectory, "session.bin"));
        var now = DateTimeOffset.UtcNow;
        await tokenStore.SaveAsync(new WinAppTokenSet(
            TestTokens.SuperAdministratorAccessToken,
            new string('r', 32),
            now.AddMinutes(5),
            now.AddDays(1),
            Guid.NewGuid(),
            Guid.NewGuid()));

        return (new WinAppAuthenticationService(tokenStore), temporaryDirectory);
    }

    private static void DeleteTemporaryDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return responseFactory(request);
        }
    }
}
