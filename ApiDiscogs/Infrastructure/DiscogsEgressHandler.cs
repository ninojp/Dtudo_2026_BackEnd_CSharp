using System.Net;
using System.Net.Sockets;
using ApiDiscogs.Configuration;
using Microsoft.Extensions.Options;

namespace ApiDiscogs.Infrastructure;

public sealed class DiscogsEgressException(string message) : HttpRequestException(message);

public sealed class DiscogsEgressHandler(IOptions<DiscogsOptions> options) : DelegatingHandler
{
    private const string RequiredHost = "api.discogs.com";

    private static readonly string[] AllowedApiPathPrefixes =
    [
        "/database/",
        "/artists/",
        "/releases/",
        "/masters/"
    ];

    private readonly HashSet<string> _allowedHosts = options.Value.AllowedHosts
        .Select(NormalizeHost)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly string _allowedPathPrefix = options.Value.AllowedPathPrefix;

    public static bool IsValidBaseUrl(DiscogsOptions options)
    {
        return Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            && baseUri.Scheme == Uri.UriSchemeHttps
            && baseUri.Port == 443
            && string.IsNullOrEmpty(baseUri.UserInfo)
            && string.IsNullOrEmpty(baseUri.Query)
            && string.IsNullOrEmpty(baseUri.Fragment)
            && string.Equals(baseUri.DnsSafeHost, RequiredHost, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidAllowedHosts(DiscogsOptions options)
        => options.AllowedHosts is { Length: > 0 }
            && options.AllowedHosts.All(host =>
                !string.IsNullOrWhiteSpace(host)
                && string.Equals(NormalizeHost(host), RequiredHost, StringComparison.OrdinalIgnoreCase)
                && Uri.CheckHostName(NormalizeHost(host)) == UriHostNameType.Dns);

    public static bool IsValidPathPrefix(DiscogsOptions options)
        => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            && !string.IsNullOrWhiteSpace(options.AllowedPathPrefix)
            && options.AllowedPathPrefix.StartsWith('/')
            && options.AllowedPathPrefix.EndsWith('/')
            && baseUri.AbsolutePath.StartsWith(options.AllowedPathPrefix, StringComparison.Ordinal);

    public static bool IsAllowedRequestUri(DiscogsOptions options, Uri? requestUri)
    {
        if (requestUri is null
            || !requestUri.IsAbsoluteUri
            || requestUri.Scheme != Uri.UriSchemeHttps
            || requestUri.Port != 443
            || !string.IsNullOrEmpty(requestUri.UserInfo)
            || !IsAllowedHost(options, requestUri.DnsSafeHost)
            || !requestUri.AbsolutePath.StartsWith(options.AllowedPathPrefix, StringComparison.Ordinal)
            || !AllowedApiPathPrefixes.Any(prefix =>
                requestUri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal))
            || HasPathTraversal(requestUri))
        {
            return false;
        }

        return true;
    }

    public static HttpMessageHandler CreatePrimaryHandler(DiscogsOptions options)
        => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            MaxConnectionsPerServer = 20,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = (context, cancellationToken) => ConnectAsync(context, options, cancellationToken)
        };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedRequestUri(request.RequestUri, _allowedHosts, _allowedPathPrefix))
        {
            throw new DiscogsEgressException("O destino da API Discogs nao pertence a allowlist de egress.");
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        DiscogsOptions options,
        CancellationToken cancellationToken)
    {
        var host = NormalizeHost(context.DnsEndPoint.Host);
        if (context.DnsEndPoint.Port != 443
            || !string.Equals(host, RequiredHost, StringComparison.OrdinalIgnoreCase)
            || options.AllowedHosts is not { Length: > 0 }
            || !options.AllowedHosts
                .Select(NormalizeHost)
                .Contains(host, StringComparer.OrdinalIgnoreCase))
        {
            throw new DiscogsEgressException("A conexao da API Discogs foi bloqueada pela allowlist de egress.");
        }

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(address => !IsGloballyRoutable(address)))
        {
            throw new DiscogsEgressException("A resolucao DNS da API Discogs retornou um destino nao publico.");
        }

        Exception? lastException = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception exception) when (exception is SocketException or TimeoutException)
            {
                socket.Dispose();
                lastException = exception;
            }
        }

        throw new HttpRequestException("Nao foi possivel conectar a API Discogs.", lastException);
    }

    private static string NormalizeHost(string host)
        => host.TrimEnd('.');

    private static bool IsAllowedHost(DiscogsOptions options, string host)
        => options.AllowedHosts is { Length: > 0 }
            && options.AllowedHosts
                .Select(NormalizeHost)
                .Contains(NormalizeHost(host), StringComparer.OrdinalIgnoreCase);

    private static bool IsAllowedRequestUri(
        Uri? requestUri,
        HashSet<string> allowedHosts,
        string allowedPathPrefix)
    {
        return requestUri is not null
            && requestUri.IsAbsoluteUri
            && requestUri.Scheme == Uri.UriSchemeHttps
            && requestUri.Port == 443
            && string.IsNullOrEmpty(requestUri.UserInfo)
            && allowedHosts.Contains(NormalizeHost(requestUri.DnsSafeHost))
            && requestUri.AbsolutePath.StartsWith(allowedPathPrefix, StringComparison.Ordinal)
            && AllowedApiPathPrefixes.Any(prefix =>
                requestUri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal))
            && !HasPathTraversal(requestUri);
    }

    private static bool HasPathTraversal(Uri requestUri)
    {
        var escapedPath = requestUri.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
        return escapedPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .Any(segment => segment is "." or ".." || segment.Contains('\\'));
    }

    private static bool IsGloballyRoutable(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address)
            || address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.None))
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var first = bytes[0];
            var second = bytes[1];
            var third = bytes[2];

            return first is not (0 or 10 or 127 or >= 224)
                && !(first == 100 && second is >= 64 and <= 127)
                && !(first == 169 && second == 254)
                && !(first == 172 && second is >= 16 and <= 31)
                && !(first == 192 && second is 0 or 168)
                && !(first == 198 && second is 18 or 19 or 51)
                && !(first == 203 && second == 0 && third == 113);
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        var isUniqueLocal = (bytes[0] & 0xFE) == 0xFC;
        var isLinkLocal = bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80;
        var isDocumentation = bytes is [0x20, 0x01, 0x0D, 0xB8, ..];
        return !isUniqueLocal && !isLinkLocal && !isDocumentation && bytes[0] != 0xFF;
    }
}
