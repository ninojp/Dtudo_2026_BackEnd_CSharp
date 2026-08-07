using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinAppDtudo.Services;

public sealed class WinAppPkceBrowserClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly Uri _identityBaseUri;
    private readonly string _clientId;
    private readonly IReadOnlyList<string> _scopes;
    private readonly Uri _redirectUri;
    private readonly Func<Uri, Task> _browserLauncher;
    private readonly TimeSpan _timeout;

    public WinAppPkceBrowserClient(
        HttpClient? httpClient = null,
        Uri? identityBaseUri = null,
        string? clientId = null,
        IReadOnlyList<string>? scopes = null,
        TimeSpan? timeout = null,
        Uri? redirectUri = null,
        Func<Uri, Task>? browserLauncher = null)
    {
        _httpClient = httpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler());
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _identityBaseUri = (identityBaseUri ?? new Uri(AppConfigurationService.ApiIdentityBaseUrl, UriKind.Absolute))
            .EnsureTrailingSlash();
        _clientId = string.IsNullOrWhiteSpace(clientId)
            ? AppConfigurationService.IdentityClientId
            : clientId.Trim();
        _scopes = scopes is { Count: > 0 }
            ? scopes
            : AppConfigurationService.IdentityScopes;
        _redirectUri = redirectUri ?? AppConfigurationService.IdentityRedirectUri;
        _browserLauncher = browserLauncher ?? (uri =>
        {
            OpenSystemBrowser(uri);
            return Task.CompletedTask;
        });
        _timeout = timeout ?? AppConfigurationService.IdentityAuthenticationTimeout;
    }

    public async Task<WinAppTokenSet> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCancellation.CancelAfter(_timeout);

        var verifier = CreateRandomValue(32);
        var state = CreateRandomValue(32);
        var challenge = WinAppPkceProtocol.CreateCodeChallenge(verifier);
        if (!WinAppPkceProtocol.IsValidLoopbackRedirectUri(_redirectUri))
        {
            throw new WinAppAuthenticationException("O callback local do Identity nao e valido.");
        }

        using var listener = new TcpListener(IPAddress.Loopback, _redirectUri.Port);
        try
        {
            listener.Start();
        }
        catch (SocketException exception)
        {
            throw new WinAppAuthenticationException(
                $"A porta de callback {_redirectUri.Port} ja esta em uso.",
                exception);
        }

        var authorizationUri = BuildAuthorizationUri(_redirectUri, state, challenge);
        try
        {
            await _browserLauncher(authorizationUri);
            var code = await WaitForAuthorizationCodeAsync(listener, state, linkedCancellation.Token);
            var tokenSet = await ExchangeCodeAsync(code, verifier, _redirectUri, linkedCancellation.Token);
            return tokenSet;
        }
        finally
        {
            listener.Stop();
        }
    }

    private Uri BuildAuthorizationUri(Uri redirectUri, string state, string challenge)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri.ToString(),
            ["scope"] = string.Join(' ', _scopes),
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256"
        };
        return new Uri(
            new Uri(_identityBaseUri, "connect/authorize")
                + "?"
                + string.Join('&', query.Select(pair =>
                    $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}")));
    }

    private async Task<string> WaitForAuthorizationCodeAsync(
        TcpListener listener,
        string expectedState,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            using var client = await listener.AcceptTcpClientAsync(cancellationToken);
            if (client.Client.RemoteEndPoint is not IPEndPoint remote
                || !IPAddress.IsLoopback(remote.Address))
            {
                await WriteResponseAsync(client, HttpStatusCode.BadRequest, "Solicitacao invalida.", cancellationToken);
                continue;
            }

            var requestTarget = await ReadRequestTargetAsync(client, cancellationToken);
            var result = WinAppPkceProtocol.ValidateCallbackRequest(
                requestTarget,
                remote.Address,
                expectedState);
            if (result.Kind == CallbackValidationResultKind.Invalid)
            {
                await WriteResponseAsync(client, HttpStatusCode.BadRequest, "Solicitacao invalida.", cancellationToken);
                continue;
            }

            if (result.Kind == CallbackValidationResultKind.Denied)
            {
                await WriteResponseAsync(client, HttpStatusCode.BadRequest, "A autenticacao foi recusada.", cancellationToken);
                throw new WinAppAuthenticationException("A autenticacao foi recusada pelo provedor.");
            }

            await WriteResponseAsync(client, HttpStatusCode.OK, "Autenticacao concluida. Esta janela pode ser fechada.", cancellationToken);
            return result.Code!;
        }
    }

    private async Task<WinAppTokenSet> ExchangeCodeAsync(
        string code,
        string verifier,
        Uri redirectUri,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri.ToString()),
            new KeyValuePair<string, string>("code_verifier", verifier)
        ]);
        using var response = await _httpClient.PostAsync(
            new Uri(_identityBaseUri, "connect/token"),
            content,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new WinAppAuthenticationException("O provedor nao concluiu a autenticacao.");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenEndpointResponse>(JsonOptions, cancellationToken);
        if (payload is null
            || string.IsNullOrWhiteSpace(payload.AccessToken)
            || string.IsNullOrWhiteSpace(payload.RefreshToken)
            || payload.ExpiresIn is < 60)
        {
            throw new WinAppAuthenticationException("A resposta do provedor nao possui uma sessao renovavel valida.");
        }

        var now = DateTimeOffset.UtcNow;
        return new WinAppTokenSet(
            payload.AccessToken,
            payload.RefreshToken,
            now.AddSeconds(payload.ExpiresIn),
            payload.RefreshExpiresIn is > 0 ? now.AddSeconds(payload.RefreshExpiresIn.Value) : null,
            Guid.Empty,
            Guid.Empty);
    }

    private static async Task<string?> ReadRequestTargetAsync(
        TcpClient client,
        CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return null;
        }

        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 3 && string.Equals(parts[0], "GET", StringComparison.Ordinal)
            ? parts[1]
            : null;
    }

    private static async Task WriteResponseAsync(
        TcpClient client,
        HttpStatusCode statusCode,
        string message,
        CancellationToken cancellationToken)
    {
        await using var stream = client.GetStream();
        var body = $"<!doctype html><html><body>{WebUtility.HtmlEncode(message)}</body></html>";
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var header = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {(int)statusCode} {statusCode}\r\n"
            + "Content-Type: text/html; charset=utf-8\r\n"
            + $"Content-Length: {bodyBytes.Length}\r\n"
            + "Connection: close\r\n\r\n");
        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(bodyBytes, cancellationToken);
    }

    private static void OpenSystemBrowser(Uri authorizationUri)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = authorizationUri.ToString(),
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new WinAppAuthenticationException("Nao foi possivel abrir o navegador do sistema.", exception);
        }
    }

    private static string CreateRandomValue(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private sealed class TokenEndpointResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_expires_in")]
        public int? RefreshExpiresIn { get; set; }
    }
}

