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
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.Dark);

        // Inicializa o gerenciador de temas (detecta Dark Mode do Windows 11)
        ThemeManager.Initialize();

        Application.Run(new Frm_WinAppDtudo());
    }
}
