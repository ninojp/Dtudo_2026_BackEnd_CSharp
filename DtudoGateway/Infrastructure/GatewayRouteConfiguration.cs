using Yarp.ReverseProxy.Configuration;

namespace DtudoGateway.Infrastructure;

public static class GatewayRouteConfiguration
{
    public const string CatalogClusterId = "api-my-animes-catalog";
    public const string MusicClusterId = "api-musicx-catalog";
    public const string DiscogsClusterId = "api-discogs-external";
    public const string IdentityClusterId = "api-identity-oidc";
    public const string AnonymousPolicy = "gateway-anonymous";
    public const string AuthenticatedCatalogPolicy = "gateway-authenticated-catalog";
    public const string AuthenticatedMutationPolicy = "gateway-authenticated-mutation";
    public const string RequireCsrfMetadataKey = "Dtudo.RequireCsrf";
    public const string MaxBodyBytesMetadataKey = "Dtudo.MaxRequestBodyBytes";
    public const string AllowEmptyBodyMetadataKey = "Dtudo.AllowEmptyBody";
    public const string AnimeStatusPublicPath = "/api/catalog/animes/status";
    public const long AnimeStatusMaxBodyBytes = 16 * 1024;
    public const string AnimeWatchRoutePrefix = "anime-watch-";
    public const long AnimeWatchEpisodesMaxBodyBytes = 64 * 1024;
    public const long AnimeWatchBodylessMaxBodyBytes = 1024;

    /// <summary>Indica se o caminho e do progresso pessoal de episodios: <c>/api/catalog/animes/{id}/watch[/episodes|/all]</c>.</summary>
    public static bool IsAnimeWatchPath(PathString path)
    {
        var segments = (path.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length is < 5 or > 6
            || !segments[0].Equals("api", StringComparison.OrdinalIgnoreCase)
            || !segments[1].Equals("catalog", StringComparison.OrdinalIgnoreCase)
            || !segments[2].Equals("animes", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(segments[3], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _)
            || !segments[4].Equals("watch", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return segments.Length == 5
            || segments[5].Equals("episodes", StringComparison.OrdinalIgnoreCase)
            || segments[5].Equals("all", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<RouteConfig> CreateRoutes()
    {
        var routes = new List<RouteConfig>
        {
            CreateExactRoute("catalog-animes-list", "/api/catalog/animes", "/apiLocal/Anime"),
            CreateExactRoute("catalog-animes-search", "/api/catalog/animes/search", "/apiLocal/Anime/buscar"),
            CreateExactRoute("catalog-animes-relacionados", "/api/catalog/animes/relacionados", "/apiLocal/Anime/relacionados"),
            CreateAnimeStatusRoute(),
            CreateAnimeWatchRoute("get", HttpMethods.Get, "watch", requireCsrf: false),
            CreateAnimeWatchRoute("clear", HttpMethods.Delete, "watch", requireCsrf: true),
            CreateAnimeWatchRoute("mark-episodes", HttpMethods.Put, "watch/episodes", requireCsrf: true, hasBody: true),
            CreateAnimeWatchRoute("unmark-episodes", HttpMethods.Delete, "watch/episodes", requireCsrf: true, hasBody: true),
            CreateAnimeWatchRoute("mark-all", HttpMethods.Put, "watch/all", requireCsrf: true),
            CreateParameterizedRoute("catalog-anime-by-id", "/api/catalog/animes/{id:int}", "/apiLocal/Anime/{id}"),
            CreateExactRoute("catalog-collections-list", "/api/catalog/collections", "/apiLocal/MyAnime"),
            CreateParameterizedRoute("catalog-collection-by-id", "/api/catalog/collections/{id:int}", "/apiLocal/MyAnime/{id}"),
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
            CreateExactRoute(
                "external-discogs-artist-search",
                "/api/external/discogs/artists/search",
                "/ApiDiscogs/artists/search",
                clusterId: DiscogsClusterId,
                removeAuthorizationHeader: false),
            CreateParameterizedRoute(
                "external-discogs-artist-by-id",
                "/api/external/discogs/artists/{id:int}",
                "/ApiDiscogs/artists/{id}",
                clusterId: DiscogsClusterId,
                removeAuthorizationHeader: false),
            CreateParameterizedRoute(
                "external-discogs-artist-releases",
                "/api/external/discogs/artists/{id:int}/releases",
                "/ApiDiscogs/artists/{id}/releases",
                clusterId: DiscogsClusterId,
                removeAuthorizationHeader: false),
            CreateParameterizedRoute(
                "external-discogs-release-by-id",
                "/api/external/discogs/releases/{id:int}",
                "/ApiDiscogs/releases/{id}",
                clusterId: DiscogsClusterId,
                removeAuthorizationHeader: false),
            CreateParameterizedRoute(
                "external-discogs-master-by-id",
                "/api/external/discogs/masters/{id:int}",
                "/ApiDiscogs/masters/{id}",
                clusterId: DiscogsClusterId,
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
        string discogsDestinationAddress,
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
            ClusterId = DiscogsClusterId,
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
            {
                ["api-discogs"] = new DestinationConfig
                {
                    Address = discogsDestinationAddress.TrimEnd('/') + "/"
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

    private static RouteConfig CreateAnimeStatusRoute() =>
        new()
        {
            RouteId = "catalog-animes-status",
            ClusterId = CatalogClusterId,
            AuthorizationPolicy = AuthenticatedMutationPolicy,
            Match = new RouteMatch
            {
                Path = AnimeStatusPublicPath,
                Methods = [HttpMethods.Post]
            },
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [RequireCsrfMetadataKey] = bool.TrueString,
                [MaxBodyBytesMetadataKey] = AnimeStatusMaxBodyBytes.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            Transforms =
            [
                .. CreateTransforms(
                    "/apiLocal/Anime/status",
                    removeAuthorizationHeader: false),
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["RequestHeaderRemove"] = "X-CSRF-TOKEN"
                }
            ]
        };

    private static RouteConfig CreateAnimeWatchRoute(
        string name,
        string method,
        string suffix,
        bool requireCsrf,
        bool hasBody = false)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        if (requireCsrf)
        {
            metadata[RequireCsrfMetadataKey] = bool.TrueString;
            metadata[AllowEmptyBodyMetadataKey] = bool.TrueString;
            metadata[MaxBodyBytesMetadataKey] = (hasBody ? AnimeWatchEpisodesMaxBodyBytes : AnimeWatchBodylessMaxBodyBytes)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var transforms = new List<IReadOnlyDictionary<string, string>>(
            CreateTransforms("/apiLocal/Anime/{id}/" + suffix, usePathPattern: true, removeAuthorizationHeader: false));
        if (requireCsrf)
        {
            transforms.Add(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["RequestHeaderRemove"] = "X-CSRF-TOKEN"
            });
        }

        return new RouteConfig
        {
            RouteId = AnimeWatchRoutePrefix + name,
            ClusterId = CatalogClusterId,
            AuthorizationPolicy = AuthenticatedMutationPolicy,
            Match = new RouteMatch
            {
                Path = "/api/catalog/animes/{id:int}/" + suffix,
                Methods = [method]
            },
            Metadata = metadata,
            Transforms = transforms
        };
    }

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
