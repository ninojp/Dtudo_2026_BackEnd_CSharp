using Yarp.ReverseProxy.Configuration;

namespace DtudoGateway.Infrastructure;

public static class GatewayRouteConfiguration
{
    public const string CatalogClusterId = "api-my-animes-catalog";
    public const string IdentityClusterId = "api-identity-oidc";
    public const string AnonymousPolicy = "gateway-public-catalog";

    public static IReadOnlyList<RouteConfig> CreateRoutes() =>
    [
        CreateExactRoute("catalog-animes-list", "/api/catalog/animes", "/apiLocal/Anime"),
        CreateExactRoute("catalog-animes-search", "/api/catalog/animes/search", "/apiLocal/Anime/buscar"),
        CreateParameterizedRoute("catalog-anime-by-id", "/api/catalog/animes/{id:int}", "/apiLocal/Anime/{id}"),
        CreateExactRoute("catalog-collections-list", "/api/catalog/collections", "/apiLocal/MyAnime"),
        CreateParameterizedRoute("catalog-collection-by-id", "/api/catalog/collections/{id:int}", "/apiLocal/MyAnime/{id}"),
        CreateExactRoute(
            "identity-authorization",
            "/identity/connect/authorize",
            "/connect/authorize",
            IdentityClusterId,
            stripBrowserHeaders: false),
        CreateExactRoute(
            "identity-logout",
            "/identity/connect/logout",
            "/connect/logout",
            IdentityClusterId,
            stripBrowserHeaders: false)
    ];

    public static IReadOnlyList<ClusterConfig> CreateClusters(
        string animeDestinationAddress,
        string identityDestinationAddress) =>
    [
        new ClusterConfig
        {
            ClusterId = CatalogClusterId,
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
            {
                ["api-my-animes"] = new DestinationConfig
                {
                    Address = animeDestinationAddress.TrimEnd('/') + "/"
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = IdentityClusterId,
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
            {
                ["api-identity"] = new DestinationConfig
                {
                    Address = identityDestinationAddress.TrimEnd('/') + "/"
                }
            }
        }
    ];

    private static RouteConfig CreateExactRoute(
        string routeId,
        string publicPath,
        string backendPath,
        string clusterId = CatalogClusterId,
        bool stripBrowserHeaders = true) =>
        new()
        {
            RouteId = routeId,
            ClusterId = clusterId,
            AuthorizationPolicy = AnonymousPolicy,
            Match = new RouteMatch
            {
                Path = publicPath,
                Methods = [HttpMethods.Get]
            },
            Transforms = CreateTransforms(backendPath, stripBrowserHeaders: stripBrowserHeaders)
        };

    private static RouteConfig CreateParameterizedRoute(string routeId, string publicPath, string backendPath) =>
        new()
        {
            RouteId = routeId,
            ClusterId = CatalogClusterId,
            AuthorizationPolicy = AnonymousPolicy,
            Match = new RouteMatch
            {
                Path = publicPath,
                Methods = [HttpMethods.Get]
            },
            Transforms = CreateTransforms(
                backendPath,
                usePathPattern: true,
                stripBrowserHeaders: true)
        };

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> CreateTransforms(
        string backendPath,
        bool usePathPattern = false,
        bool stripBrowserHeaders = true)
    {
        var transforms = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [usePathPattern ? "PathPattern" : "PathSet"] = backendPath
            },
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["RequestHeaderRemove"] = "Authorization"
            }
        };

        if (stripBrowserHeaders)
        {
            transforms.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["RequestHeaderRemove"] = "Cookie"
            });
            transforms.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ResponseHeaderRemove"] = "Set-Cookie"
            });
        }

        return transforms;
    }
}
