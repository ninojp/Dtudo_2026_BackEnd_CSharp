using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class FileStorageApiClientTests
{
    [Fact]
    public async Task PrepareExportSendsOnlyIdsWithAuthenticatedSessionContext()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new WinAppStorageExportPlan(
                7,
                [new WinAppStorageExportPlanItem(42, "v1.logical-42")]))
        });
        using var storageHttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://file-storage.test/")
        };
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            {
                var client = new FileStorageApiClient(authenticationService, storageHttpClient);
                var result = await client.PrepareExportAsync(7, [42]);

                Assert.Equal(7, result.MyAnimeId);
            }

            Assert.Equal(HttpMethod.Post, handler.Method);
            Assert.Equal("/api/file-storage/export/plan", handler.RequestUri?.AbsolutePath);
            Assert.Equal("Bearer", handler.AuthorizationScheme);
            Assert.False(string.IsNullOrWhiteSpace(handler.AuthorizationParameter));
            Assert.True(Guid.TryParse(handler.SessionId, out _));
            Assert.True(Guid.TryParse(handler.DeviceId, out _));
            Assert.Contains("42", handler.Body ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("ConnectionStrings", handler.Body ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("C:\\", handler.Body ?? string.Empty, StringComparison.Ordinal);
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public async Task ImportSendsRecreatableMultipartWithIdempotencyKey()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new WinAppStorageImportResult(
                "v1.logical-42",
                new string('a', 64),
                3,
                DateTimeOffset.UtcNow,
                false))
        });
        using var storageHttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://file-storage.test/")
        };
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            {
                var client = new FileStorageApiClient(authenticationService, storageHttpClient);
                var result = await client.ImportAsync(
                    "v1.logical-42",
                    "42.jpg",
                    "image/jpeg",
                    new byte[] { 1, 2, 3 },
                    "export-7-42");

                Assert.False(result.Replayed);
            }

            Assert.Equal("export-7-42", handler.IdempotencyKey);
            Assert.Equal("/api/file-storage/import", handler.RequestUri?.AbsolutePath);
            Assert.Contains("v1.logical-42", handler.Body ?? string.Empty, StringComparison.Ordinal);
            Assert.Contains("42.jpg", handler.Body ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("C:\\", handler.Body ?? string.Empty, StringComparison.Ordinal);
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    private static async Task<(WinAppAuthenticationService Service, string Directory)> CreateAuthenticationServiceAsync()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "Dtudo2026-WinAppFileStorageTests",
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
        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        public string? SessionId { get; private set; }

        public string? DeviceId { get; private set; }

        public string? IdempotencyKey { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            SessionId = request.Headers.TryGetValues("X-Dtudo-Session-Id", out var sessionValues)
                ? sessionValues.Single()
                : null;
            DeviceId = request.Headers.TryGetValues("X-Dtudo-Device-Id", out var deviceValues)
                ? deviceValues.Single()
                : null;
            IdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var idempotencyValues)
                ? idempotencyValues.Single()
                : null;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return responseFactory(request);
        }
    }
}
