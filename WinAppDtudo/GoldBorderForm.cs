using System.Runtime.InteropServices;

namespace WinAppDtudo;

public class GoldBorderForm : Form
{
    private const int DwmwaBorderColor = 34;

    public GoldBorderForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyNativeBorderColor();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyNativeBorderColor();
    }

    protected override void OnStyleChanged(EventArgs e)
    {
        base.OnStyleChanged(e);
        ApplyNativeBorderColor();
    }

    private void ApplyNativeBorderColor()
    {
        if (!IsHandleCreated || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            return;

        var borderColor = ColorTranslator.ToWin32(Color.Gold);
        _ = DwmSetWindowAttribute(Handle, DwmwaBorderColor, ref borderColor, sizeof(int));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hWnd,
        int attribute,
        ref int value,
        int valueSize);
}
