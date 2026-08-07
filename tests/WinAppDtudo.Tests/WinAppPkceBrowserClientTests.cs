using System.Net;
using System.Net.Sockets;
using System.Text;
using WinAppDtudo.Services;

namespace WinAppDtudo.Tests;

public sealed class WinAppPkceBrowserClientTests
{
    [Fact]
    public void RedirectValidationRejectsNonLoopbackQueryFragmentAndUserInfo()
    {
        Assert.True(WinAppPkceProtocol.IsValidLoopbackRedirectUri(
            new Uri("http://127.0.0.1:49173/callback/")));
        Assert.False(WinAppPkceProtocol.IsValidLoopbackRedirectUri(
            new Uri("http://192.168.1.20:49173/callback/")));
        Assert.False(WinAppPkceProtocol.IsValidLoopbackRedirectUri(
            new Uri("http://127.0.0.1:49173/callback/?source=unexpected")));
        Assert.False(WinAppPkceProtocol.IsValidLoopbackRedirectUri(
            new Uri("http://127.0.0.1:49173/callback/#fragment")));
        Assert.False(WinAppPkceProtocol.IsValidLoopbackRedirectUri(
            new Uri("http://attacker@127.0.0.1:49173/callback/")));
    }

    [Fact]
    public void CallbackRequiresLoopbackPeerExpectedStateAndCallbackPath()
    {
        const string expectedState = "expected-state";

        Assert.Equal(
            CallbackValidationResultKind.Invalid,
            WinAppPkceProtocol.ValidateCallbackRequest(
                "/callback/?code=authorization-code&state=wrong-state",
                IPAddress.Loopback,
                expectedState).Kind);
        Assert.Equal(
            CallbackValidationResultKind.Invalid,
            WinAppPkceProtocol.ValidateCallbackRequest(
                "/wrong/?code=authorization-code&state=expected-state",
                IPAddress.Loopback,
                expectedState).Kind);
        Assert.Equal(
            CallbackValidationResultKind.Invalid,
            WinAppPkceProtocol.ValidateCallbackRequest(
                "/callback/?code=authorization-code&state=expected-state#fragment",
                IPAddress.Loopback,
                expectedState).Kind);
        Assert.Equal(
            CallbackValidationResultKind.Invalid,
            WinAppPkceProtocol.ValidateCallbackRequest(
                "/callback/?code=authorization-code&state=expected-state",
                IPAddress.Parse("192.168.1.20"),
                expectedState).Kind);
    }

    [Fact]
    public void ProviderErrorRequiresMatchingState()
    {
        Assert.Equal(
            CallbackValidationResultKind.Invalid,
            WinAppPkceProtocol.ValidateCallbackRequest(
                "/callback/?error=access_denied&state=wrong-state",
                IPAddress.Loopback,
                "expected-state").Kind);
        Assert.Equal(
            CallbackValidationResultKind.Denied,
            WinAppPkceProtocol.ValidateCallbackRequest(
                "/callback/?error=access_denied&state=expected-state",
                IPAddress.Loopback,
                "expected-state").Kind);
    }

    [Fact]
    public void PkceChallengeChangesWhenVerifierChanges()
    {
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        const string expectedChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        Assert.Equal(expectedChallenge, WinAppPkceProtocol.CreateCodeChallenge(verifier));
        Assert.NotEqual(
            expectedChallenge,
            WinAppPkceProtocol.CreateCodeChallenge(verifier + "-wrong"));
    }

    [Fact]
    public async Task AuthenticateIgnoresInvalidStateAndExchangesMatchingPkceVerifier()
    {
        var redirectUri = CreateLoopbackRedirectUri();
        Uri? authorizationUri = null;
        Task? callbackTask = null;
        var tokenHandler = new PkceTokenHandler(() => authorizationUri);

        using var httpClient = new HttpClient(tokenHandler);
        var client = new WinAppPkceBrowserClient(
            httpClient: httpClient,
            identityBaseUri: new Uri("https://identity.test/"),
            clientId: "dtudo-winapp",
            scopes: ["openid", "offline_access"],
            timeout: TimeSpan.FromSeconds(5),
            redirectUri: redirectUri,
            browserLauncher: uri =>
            {
                authorizationUri = uri;
                callbackTask = SendCallbacksAsync(uri, redirectUri.Port);
                return Task.CompletedTask;
            });

        var tokenSet = await client.AuthenticateAsync();
        await callbackTask!;

        Assert.Equal("access-token", tokenSet.AccessToken);
        Assert.Equal("refresh-token", tokenSet.RefreshToken);
        Assert.Equal("authorization-code", tokenHandler.Code);
        Assert.NotNull(tokenHandler.CodeVerifier);
        Assert.Equal(
            tokenHandler.AuthorizationChallenge,
            WinAppPkceProtocol.CreateCodeChallenge(tokenHandler.CodeVerifier!));
    }