internal enum CallbackValidationResultKind
{
    Invalid,
    Denied,
    Success
}

internal readonly record struct CallbackValidationResult(
    CallbackValidationResultKind Kind,
    string? Code);

internal static class WinAppPkceProtocol
{
    public static CallbackValidationResult ValidateCallbackRequest(
        string? requestTarget,
        IPAddress? remoteAddress,
        string expectedState)
    {
        if (remoteAddress is null
            || !IPAddress.IsLoopback(remoteAddress)
            || string.IsNullOrWhiteSpace(requestTarget)
            || string.IsNullOrWhiteSpace(expectedState)
            || !Uri.TryCreate("http://127.0.0.1" + requestTarget, UriKind.Absolute, out var callbackUri)
            || callbackUri.AbsolutePath != "/callback/"
            || !string.IsNullOrEmpty(callbackUri.Fragment))
        {
            return new CallbackValidationResult(CallbackValidationResultKind.Invalid, null);
        }

        var values = ParseQuery(callbackUri.Query);
        if (!values.TryGetValue("state", out var state)
            || !FixedTimeEquals(state, expectedState))
        {
            return new CallbackValidationResult(CallbackValidationResultKind.Invalid, null);
        }

        if (values.ContainsKey("error"))
        {
            return new CallbackValidationResult(CallbackValidationResultKind.Denied, null);
        }

        return values.TryGetValue("code", out var code) && !string.IsNullOrWhiteSpace(code)
            ? new CallbackValidationResult(CallbackValidationResultKind.Success, code)
            : new CallbackValidationResult(CallbackValidationResultKind.Invalid, null);
    }

    public static string CreateCodeChallenge(string verifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verifier);
        var digest = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(digest)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool IsValidLoopbackRedirectUri(Uri uri) =>
        uri is not null
        && uri.Scheme == Uri.UriSchemeHttp
        && IPAddress.TryParse(uri.Host, out var address)
        && address.Equals(IPAddress.Loopback)
        && string.IsNullOrEmpty(uri.UserInfo)
        && uri.Port is >= 1024 and <= 65535
        && uri.AbsolutePath == "/callback/"
        && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment);

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var key = separator < 0 ? pair : pair[..separator];
            var value = separator < 0 ? string.Empty : pair[(separator + 1)..];
            values[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value.Replace('+', ' '));
        }

        return values;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.ASCII.GetBytes(left);
        var rightBytes = Encoding.ASCII.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

public sealed class WinAppAuthenticationException : Exception
{
    public WinAppAuthenticationException(string message)
        : base(message)
    {
    }

    public WinAppAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

internal static class UriExtensions
{
    public static Uri EnsureTrailingSlash(this Uri uri) =>
        uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? uri
            : new Uri(uri.AbsoluteUri + "/", UriKind.Absolute);
}
