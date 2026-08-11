using Yarp.ReverseProxy.Configuration;

namespace DtudoGateway.Infrastructure;

public static class GatewayRouteConfiguration
{
    public const string CatalogClusterId = "api-my-animes-catalog";
    public const string MusicClusterId = "api-musicx-catalog";
    public const string IdentityClusterId = "api-identity-oidc";
    public const string AnonymousPolicy = "gateway-anonymous";
    public const string AuthenticatedCatalogPolicy = "gateway-authenticated-catalog";

    public static IReadOnlyList<RouteConfig> CreateRoutes()
    {
        var routes = new List<RouteConfig>
        {
            CreateExactRoute("catalog-animes-list", "/api/catalog/animes", "/apiLocal/Anime"),
            CreateExactRoute("catalog-animes-search", "/api/catalog/animes/search", "/apiLocal/Anime/buscar"),
            CreateParameterizedRoute("catalog-anime-by-id", "/api/catalog/animes/{id:int}", "/apiLocal/Anime/{id}"),
            CreateExactRoute("catalog-collections-list", "/api/catalog/collections", "/apiLocal/MyAnime/public"),
            CreateParameterizedRoute("catalog-collection-by-id", "/api/catalog/collections/{id:int}", "/apiLocal/MyAnime/public/{id}"),
            CreateExactRoute(
                "musicx-collections-list",
                "/api/catalog/music/collections",
                "/apiLocal/collections",
                clusterId: MusicClusterId,
                removeAuthorizationHeader: false),
            CreateParameterizedRoute(
                "musicx-collection-by-id",
                "/api/catalog/music/collections/{id:long}",
                "/apiLocal/collections/{id}",
                clusterId: MusicClusterId,
                removeAuthorizationHeader: false),
            CreateParameterizedRoute(
                "musicx-collection-releases",
                "/api/catalog/music/collections/{id:long}/releases",
                "/apiLocal/collections/{id}/releases",
                clusterId: MusicClusterId,
                removeAuthorizationHeader: false),
            CreateExactRoute(
                "musicx-artists-list",
                "/api/catalog/music/artists",
                "/apiLocal/artists",
                clusterId: MusicClusterId,
                removeAuthorizationHeader: false),
            CreateParameterizedRoute(
                "musicx-artist-by-id",
                "/api/catalog/music/artists/{id:long}",
                "/apiLocal/artists/{id}",
                clusterId: MusicClusterId,
                removeAuthorizationHeader: false),
            CreateParameterizedRoute(
                "musicx-release-by-id",
                "/api/catalog/music/releases/{id:long}",
                "/apiLocal/releases/{id}",
                clusterId: MusicClusterId,
                removeAuthorizationHeader: false),
        };

        routes.Add(CreateExactRoute(
            "identity-authorization",
            "/identity/connect/authorize",
            "/connect/authorize",
            IdentityClusterId,
            stripBrowserHeaders: false,
            authorizationPolicy: AnonymousPolicy));
        routes.Add(CreateExactRoute(
            "identity-logout",
            "/identity/connect/logout",
            "/connect/logout",
            IdentityClusterId,
            stripBrowserHeaders: false,
            authorizationPolicy: AnonymousPolicy));

        return routes;
    }

    public static IReadOnlyList<ClusterConfig> CreateClusters(
        string animeDestinationAddress,
        string musicDestinationAddress,
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
            ClusterId = MusicClusterId,
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
            {
                ["api-musicx"] = new DestinationConfig
                {
                    Address = musicDestinationAddress.TrimEnd('/') + "/"
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
        bool stripBrowserHeaders = true,
            string authorizationPolicy = AuthenticatedCatalogPolicy,
            bool removeAuthorizationHeader = true) =>
        new()
        {
            RouteId = routeId,
            ClusterId = clusterId,
            AuthorizationPolicy = authorizationPolicy,
            Match = new RouteMatch
            {
                Path = publicPath,
                Methods = [HttpMethods.Get]
            },
            Transforms = CreateTransforms(
                backendPath,
                stripBrowserHeaders: stripBrowserHeaders,
                removeAuthorizationHeader: removeAuthorizationHeader)
        };

    private static RouteConfig CreateParameterizedRoute(
        string routeId,
        string publicPath,
        string backendPath,
        string clusterId = CatalogClusterId,
        bool stripBrowserHeaders = true,
        bool removeAuthorizationHeader = true) =>
        new()
        {
            RouteId = routeId,
            ClusterId = clusterId,
            AuthorizationPolicy = AuthenticatedCatalogPolicy,
            Match = new RouteMatch
            {
                Path = publicPath,
                Methods = [HttpMethods.Get]
            },
            Transforms = CreateTransforms(
                backendPath,
                usePathPattern: true,
                stripBrowserHeaders: stripBrowserHeaders,
                removeAuthorizationHeader: removeAuthorizationHeader)
        };

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> CreateTransforms(
        string backendPath,
        bool usePathPattern = false,
        bool stripBrowserHeaders = true,
        bool removeAuthorizationHeader = true)
    {
        var transforms = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [usePathPattern ? "PathPattern" : "PathSet"] = backendPath
            }
        };

        if (removeAuthorizationHeader)
        {
            transforms.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["RequestHeaderRemove"] = "Authorization"
            });
        }

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
