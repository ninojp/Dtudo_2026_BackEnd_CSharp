using System.Text.Json;

namespace WinAppDtudo.Services;

public static class AppConfigurationService
{
    private static readonly Lazy<AppSettings> Settings = new(LoadSettings);

    public static string ApiMyAnimesBaseUrl =>
        GetEnvironment("DTUDO_API_MYANIMES_BASE_URL") ?? Settings.Value.ApiMyAnimes.BaseUrl;

    public static string ApiMyAnimeListBaseUrl =>
        GetEnvironment("DTUDO_API_MYANIMELIST_BASE_URL") ?? Settings.Value.ApiMyAnimeList.BaseUrl;

    public static string ApiIdentityBaseUrl =>
        GetEnvironment("DTUDO_API_IDENTITY_BASE_URL") ?? Settings.Value.ApiIdentity.BaseUrl;
    public static string ApiFileStorageBaseUrl =>
        GetEnvironment("DTUDO_API_FILE_STORAGE_BASE_URL") ?? Settings.Value.ApiFileStorage.BaseUrl;

    public static string IdentityClientId =>
        GetEnvironmentValue("DTUDO_IDENTITY_CLIENT_ID") ?? Settings.Value.Identity.ClientId;

    public static IReadOnlyList<string> IdentityScopes =>
        Settings.Value.Identity.Scopes;

    public static IReadOnlyList<string> IdentityResources =>
        Settings.Value.Identity.Resources;

    public static Uri IdentityRedirectUri =>
        new(Settings.Value.Identity.RedirectUri, UriKind.Absolute);

    public static TimeSpan IdentityAuthenticationTimeout =>
        TimeSpan.FromSeconds(Math.Clamp(Settings.Value.Identity.AuthenticationTimeoutSeconds, 30, 600));

    public static TimeSpan HealthProbeTimeout =>
        TimeSpan.FromSeconds(Math.Clamp(Settings.Value.Monitoring.HealthProbeTimeoutSeconds, 1, 30));

    public static string? BackupRoot =>
        GetEnvironmentValue("DTUDO_BACKUP_ROOT") ?? Settings.Value.Monitoring.BackupRoot;

    public static string IdentitySessionStorePath =>
        GetEnvironmentValue("DTUDO_IDENTITY_SESSION_STORE")
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Dtudo2026",
            "WinAppDtudo",
            "identity-session.bin");

    public static string ApiMyAnimeListAutoStartUrl =>
        GetEnvironment("DTUDO_API_MYANIMELIST_AUTOSTART_URL") ?? Settings.Value.ApiMyAnimeList.AutoStartUrl;

    public static string DtudoSiteStartUrl =>
        GetEnvironment("DTUDO_SITE_START_URL") ?? Settings.Value.DtudoSite.StartUrl;

    public static string? DtudoSiteDirectory =>
        GetEnvironmentValue("DTUDO_SITE_DIRECTORY") ?? Settings.Value.DtudoSite.Directory;

    public static TimeSpan DtudoSiteStartupTimeout =>
        TimeSpan.FromSeconds(Math.Clamp(Settings.Value.DtudoSite.StartupTimeoutSeconds, 15, 300));

    public static string? GoogleChromeExecutablePath =>
        GetEnvironmentValue("DTUDO_GOOGLE_CHROME_PATH") ?? Settings.Value.DtudoSite.GoogleChromeExecutablePath;

    public static string? NpmExecutablePath =>
        GetEnvironmentValue("DTUDO_NPM_PATH") ?? Settings.Value.DtudoSite.NpmExecutablePath;

    public static bool AllowInvalidCertificates =>
        bool.TryParse(GetEnvironment("DTUDO_ALLOW_INVALID_CERTIFICATES"), out var envValue)
            ? envValue
            : Settings.Value.Http.AllowInvalidCertificates;

    public static HttpClientHandler CreateHttpClientHandler()
    {
        var handler = new HttpClientHandler();
#if DEBUG
        if (AllowInvalidCertificates)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
#endif
        return handler;
    }

    private static AppSettings LoadSettings()
    {
        foreach (var path in CandidatePaths())
        {
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AppSettings();
        }

        return new AppSettings();
    }

    private static IEnumerable<string> CandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        var solutionRoot = FindSolutionRoot();
        if (solutionRoot is not null)
            yield return Path.Combine(solutionRoot.FullName, "WinAppDtudo", "appsettings.json");
    }

    private static DirectoryInfo? FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Dtudo2026.slnx"))) return directory;
            directory = directory.Parent;
        }

        return null;
    }

    private static string? GetEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.TrimEnd('/');
    }

    private static string? GetEnvironmentValue(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class AppSettings
    {
        public ApiSettings ApiMyAnimes { get; set; } = new("https://localhost:63980");
        public ApiSettings ApiMyAnimeList { get; set; } = new("https://localhost:7146");
        public ApiSettings ApiIdentity { get; set; } = new("https://localhost:7243");
        public ApiSettings ApiFileStorage { get; set; } = new("https://localhost:7244");
        public IdentitySettings Identity { get; set; } = new();
        public DtudoSiteSettings DtudoSite { get; set; } = new();
        public HttpSettings Http { get; set; } = new();
        public MonitoringSettings Monitoring { get; set; } = new();
    }

    private sealed class ApiSettings(string baseUrl)
    {
        public string BaseUrl { get; set; } = baseUrl;
        public string AutoStartUrl { get; set; } = baseUrl;
    }

    private sealed class IdentitySettings
    {
        public string ClientId { get; set; } = "dtudo-winapp";
        public string RedirectUri { get; set; } = "http://127.0.0.1:49173/callback/";
        public string[] Scopes { get; set; } = ["openid", "profile", "offline_access", "identity.login", "identity.provision", "catalog.write", "catalog.delete", "health.read"];
        public string[] Resources { get; set; } = ["urn:dtudo:api-my-animes", "urn:dtudo:api-my-animelist"];
        public int AuthenticationTimeoutSeconds { get; set; } = 300;
    }

    private sealed class DtudoSiteSettings
    {
        public string StartUrl { get; set; } = "http://localhost:5173/animes";
        public string? Directory { get; set; }
        public int StartupTimeoutSeconds { get; set; } = 90;
        public string? GoogleChromeExecutablePath { get; set; }
        public string? NpmExecutablePath { get; set; }
    }

    private sealed class HttpSettings
    {
        public bool AllowInvalidCertificates { get; set; } = true;
    }

    private sealed class MonitoringSettings
    {
        public int HealthProbeTimeoutSeconds { get; set; } = 5;
        public string? BackupRoot { get; set; }
    }
}
