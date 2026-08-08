using System.Net;
using System.Net.Http.Json;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class WinAppHealthMonitoringServiceTests
{
    [Fact]
    public async Task HealthQueriesUseBearerAndKeepUnavailableSourcesIndependent()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/file-storage/health")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        status = "ok",
                        roots = new[]
                        {
                            new
                            {
                                status = "ok",
                                availableBytes = 900L,
                                totalBytes = 1_000L,
                                minimumFreeSpaceBytes = 100L
                            }
                        },
                        scanner = new { status = "ok" },
                        quarantine = new { status = "ok", pendingCount = 0, threatCount = 0 }
                    })
                };
            }

            return request.RequestUri?.AbsolutePath == "/ApiMyAnimeList/health"
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            using (var httpClient = new HttpClient(handler))
            using (var service = new WinAppHealthMonitoringService(
                       authenticationService,
                       httpClient,
                       new WinAppHealthMonitoringOptions
                       {
                           IdentityBaseUrl = new Uri("https://identity.test/"),
                           ApiMyAnimesBaseUrl = new Uri("https://animes.test/"),
                           ApiMyAnimeListBaseUrl = new Uri("https://mal.test/"),
                           ApiFileStorageBaseUrl = new Uri("https://storage.test/"),
                           CertificateTargets = [],
                           BackupRoot = null,
                           ProbeTimeout = TimeSpan.FromSeconds(2)
                       }))
            {
                var snapshot = await service.CheckAsync();

                Assert.Equal(WinAppHealthState.Healthy, Find(snapshot, "ApiIdentity").State);
                Assert.Equal(WinAppHealthState.Healthy, Find(snapshot, "ApiMyAnimes").State);
                Assert.Equal(WinAppHealthState.Unavailable, Find(snapshot, "ApiMyAnimeList").State);
                Assert.Equal(WinAppHealthState.Healthy, Find(snapshot, "ApiFileStorage").State);
                Assert.Equal(WinAppHealthState.Healthy, Find(snapshot, "Armazenamento").State);
                Assert.Equal(WinAppHealthState.Healthy, Find(snapshot, "Ciclo de arquivos").State);
            }

            Assert.True(handler.Requests.Count >= 4);
            Assert.All(handler.Requests, request =>
            {
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                Assert.False(string.IsNullOrWhiteSpace(request.Headers.Authorization?.Parameter));
            });
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public async Task TimeoutDoesNotThrowOrBlockOtherStateEvaluation()
    {
        var handler = new RecordingHandler(async (request, cancellationToken) =>
        {
            if (request.RequestUri?.Host == "mal.test")
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            using (var httpClient = new HttpClient(handler))
            using (var service = new WinAppHealthMonitoringService(
                       authenticationService,
                       httpClient,
                       new WinAppHealthMonitoringOptions
                       {
                           IdentityBaseUrl = new Uri("https://identity.test/"),
                           ApiMyAnimesBaseUrl = new Uri("https://animes.test/"),
                           ApiMyAnimeListBaseUrl = new Uri("https://mal.test/"),
                           ApiFileStorageBaseUrl = new Uri("https://storage.test/"),
                           CertificateTargets = [],
                           BackupRoot = null,
                           ProbeTimeout = TimeSpan.FromMilliseconds(25)
                       }))
            {
                var snapshot = await service.CheckAsync();

                Assert.Equal(WinAppHealthState.Unavailable, Find(snapshot, "ApiMyAnimeList").State);
                Assert.Equal(WinAppHealthState.Healthy, Find(snapshot, "ApiMyAnimes").State);
            }
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public async Task ExpiringSessionIsReportedAsWarning()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync(TimeSpan.FromMinutes(2));

        try
        {
            using (authenticationService)
            using (var httpClient = new HttpClient(handler))
            using (var service = new WinAppHealthMonitoringService(
                       authenticationService,
                       httpClient,
                       new WinAppHealthMonitoringOptions
                       {
                           IdentityBaseUrl = new Uri("https://identity.test/"),
                           ApiMyAnimesBaseUrl = new Uri("https://animes.test/"),
                           ApiMyAnimeListBaseUrl = new Uri("https://mal.test/"),
                           ApiFileStorageBaseUrl = new Uri("https://storage.test/"),
                           CertificateTargets = [],
                           BackupRoot = null,
                           ProbeTimeout = TimeSpan.FromSeconds(2)
                       }))
            {
                await authenticationService.GetAccessTokenAsync();
                var snapshot = await service.CheckAsync();

                Assert.Equal(WinAppHealthState.Warning, Find(snapshot, "Sessao administrativa").State);
            }
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public async Task ExpiredAccessTokenWithRefreshableSessionIsNotReportedAsCritical()
    {
        var handler = new RecordingHandler((request, cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("Identity unavailable.")));
        using var authenticationHttpClient = new HttpClient(handler);
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync(
            TimeSpan.FromMinutes(-1),
            authenticationHttpClient);

        try
        {
            using (authenticationService)
            using (var httpClient = new HttpClient(handler))
            using (var service = new WinAppHealthMonitoringService(
                       authenticationService,
                       httpClient,
                       new WinAppHealthMonitoringOptions
                       {
                           IdentityBaseUrl = new Uri("https://identity.test/"),
                           ApiMyAnimesBaseUrl = new Uri("https://animes.test/"),
                           ApiMyAnimeListBaseUrl = new Uri("https://mal.test/"),
                           ApiFileStorageBaseUrl = new Uri("https://storage.test/"),
                           CertificateTargets = [],
                           BackupRoot = null,
                           ProbeTimeout = TimeSpan.FromSeconds(2)
                       }))
            {
                await authenticationService.SignInAsync();
                var snapshot = await service.CheckAsync();

                Assert.Equal(WinAppHealthState.Warning, Find(snapshot, "Sessao administrativa").State);
            }
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public async Task LowStorageProducesCriticalState()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/file-storage/health")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        status = "ok",
                        roots = new[]
                        {
                            new
                            {
                                status = "critical",
                                availableBytes = 50L,
                                totalBytes = 1_000L,
                                minimumFreeSpaceBytes = 100L
                            }
                        },
                        scanner = new { status = "ok" },
                        quarantine = new { status = "ok", pendingCount = 0, threatCount = 0 }
                    })
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var (authenticationService, temporaryDirectory) = await CreateAuthenticationServiceAsync();

        try
        {
            using (authenticationService)
            using (var httpClient = new HttpClient(handler))
            using (var service = new WinAppHealthMonitoringService(
                       authenticationService,
                       httpClient,
                       new WinAppHealthMonitoringOptions
                       {
                           IdentityBaseUrl = new Uri("https://identity.test/"),
                           ApiMyAnimesBaseUrl = new Uri("https://animes.test/"),
                           ApiMyAnimeListBaseUrl = new Uri("https://mal.test/"),
                           ApiFileStorageBaseUrl = new Uri("https://storage.test/"),
                           CertificateTargets = [],
                           BackupRoot = null,
                           ProbeTimeout = TimeSpan.FromSeconds(2)
                       }))
            {
                var snapshot = await service.CheckAsync();

                Assert.Equal(WinAppHealthState.Critical, Find(snapshot, "Armazenamento").State);
            }
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    [Fact]
    public void OnlyCriticalItemsRequestWindowsNotification()
    {
        var critical = new WinAppHealthItem(
            "Servicos",
            "ApiIdentity",
            WinAppHealthState.Critical,
            "estado",
            DateTimeOffset.UtcNow);
        var unavailable = critical with { State = WinAppHealthState.Unavailable };

        Assert.True(critical.RequiresNotification);
        Assert.False(unavailable.RequiresNotification);
    }

    [Fact]
    public void BackupProbeMarksBrokenManifestAsCritical()
    {
        var root = Path.Combine(Path.GetTempPath(), "Dtudo2026-WinAppTests", Guid.NewGuid().ToString("N"));
        var backup = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd"));
        Directory.CreateDirectory(backup);
        File.WriteAllText(Path.Combine(backup, "manifest.json"), "{\"BackupType\":\"Dtudo\",\"CreatedUtc\":\"2026-08-07T00:00:00Z\"}");
        File.WriteAllText(Path.Combine(backup, "manifest.sha256"), "0000  manifest.json");

        try
        {
            var item = WinAppBackupHealthProbe.Check(root, DateTimeOffset.Parse("2026-08-07T12:00:00Z"));
            Assert.Equal(WinAppHealthState.Critical, item.State);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    [Fact]
    public void BackupProbeMarksValidManifestAsHealthy()
    {
        var root = Path.Combine(Path.GetTempPath(), "Dtudo2026-WinAppTests", Guid.NewGuid().ToString("N"));
        var backup = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd"));
        Directory.CreateDirectory(backup);
        var manifestPath = Path.Combine(backup, "manifest.json");
        var manifest = "{\"BackupType\":\"Dtudo\",\"CreatedUtc\":\"" +
            DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O") + "\"}";
        File.WriteAllText(manifestPath, manifest);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(manifestPath)));
        File.WriteAllText(Path.Combine(backup, "manifest.sha256"), hash + "  manifest.json");

        try
        {
            var item = WinAppBackupHealthProbe.Check(root, DateTimeOffset.UtcNow);

            Assert.Equal(WinAppHealthState.Healthy, item.State);
            Assert.Equal("Backup recente e manifesto verificado.", item.Summary);
        }
        finally
        {
            DeleteTemporaryDirectory(root);
        }
    }

    private static WinAppHealthItem Find(WinAppHealthSnapshot snapshot, string name) =>
        snapshot.Items.Single(item => item.Name == name);

    private static async Task<(WinAppAuthenticationService Service, string Directory)> CreateAuthenticationServiceAsync(
        TimeSpan? accessTokenLifetime = null,
        HttpClient? httpClient = null)
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "Dtudo2026-WinAppTests",
            Guid.NewGuid().ToString("N"));
        var tokenStore = new ProtectedTokenStore(Path.Combine(temporaryDirectory, "session.bin"));
        var now = DateTimeOffset.UtcNow;
        await tokenStore.SaveAsync(new WinAppTokenSet(
            new string('t', 32),
            new string('r', 32),
            now.Add(accessTokenLifetime ?? TimeSpan.FromMinutes(5)),
            now.AddDays(1),
            Guid.NewGuid(),
            Guid.NewGuid()));

        return (new WinAppAuthenticationService(tokenStore, httpClient: httpClient), temporaryDirectory);
    }

    private static void DeleteTemporaryDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responseFactory;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            : this((request, _) => Task.FromResult(responseFactory(request)))
        {
        }

        public RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return _responseFactory(request, cancellationToken);
        }
    }
}
