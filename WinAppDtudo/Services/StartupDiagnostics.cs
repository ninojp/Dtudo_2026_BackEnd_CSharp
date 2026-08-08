namespace WinAppDtudo.Services;

internal static class StartupDiagnostics
{
    private static readonly object SyncRoot = new();

    public static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Dtudo2026",
        "WinAppDtudo",
        "startup.log");

    public static void Mark(string phase)
        => Write($"{DateTimeOffset.UtcNow:O} INFO {phase}");

    public static void Record(string source, Exception exception)
        => Write($"{DateTimeOffset.UtcNow:O} ERROR {source} {exception.GetType().FullName}: {exception.Message}");

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
