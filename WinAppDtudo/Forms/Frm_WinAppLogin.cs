using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public sealed class Frm_WinAppLogin : Form
{
    private readonly Uri _authorizationUri;
    private readonly Uri _redirectUri;
    private readonly WebView2 _webView = new();
    private readonly Label _statusLabel = new();
    private bool _callbackNavigationStarted;

    public Frm_WinAppLogin(Uri authorizationUri, Uri redirectUri)
    {
        ArgumentNullException.ThrowIfNull(authorizationUri);
        ArgumentNullException.ThrowIfNull(redirectUri);

        _authorizationUri = authorizationUri;
        _redirectUri = redirectUri;

        Text = "Entrar no WinAppDtudo";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 900);
        MinimumSize = new Size(640, 700);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;

        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = 48;
        _statusLabel.Padding = new Padding(18, 11, 18, 8);
        _statusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _statusLabel.Text = "Autenticacao segura | WinAppDtudo";

        _webView.Dock = DockStyle.Fill;
        _webView.DefaultBackgroundColor = DarkModeColors.BackgroundColor;

        Controls.Add(_webView);
        Controls.Add(_statusLabel);
        ThemeManager.ApplyDarkModeToForm(this);
        _statusLabel.BackColor = Color.FromArgb(25, 25, 25);
        _statusLabel.ForeColor = DarkModeColors.WarningColor;
        Load += async (_, _) => await InitializeWebViewAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Dtudo2026",
                "WinAppDtudo",
                "WinAppLoginWebView2");
            var environment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: userDataFolder);
            await _webView.EnsureCoreWebView2Async(environment);

            var coreWebView = _webView.CoreWebView2
                ?? throw new InvalidOperationException("O WebView2 nao foi inicializado.");
            coreWebView.Settings.AreDefaultContextMenusEnabled = false;
            coreWebView.Settings.AreDevToolsEnabled = false;
            coreWebView.Settings.IsStatusBarEnabled = false;
            coreWebView.Settings.IsZoomControlEnabled = true;
            coreWebView.NavigationStarting += WebView_NavigationStarting;
            coreWebView.NavigationCompleted += WebView_NavigationCompleted;
            coreWebView.NewWindowRequested += WebView_NewWindowRequested;
            _statusLabel.Text = "Entre com a conta Superadministrador para continuar.";
            coreWebView.Navigate(_authorizationUri.AbsoluteUri);
        }
        catch (WebView2RuntimeNotFoundException exception)
        {
            ShowInitializationFailure(
                "O Microsoft Edge WebView2 Runtime nao foi encontrado.",
                exception.Message);
        }
        catch (Exception exception)
        {
            ShowInitializationFailure(
                "Nao foi possivel abrir a janela de login.",
                exception.Message);
        }
    }

    private void WebView_NavigationStarting(
        object? sender,
        CoreWebView2NavigationStartingEventArgs eventArgs)
    {
        if (!Uri.TryCreate(eventArgs.Uri, UriKind.Absolute, out var navigationUri)
            || !IsCallbackUri(navigationUri))
        {
            return;
        }

        _callbackNavigationStarted = true;
        _statusLabel.Text = "Login concluido. Finalizando a sessao segura...";
    }

    private void WebView_NavigationCompleted(
        object? sender,
        CoreWebView2NavigationCompletedEventArgs eventArgs)
    {
        if (!_callbackNavigationStarted || IsDisposed)
        {
            return;
        }

        BeginInvoke(CloseSuccessfulLogin);
    }

    private void WebView_NewWindowRequested(
        object? sender,
        CoreWebView2NewWindowRequestedEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        if (Uri.TryCreate(eventArgs.Uri, UriKind.Absolute, out var requestedUri))
        {
            _webView.CoreWebView2?.Navigate(requestedUri.AbsoluteUri);
        }
    }

    private bool IsCallbackUri(Uri navigationUri) =>
        string.Equals(navigationUri.Scheme, _redirectUri.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(navigationUri.Host, _redirectUri.Host, StringComparison.OrdinalIgnoreCase)
        && navigationUri.Port == _redirectUri.Port
        && string.Equals(navigationUri.AbsolutePath, _redirectUri.AbsolutePath, StringComparison.Ordinal)
        && string.IsNullOrEmpty(navigationUri.UserInfo)
        && string.IsNullOrEmpty(navigationUri.Fragment)
        && !string.IsNullOrEmpty(navigationUri.Query);

    private void CloseSuccessfulLogin()
    {
        if (IsDisposed)
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowInitializationFailure(string title, string details)
    {
        if (IsDisposed)
        {
            return;
        }

        _statusLabel.Text = title;
        _webView.Visible = false;
        Controls.Add(new Label
        {
            BackColor = DarkModeColors.BackgroundColor,
            Dock = DockStyle.Fill,
            ForeColor = DarkModeColors.TextColor,
            Padding = new Padding(24),
            Text = $"{title}\r\n\r\n{details}\r\n\r\nFeche esta janela e tente novamente.",
            TextAlign = ContentAlignment.MiddleCenter
        });
    }
}
