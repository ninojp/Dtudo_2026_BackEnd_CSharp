using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

/// <summary>
/// Conteudo de uma aba de navegacao do DtudoSite (WebView2). Cada pagina web ocupa a sua propria aba
/// da janela principal; "Nova guia" e links que abrem nova janela pedem uma nova aba ao host.
/// </summary>
public sealed class FUC_DtudoWebView : UserControl
{
    private const int MaxTitleLength = 40;
    private const string DefaultTitle = "DtudoSite";

    private readonly Uri _startUri;
    private readonly ToolStrip _navigationStrip = new();
    private readonly ToolStripButton _backButton;
    private readonly ToolStripButton _forwardButton;
    private readonly ToolStripButton _refreshButton;
    private readonly ToolStripButton _newTabButton;
    private readonly CancellationTokenSource _lifetime = new();
    private WebView2? _webView;
    private bool _initialized;

    public FUC_DtudoWebView(Uri startUri)
    {
        ArgumentNullException.ThrowIfNull(startUri);
        if (!startUri.IsAbsoluteUri)
        {
            throw new ArgumentException("The DtudoSite URL must be absolute.", nameof(startUri));
        }

        _startUri = startUri;
        Name = "FUC_DtudoWebView";
        Dock = DockStyle.Fill;

        _backButton = CreateNavigationButton("Voltar", (_, _) => GoBack());
        _forwardButton = CreateNavigationButton("Avancar", (_, _) => GoForward());
        _refreshButton = CreateNavigationButton("Atualizar", (_, _) => _webView?.CoreWebView2?.Reload());
        _newTabButton = CreateNavigationButton("Nova guia", (_, _) => NewTabRequested?.Invoke(this, _startUri));

        _navigationStrip.GripStyle = ToolStripGripStyle.Hidden;
        _navigationStrip.Dock = DockStyle.Top;
        _navigationStrip.Items.AddRange([
            _backButton,
            _forwardButton,
            _refreshButton,
            new ToolStripSeparator(),
            _newTabButton
        ]);

        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.Black
        };

        Controls.Add(_webView);
        Controls.Add(_navigationStrip);
        ThemeManager.ApplyDarkModeToUserControl(this);
        BackColor = Color.Black;
        UpdateNavigationControls();

        Load += async (_, _) => await InitializeBrowserAsync();
    }

    /// <summary>Disparado quando o titulo do documento muda (titulo curto, titulo completo).</summary>
    public event EventHandler<WebViewTitleChangedEventArgs>? TitleChanged;

    /// <summary>Disparado quando o usuario pede nova guia ou a pagina abre uma nova janela.</summary>
    public event EventHandler<Uri>? NewTabRequested;

    public static string FormatTitle(string? title)
    {
        var normalized = string.IsNullOrWhiteSpace(title) ? DefaultTitle : title.Trim();
        return normalized.Length <= MaxTitleLength ? normalized : normalized[..(MaxTitleLength - 3)] + "...";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task InitializeBrowserAsync()
    {
        if (_initialized || IsDisposed || _webView is null)
        {
            return;
        }

        _initialized = true;
        var webView = _webView;
        try
        {
            await webView.EnsureCoreWebView2Async();
            _lifetime.Token.ThrowIfCancellationRequested();

            ConfigureWebView(webView);
            webView.CoreWebView2.Navigate(_startUri.AbsoluteUri);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (IsDisposed)
        {
        }
        catch (WebView2RuntimeNotFoundException exception)
        {
            ShowFailure("WebView2 Runtime nao encontrado.", exception.Message);
        }
        catch (Exception exception)
        {
            ShowFailure("Nao foi possivel iniciar o navegador.", exception.Message);
        }
    }

    private void ConfigureWebView(WebView2 webView)
    {
        var coreWebView = webView.CoreWebView2
            ?? throw new InvalidOperationException("WebView2 initialization did not produce a browser instance.");

        coreWebView.Settings.AreDefaultContextMenusEnabled = true;
        coreWebView.Settings.AreDevToolsEnabled = true;
        coreWebView.Settings.IsStatusBarEnabled = false;
        coreWebView.Settings.IsZoomControlEnabled = true;
        coreWebView.DocumentTitleChanged += (_, _) => RaiseTitleChanged(coreWebView.DocumentTitle);
        coreWebView.HistoryChanged += (_, _) => UpdateNavigationControls();
        coreWebView.NavigationCompleted += (_, eventArgs) =>
        {
            if (!eventArgs.IsSuccess)
            {
                RaiseTitleChanged("Erro de navegacao", $"Navigation error: {eventArgs.WebErrorStatus}");
            }

            UpdateNavigationControls();
        };
        coreWebView.NewWindowRequested += (_, eventArgs) =>
        {
            eventArgs.Handled = true;
            if (Uri.TryCreate(eventArgs.Uri, UriKind.Absolute, out var requestedUri))
            {
                NewTabRequested?.Invoke(this, requestedUri);
            }
        };
    }

    private void RaiseTitleChanged(string? title, string? tooltip = null)
    {
        if (IsDisposed)
        {
            return;
        }

        var full = string.IsNullOrWhiteSpace(title) ? DefaultTitle : title.Trim();
        TitleChanged?.Invoke(this, new WebViewTitleChangedEventArgs(FormatTitle(full), tooltip ?? full));
    }

    private void GoBack()
    {
        if (_webView?.CoreWebView2 is { CanGoBack: true } coreWebView)
        {
            coreWebView.GoBack();
        }
    }

    private void GoForward()
    {
        if (_webView?.CoreWebView2 is { CanGoForward: true } coreWebView)
        {
            coreWebView.GoForward();
        }
    }

    private void ShowFailure(string title, string details)
    {
        if (IsDisposed || _webView is null)
        {
            return;
        }

        Controls.Remove(_webView);
        _webView.Dispose();
        _webView = null;
        RaiseTitleChanged("Erro", details);
        Controls.Add(new Label
        {
            BackColor = Color.Black,
            Dock = DockStyle.Fill,
            ForeColor = Color.Gold,
            Padding = new Padding(32),
            Text = $"{title}\r\n\r\n{details}\r\n\r\nInstale ou repare o Microsoft Edge WebView2 Runtime e tente novamente.",
            TextAlign = ContentAlignment.MiddleCenter
        });
        _navigationStrip.BringToFront();
        UpdateNavigationControls();
    }

    private void UpdateNavigationControls()
    {
        if (IsDisposed)
        {
            return;
        }

        var coreWebView = _webView?.CoreWebView2;
        _backButton.Enabled = coreWebView?.CanGoBack == true;
        _forwardButton.Enabled = coreWebView?.CanGoForward == true;
        _refreshButton.Enabled = coreWebView is not null;
    }

    private static ToolStripButton CreateNavigationButton(string text, EventHandler clickHandler)
    {
        ToolStripButton button = new(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ToolTipText = text
        };
        button.Click += clickHandler;
        return button;
    }
}

public sealed class WebViewTitleChangedEventArgs(string title, string toolTip) : EventArgs
{
    public string Title { get; } = title;

    public string ToolTip { get; } = toolTip;
}
