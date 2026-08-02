using System.Runtime.InteropServices;

namespace WinAppDtudo.Services;

internal static class WindowsDarkMode
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const string DarkModeExplorerTheme = "DarkMode_Explorer";

    public static void ApplyTo(Form form)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) || form.IsDisposed)
            return;

        if (!form.IsHandleCreated)
        {
            form.HandleCreated += (_, _) => ApplyTo(form);
            return;
        }

        var enabled = 1;
        _ = DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));
        _ = DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref enabled, sizeof(int));
        ApplyThemeToHandle(form.Handle);
    }

    public static void ApplyTo(Control control)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) || control.IsDisposed)
            return;

        if (!control.IsHandleCreated)
        {
            control.HandleCreated -= ControlHandleCreated;
            control.HandleCreated += ControlHandleCreated;
            return;
        }

        ApplyThemeToHandle(control.Handle);
        control.Invalidate();
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? pszSubAppName, string? pszSubIdList);

    private static void ControlHandleCreated(object? sender, EventArgs e)
    {
        if (sender is Control control)
            ApplyTo(control);
    }

    private static void ApplyThemeToHandle(IntPtr handle)
        => _ = SetWindowTheme(handle, DarkModeExplorerTheme, null);
}
