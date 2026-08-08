namespace WinAppDtudo.Services;

public sealed class WindowsHealthNotificationService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon? _ownedIcon;
    private string? _lastCriticalSignature;
    private bool _disposed;

    public WindowsHealthNotificationService(Icon? applicationIcon)
    {
        _ownedIcon = applicationIcon is null ? null : new Icon(applicationIcon, applicationIcon.Size);
        _notifyIcon = new NotifyIcon
        {
            Icon = _ownedIcon ?? SystemIcons.Application,
            Text = "Dtudo - Saude",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("Abrir painel de saude");
        openItem.Click += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        var exitItem = new ToolStripMenuItem("Sair");
        exitItem.Click += (_, _) => Application.Exit();
        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);
        ThemeManager.ApplyDarkModeToContextMenuStrip(contextMenu);
        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OpenRequested;

    public void Notify(WinAppHealthSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var criticalItems = snapshot.Items
            .Where(item => item.RequiresNotification)
            .ToArray();
        if (criticalItems.Length == 0)
        {
            _lastCriticalSignature = null;
            return;
        }

        var signature = string.Join(
            "|",
            criticalItems
                .Select(item => $"{item.Category}:{item.Name}:{item.State}")
                .OrderBy(value => value, StringComparer.Ordinal));
        if (string.Equals(signature, _lastCriticalSignature, StringComparison.Ordinal))
        {
            return;
        }

        _lastCriticalSignature = signature;
        _notifyIcon.ShowBalloonTip(
            5_000,
            "Dtudo - alerta de saude",
            $"{criticalItems.Length} alerta(s) critico(s). Abra o painel para detalhes.",
            ToolTipIcon.Error);
    }

    public void Reset()
    {
        _lastCriticalSignature = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _ownedIcon?.Dispose();
    }
}
