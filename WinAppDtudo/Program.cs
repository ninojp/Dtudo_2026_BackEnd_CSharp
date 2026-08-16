using WinAppDtudo.Services;

namespace WinAppDtudo;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        StartupDiagnostics.Mark("Main entered");
        Application.ThreadException += (_, eventArgs) => ShowStartupException("UI thread", eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
                ShowStartupException("Unhandled exception", exception);
        };

        try
        {
            StartupDiagnostics.Mark("Before ApplicationConfiguration.Initialize");
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
            StartupDiagnostics.Mark("After ApplicationConfiguration.Initialize");
        Application.SetColorMode(SystemColorMode.Dark);

        // Inicializa o gerenciador de temas (detecta Dark Mode do Windows 11)
            StartupDiagnostics.Mark("Before ThemeManager.Initialize");
        ThemeManager.Initialize();
            StartupDiagnostics.Mark("After ThemeManager.Initialize");

            StartupDiagnostics.Mark("Before Frm_WinAppDtudo construction");
            var mainForm = new Frm_WinAppDtudo();
            StartupDiagnostics.Mark("After Frm_WinAppDtudo construction");
            StartupDiagnostics.Mark("Before Application.Run");
            Application.Run(mainForm);
            StartupDiagnostics.Mark("After Application.Run");
        }
        catch (Exception exception)
        {
            ShowStartupException("Startup", exception);
        }
    }

    private static void ShowStartupException(string source, Exception exception)
    {
        StartupDiagnostics.Record(source, exception);
        try
        {
            var momentoErro = source == "Startup"
                ? "durante a inicializacao"
                : "durante a execucao";
            MessageBox.Show(
                $"O WinAppDtudo encontrou um erro {momentoErro}.\n\n{exception.Message}\n\nLog: {StartupDiagnostics.LogPath}",
                "Erro no WinAppDtudo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
        }
    }
}