    [Fact]
    public async Task AuthenticateRejectsOccupiedCallbackPortBeforeOpeningBrowser()
    {
        using var blocker = new TcpListener(IPAddress.Loopback, 0);
        blocker.Start();
        var port = ((IPEndPoint)blocker.LocalEndpoint).Port;
        var browserOpened = false;

        using var httpClient = new HttpClient(new RejectingHandler());
        var client = new WinAppPkceBrowserClient(
            httpClient: httpClient,
            redirectUri: new Uri($"http://127.0.0.1:{port}/callback/"),
            browserLauncher: _ =>
            {
                browserOpened = true;
                return Task.CompletedTask;
            });

        var exception = await Assert.ThrowsAsync<WinAppAuthenticationException>(
            () => client.AuthenticateAsync());

        Assert.Contains("porta de callback", exception.Message, StringComparison.Ordinal);
        Assert.False(browserOpened);
    }

    private static Uri CreateLoopbackRedirectUri()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return new Uri($"http://127.0.0.1:{port}/callback/");
    }

    private static async Task SendCallbacksAsync(Uri authorizationUri, int port)
    {
        var state = GetQueryValue(authorizationUri, "state");
        await SendCallbackAsync(
            port,
            $"/callback/?code=ignored-code&state={Uri.EscapeDataString("wrong-state")}");
        await SendCallbackAsync(
            port,
            $"/callback/?code=authorization-code&state={Uri.EscapeDataString(state)}");
    }

    private static async Task SendCallbackAsync(int port, string requestTarget)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        await using var stream = client.GetStream();
        var request = Encoding.ASCII.GetBytes(
            $"GET {requestTarget} HTTP/1.1\r\n"
            + $"Host: 127.0.0.1:{port}\r\n"
            + "Connection: close\r\n\r\n");
        await stream.WriteAsync(request);
        await stream.FlushAsync();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        await reader.ReadLineAsync();
    }

    private static string GetQueryValue(Uri uri, string name)
    {
        var encodedValue = uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Single(value => value.StartsWith($"{name}=", StringComparison.Ordinal))
            .Split('=', 2)[1];
        return Uri.UnescapeDataString(encodedValue.Replace('+', ' '));
    }

    private sealed class PkceTokenHandler(Func<Uri?> authorizationUriProvider) : HttpMessageHandler
    {
        public string? AuthorizationChallenge { get; private set; }
        public string? Code { get; private set; }
        public string? CodeVerifier { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!request.RequestUri!.AbsolutePath.EndsWith("/connect/token", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var form = ParseForm(await request.Content!.ReadAsStringAsync(cancellationToken));
            Code = form["code"];
            CodeVerifier = form["code_verifier"];
            AuthorizationChallenge = GetQueryValue(authorizationUriProvider()!, "code_challenge");
            if (Code != "authorization-code"
                || string.IsNullOrWhiteSpace(CodeVerifier)
                || !string.Equals(
                    AuthorizationChallenge,
                    WinAppPkceProtocol.CreateCodeChallenge(CodeVerifier),
                    StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"access_token\":\"access-token\",\"refresh_token\":\"refresh-token\",\"expires_in\":300,\"refresh_expires_in\":3600}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed class RejectingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
    }

    private static Dictionary<string, string> ParseForm(string form)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in form.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var key = separator < 0 ? pair : pair[..separator];
            var value = separator < 0 ? string.Empty : pair[(separator + 1)..];
            values[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value.Replace('+', ' '));
        }

        return values;
    }
}
