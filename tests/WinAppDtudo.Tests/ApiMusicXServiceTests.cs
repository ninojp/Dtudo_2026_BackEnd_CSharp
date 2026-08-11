using System.Net;
using System.Net.Http.Json;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class ApiMusicXServiceTests
{
    [Fact]
    public async Task ImportUsesBearerAndReportsStagesWithoutSendingSecretsInPayload()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new ApiMusicXImportCollectionResponse(
                new ApiMusicXCollectionDto(
                    11,
                    "Colecao A",
                    null,
                    [new ApiMusicXArtistSummaryDto(7, "Artista A", ApiMusicXArtistType.Solo)],
                    [],
                    [new ApiMusicXExternalIdentifierDto("ApiNode.MyMusicX", "Collection", "legacy-1")]),
                Created: true,
                Changed: true,
                ArtistsAdded: 1,
                ReleasesAdded: 0,
                TracksAdded: 0,
                LocalFileReferencesAdded: 0))
        });
        using var apiHttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-musicx.test/")
        };
        var progress = new RecordingProgress();
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            {
                var service = new ApiMusicXService(authenticationService, apiHttpClient);
                var result = await service.ImportarColecaoAsync(
                    new ApiMusicXImportCollectionRequest
                    {
                        DisplayName = "Colecao A",
                        ExternalIdentifiers =
                        [
                            new ApiMusicXExternalIdentifierRequest
                            {
                                Provider = "ApiNode.MyMusicX",
                                ResourceType = "Collection",
                                ExternalId = "legacy-1"
                            }
                        ],
                        Artists =
                        [
                            new ApiMusicXArtistImportRequest
                            {
                                DisplayName = "Artista A",
                                ArtistType = ApiMusicXArtistType.Solo
                            }
                        ]
                    },
                    progress);

                Assert.Equal(11, result.Collection.MusicCollectionId);
            }

            Assert.Equal(1, handler.RequestCount);
            Assert.Equal(HttpMethod.Post, handler.Method);
            Assert.Equal("/apiLocal/collections/import", handler.RequestUri?.AbsolutePath);
            Assert.Equal("Bearer", handler.AuthorizationScheme);
            Assert.Equal(TestTokens.SuperAdministratorAccessToken, handler.AuthorizationParameter);
            Assert.Contains("preparando", string.Join(Environment.NewLine, progress.Messages), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("resposta HTTP 201", string.Join(Environment.NewLine, progress.Messages), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("concluida", string.Join(Environment.NewLine, progress.Messages), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(TestTokens.SuperAdministratorAccessToken, handler.Body ?? string.Empty, StringComparison.Ordinal);
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public async Task ProtectedReadWithoutSessionFailsBeforeSendingRequest()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var apiHttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-musicx.test/")
        };
        var service = new ApiMusicXService(httpClient: apiHttpClient);

        await Assert.ThrowsAsync<WinAppAuthenticationException>(() =>
            service.ObterColecoesAsync());

        Assert.Equal(0, handler.RequestCount);
    }

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
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class RecordingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string? value)
        {
            if (value is not null)
            {
                Messages.Add(value);
            }
        }
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
