using System.Net;
using System.Net.Sockets;
using ApiMyAnimeList.Configuration;
using Microsoft.Extensions.Options;

namespace ApiMyAnimeList.Services;

public sealed class MyAnimeListEgressException(string message) : HttpRequestException(message);

public sealed class MyAnimeListEgressHandler(IOptions<MyAnimeListOptions> options) : DelegatingHandler
{
    private readonly HashSet<string> _allowedHosts = options.Value.AllowedHosts
        .Select(NormalizeHost)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    private readonly string _allowedPathPrefix = options.Value.AllowedPathPrefix;

    public static bool IsValidConfiguration(MyAnimeListOptions options)
    {
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme != Uri.UriSchemeHttps
            || baseUri.Port != 443
            || !string.IsNullOrEmpty(baseUri.UserInfo)
            || string.IsNullOrWhiteSpace(options.AllowedPathPrefix)
            || !options.AllowedPathPrefix.StartsWith('/')
            || !options.AllowedPathPrefix.EndsWith('/')
            || !baseUri.AbsolutePath.StartsWith(options.AllowedPathPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (options.AllowedHosts is not { Length: > 0 }
            || options.AllowedHosts.Any(host => Uri.CheckHostName(host) != UriHostNameType.Dns))
        {
            return false;
        }

        return options.AllowedHosts.Contains(baseUri.DnsSafeHost, StringComparer.OrdinalIgnoreCase);
    }

    public static HttpMessageHandler CreatePrimaryHandler(MyAnimeListOptions options)
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
        var requestUri = request.RequestUri;
        if (requestUri is null
            || !requestUri.IsAbsoluteUri
            || requestUri.Scheme != Uri.UriSchemeHttps
            || requestUri.Port != 443
            || !string.IsNullOrEmpty(requestUri.UserInfo)
            || !_allowedHosts.Contains(NormalizeHost(requestUri.DnsSafeHost))
            || !requestUri.AbsolutePath.StartsWith(_allowedPathPrefix, StringComparison.Ordinal))
        {
            throw new MyAnimeListEgressException("O destino da API MyAnimeList nao pertence a allowlist de egress.");
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        MyAnimeListOptions options,
        CancellationToken cancellationToken)
    {
        var host = NormalizeHost(context.DnsEndPoint.Host);
        if (context.DnsEndPoint.Port != 443
            || options.AllowedHosts is not { Length: > 0 }
            || !options.AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
        {
            throw new MyAnimeListEgressException("A conexao da API MyAnimeList foi bloqueada pela allowlist de egress.");
        }

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(address => !IsGloballyRoutable(address)))
        {
            throw new MyAnimeListEgressException("A resolucao DNS da API MyAnimeList retornou um destino nao publico.");
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

        throw new HttpRequestException("Nao foi possivel conectar a API MyAnimeList.", lastException);
    }

    private static string NormalizeHost(string host)
        => host.TrimEnd('.');

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
