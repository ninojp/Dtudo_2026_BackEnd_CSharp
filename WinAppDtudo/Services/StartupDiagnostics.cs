namespace WinAppDtudo.Services;

internal static class StartupDiagnostics
{
    private static readonly object SyncRoot = new();

    public static string LogPath => Path.Combine(ResolveLogDirectory(), "startup.log");

    public static void Mark(string phase)
        => Write($"{DateTimeOffset.UtcNow:O} INFO {phase}");

    public static void Record(string source, Exception exception)
        => Write($"{DateTimeOffset.UtcNow:O} ERROR {source} {exception}");

    private static string ResolveLogDirectory()
    {
        var configuredDirectory = Environment.GetEnvironmentVariable("DTUDO_WINAPP_LOG_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
            return configuredDirectory.Trim();

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WinAppDtudo.csproj")))
                return Path.Combine(directory.FullName, "Logs");

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Logs");
    }

    private static void Write(string message)
    {
        try
        {
            lock (SyncRoot)
            {
                var directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.AppendAllText(LogPath, message + Environment.NewLine);
            }
        }
        catch
        {
        }
    }
}
