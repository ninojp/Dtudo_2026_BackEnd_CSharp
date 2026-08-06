using System.Text.Json;

namespace WinAppDtudo.Services;

public static class AppConfigurationService
{
    private static readonly Lazy<AppSettings> Settings = new(LoadSettings);

    public static string ApiMyAnimesBaseUrl =>
        GetEnvironment("DTUDO_API_MYANIMES_BASE_URL") ?? Settings.Value.ApiMyAnimes.BaseUrl;

    public static string ApiMyAnimeListBaseUrl =>
        GetEnvironment("DTUDO_API_MYANIMELIST_BASE_URL") ?? Settings.Value.ApiMyAnimeList.BaseUrl;

    public static string ApiMyAnimeListAutoStartUrl =>
        GetEnvironment("DTUDO_API_MYANIMELIST_AUTOSTART_URL") ?? Settings.Value.ApiMyAnimeList.AutoStartUrl;

    public static string DtudoSiteStartUrl =>
        GetEnvironment("DTUDO_SITE_START_URL") ?? Settings.Value.DtudoSite.StartUrl;

    public static string? DtudoSiteDirectory =>
        GetEnvironmentValue("DTUDO_SITE_DIRECTORY") ?? Settings.Value.DtudoSite.Directory;

    public static string DtudoLocalDbInstanceName =>
        GetEnvironmentValue("DTUDO_LOCALDB_INSTANCE") ?? Settings.Value.DtudoSite.LocalDbInstanceName;

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
        public DtudoSiteSettings DtudoSite { get; set; } = new();
        public HttpSettings Http { get; set; } = new();
    }

    private sealed class ApiSettings(string baseUrl)
    {
        public string BaseUrl { get; set; } = baseUrl;
        public string AutoStartUrl { get; set; } = baseUrl;
    }

    private sealed class DtudoSiteSettings
    {
        public string StartUrl { get; set; } = "http://localhost:5173/myanimes";
        public string? Directory { get; set; }
        public string LocalDbInstanceName { get; set; } = "MSSQLLocalDB";
        public int StartupTimeoutSeconds { get; set; } = 90;
        public string? GoogleChromeExecutablePath { get; set; }
        public string? NpmExecutablePath { get; set; }
    }

    private sealed class HttpSettings
    {
        public bool AllowInvalidCertificates { get; set; } = true;
    }
}
