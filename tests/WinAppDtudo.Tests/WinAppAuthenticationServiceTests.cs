using System.Net;
using System.Text;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class WinAppAuthenticationServiceTests
{
    [Fact]
    public async Task SignInRefreshesSnakeCaseOAuthResponseAndRebindsAccessToken()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            "Dtudo2026",
            "WinAppAuthenticationServiceTests",
            Guid.NewGuid().ToString("N"),
            "session.bin");
        var sessionId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var store = new ProtectedTokenStore(filePath);
        await store.SaveAsync(new WinAppTokenSet(
            "expired-access-token",
            "valid-refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddHours(1),
            sessionId,
            deviceId));

        var handler = new RefreshHandler(sessionId);
        try
        {
            using var httpClient = new HttpClient(handler);
            using var service = new WinAppAuthenticationService(
                store,
                httpClient: httpClient);

            var session = await service.SignInAsync();

            Assert.Equal(sessionId, session.SessionId);
            Assert.Equal(deviceId, session.DeviceId);
            Assert.Equal("new-access-token", await service.GetAccessTokenAsync());
            Assert.True(handler.RefreshCalled);
            Assert.True(handler.BindingCalled);
            Assert.Equal("new-access-token", handler.BoundAccessToken);
            Assert.Equal("new-refresh-token", (await store.LoadAsync())!.RefreshToken);
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.Delete(Path.GetDirectoryName(filePath)!, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SignOutRevokesRefreshTokenAndSecuritySessionBeforeClearingLocalState()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            "Dtudo2026",
            "WinAppAuthenticationServiceTests",
            Guid.NewGuid().ToString("N"),
            "session.bin");
        var sessionId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var store = new ProtectedTokenStore(filePath);
        await store.SaveAsync(new WinAppTokenSet(
            "active-access-token",
            "active-refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(5),
            DateTimeOffset.UtcNow.AddHours(1),
            sessionId,
            deviceId));

        var handler = new LogoutHandler(sessionId);
        try
        {
            using var httpClient = new HttpClient(handler);
            using var service = new WinAppAuthenticationService(
                store,
                httpClient: httpClient);

            Assert.True(await service.SignOutAsync());
            Assert.True(handler.RefreshTokenRevoked);
            Assert.True(handler.SessionRevoked);
            Assert.Equal("active-refresh-token", handler.RevokedRefreshToken);
            Assert.Equal("dtudo-winapp", handler.RevocationClientId);
            Assert.Equal("refresh_token", handler.RevocationTokenTypeHint);
            Assert.False(File.Exists(filePath));
            Assert.False(service.IsAuthenticated);
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.Delete(Path.GetDirectoryName(filePath)!, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SignOutClearsLocalStateWhenIdentityIsUnavailable()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            "Dtudo2026",
            "WinAppAuthenticationServiceTests",
            Guid.NewGuid().ToString("N"),
            "session.bin");
        var store = new ProtectedTokenStore(filePath);
        await store.SaveAsync(new WinAppTokenSet(
            "expired-access-token",
            "active-refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddHours(1),
            Guid.NewGuid(),
            Guid.NewGuid()));

        try
        {
            using var httpClient = new HttpClient(new UnavailableHandler());
            using var service = new WinAppAuthenticationService(
                store,
                httpClient: httpClient);

            Assert.False(await service.SignOutAsync());
            Assert.False(File.Exists(filePath));
            Assert.False(service.IsAuthenticated);
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.Delete(Path.GetDirectoryName(filePath)!, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DisposeWhileSignOutIsInFlightDoesNotBreakSessionGateRelease()
    {
        var filePath = Path.Combine(
            Path.GetTempPath(),
            "Dtudo2026",
            "WinAppAuthenticationServiceTests",
            Guid.NewGuid().ToString("N"),
            "session.bin");
        var store = new ProtectedTokenStore(filePath);
        await store.SaveAsync(new WinAppTokenSet(
            "active-access-token",
            "active-refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(5),
            DateTimeOffset.UtcNow.AddHours(1),
            Guid.NewGuid(),
            Guid.NewGuid()));

        using var requestStarted = new ManualResetEventSlim();
        using var httpClient = new HttpClient(new BlockingHandler(requestStarted));
        using var cancellationTokenSource = new CancellationTokenSource();
        var service = new WinAppAuthenticationService(
            store,
            httpClient: httpClient);

        try
        {
            var signOutTask = service.SignOutAsync(cancellationTokenSource.Token);
            Assert.True(requestStarted.Wait(TimeSpan.FromSeconds(2)));

            service.Dispose();
            cancellationTokenSource.Cancel();

            await signOutTask;
            Assert.False(File.Exists(filePath));
        }
        finally
        {
            service.Dispose();
            if (Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.Delete(Path.GetDirectoryName(filePath)!, recursive: true);
            }
        }
    }

    private sealed class RefreshHandler(Guid sessionId) : HttpMessageHandler
    {
        public bool RefreshCalled { get; private set; }
        public bool BindingCalled { get; private set; }
        public string? BoundAccessToken { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/connect/token", StringComparison.Ordinal))
            {
                RefreshCalled = true;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"access_token\":\"new-access-token\",\"refresh_token\":\"new-refresh-token\",\"expires_in\":300,\"refresh_expires_in\":3600}",
                        Encoding.UTF8,
                        "application/json")
                });
            }

            if (path.Equals($"/identity/security/sessions/{sessionId:D}/token", StringComparison.Ordinal))
            {
                BindingCalled = true;
                BoundAccessToken = request.Headers.Authorization?.Parameter;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class LogoutHandler(Guid sessionId) : HttpMessageHandler
    {
        public bool RefreshTokenRevoked { get; private set; }
        public bool SessionRevoked { get; private set; }
        public string? RevokedRefreshToken { get; private set; }
        public string? RevocationClientId { get; private set; }
        public string? RevocationTokenTypeHint { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/connect/revocation", StringComparison.Ordinal))
            {
                RefreshTokenRevoked = true;
                var form = await request.Content!.ReadAsStringAsync(cancellationToken);
                RevokedRefreshToken = GetFormValue(form, "token");
                RevocationClientId = GetFormValue(form, "client_id");
                RevocationTokenTypeHint = GetFormValue(form, "token_type_hint");
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            if (path.Equals($"/identity/security/sessions/{sessionId:D}", StringComparison.Ordinal))
            {
                SessionRevoked = true;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static string? GetFormValue(string form, string name)
        {
            var value = form.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .SingleOrDefault(item => item.StartsWith($"{name}=", StringComparison.Ordinal));
            return value is null
                ? null
                : Uri.UnescapeDataString(value.Split('=', 2)[1]);
        }
    }

    private sealed class UnavailableHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Identity unavailable.");
    }

    private sealed class BlockingHandler(ManualResetEventSlim requestStarted) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            requestStarted.Set();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
