using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public sealed class Frm_DtudoSiteBrowser : CustomFormNoBorder
{
    private readonly Uri _startUri;
    private readonly MenuStrip _menuStrip = new();
    private readonly ToolStrip _navigationStrip = new();
    private readonly TabControl _tabs = new();
    private readonly ToolStripButton _backButton;
    private readonly ToolStripButton _forwardButton;
    private readonly ToolStripButton _refreshButton;
    private readonly ToolStripButton _newTabButton;
    private readonly ToolStripButton _closeTabButton;

    public Frm_DtudoSiteBrowser(Uri startUri)
    {
        ArgumentNullException.ThrowIfNull(startUri);
        if (!startUri.IsAbsoluteUri)
            throw new ArgumentException("The DtudoSite URL must be absolute.", nameof(startUri));

        _startUri = startUri;

        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.Black;
        ForeColor = Color.Gold;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        MinimumSize = new Size(960, 640);
        Size = new Size(1500, 950);
        StartPosition = FormStartPosition.CenterParent;
        Text = "DtudoSite";

        _menuStrip.BackColor = Color.Black;
        _menuStrip.ForeColor = Color.Gold;
        _menuStrip.Items.Add(new ToolStripMenuItem("DtudoSite") { Enabled = false });

        _backButton = CreateNavigationButton("Voltar", (_, _) => GoBack());
        _forwardButton = CreateNavigationButton("Avancar", (_, _) => GoForward());
        _refreshButton = CreateNavigationButton("Atualizar", (_, _) => RefreshCurrentTab());
        _newTabButton = CreateNavigationButton("Nova guia", NewTabButton_Click);
        _closeTabButton = CreateNavigationButton("Fechar guia", (_, _) => CloseSelectedTab());

        _navigationStrip.BackColor = Color.FromArgb(26, 26, 26);
        _navigationStrip.ForeColor = Color.Gold;
        _navigationStrip.GripStyle = ToolStripGripStyle.Hidden;
        _navigationStrip.Items.AddRange([
            _backButton,
            _forwardButton,
            _refreshButton,
            new ToolStripSeparator(),
            _newTabButton,
            _closeTabButton
        ]);

        _tabs.Dock = DockStyle.Fill;
        _tabs.HotTrack = true;
        _tabs.ShowToolTips = true;
        _tabs.SelectedIndexChanged += (_, _) => UpdateNavigationControls();

        Controls.Add(_tabs);
        Controls.Add(_navigationStrip);
        Controls.Add(_menuStrip);

        InitializeCustomFormNoBorder(_menuStrip);
        AddControlButtonsToMenuStrip(_menuStrip);
        ThemeManager.ApplyDarkModeToForm(this);
        UpdateNavigationControls();
        ResumeLayout(true);
    }

    public async Task OpenNewTabAsync(Uri targetUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targetUri);
        if (!targetUri.IsAbsoluteUri)
            throw new ArgumentException("The requested URL must be absolute.", nameof(targetUri));
        if (IsDisposed || Disposing)
            return;

        var tab = new TabPage("Carregando")
        {
            BackColor = Color.Black,
            ForeColor = Color.Gold,
            ToolTipText = targetUri.AbsoluteUri
        };
        var webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.Black
        };

        tab.Controls.Add(webView);
        tab.Tag = webView;
        _tabs.TabPages.Add(tab);
        _tabs.SelectedTab = tab;
        UpdateNavigationControls();

        try
        {
            await webView.EnsureCoreWebView2Async();
            cancellationToken.ThrowIfCancellationRequested();

            ConfigureWebView(webView, tab);
            webView.CoreWebView2.Navigate(targetUri.AbsoluteUri);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RemoveTab(tab, closeWhenEmpty: true);
        }
        catch (WebView2RuntimeNotFoundException exception)
        {
            ShowFailure(tab, webView, "WebView2 Runtime nao encontrado.", exception.Message);
        }
        catch (Exception exception)
        {
            ShowFailure(tab, webView, "Nao foi possivel iniciar o navegador.", exception.Message);
        }
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

    private async void NewTabButton_Click(object? sender, EventArgs e)
    {
        await OpenNewTabAsync(_startUri, CancellationToken.None);
    }

    private void ConfigureWebView(WebView2 webView, TabPage tab)
    {
        var coreWebView = webView.CoreWebView2
            ?? throw new InvalidOperationException("WebView2 initialization did not produce a browser instance.");

        coreWebView.Settings.AreDefaultContextMenusEnabled = true;
        coreWebView.Settings.AreDevToolsEnabled = true;
        coreWebView.Settings.IsStatusBarEnabled = false;
        coreWebView.Settings.IsZoomControlEnabled = true;
        coreWebView.DocumentTitleChanged += (_, _) => UpdateTabTitle(tab, coreWebView.DocumentTitle);
        coreWebView.HistoryChanged += (_, _) => UpdateNavigationControls();
        coreWebView.NavigationCompleted += (_, eventArgs) =>
        {
            if (!eventArgs.IsSuccess)
            {
                tab.Text = "Erro de navegacao";
                tab.ToolTipText = $"Navigation error: {eventArgs.WebErrorStatus}";
            }

            UpdateNavigationControls();
        };
        coreWebView.NewWindowRequested += (_, eventArgs) =>
        {
            eventArgs.Handled = true;
            if (Uri.TryCreate(eventArgs.Uri, UriKind.Absolute, out var requestedUri))
                _ = OpenNewTabAsync(requestedUri, CancellationToken.None);
        };
    }

    private void GoBack()
    {
        if (GetActiveWebView()?.CoreWebView2 is { CanGoBack: true } coreWebView)
            coreWebView.GoBack();
    }

    private void GoForward()
    {
        if (GetActiveWebView()?.CoreWebView2 is { CanGoForward: true } coreWebView)
            coreWebView.GoForward();
    }

    private void RefreshCurrentTab()
    {
        GetActiveWebView()?.CoreWebView2?.Reload();
    }

    private void CloseSelectedTab()
    {
        if (_tabs.SelectedTab is not null)
            RemoveTab(_tabs.SelectedTab, closeWhenEmpty: true);
    }

    private void RemoveTab(TabPage tab, bool closeWhenEmpty)
    {
        if (tab.IsDisposed)
            return;

        _tabs.TabPages.Remove(tab);
        tab.Dispose();

        if (closeWhenEmpty && _tabs.TabPages.Count == 0)
        {
            Close();
            return;
        }

        UpdateNavigationControls();
    }

    private void ShowFailure(TabPage tab, WebView2 webView, string title, string details)
    {
        if (IsDisposed || tab.IsDisposed)
            return;

        tab.Controls.Remove(webView);
        webView.Dispose();
        tab.Tag = null;
        tab.Text = "Erro";
        tab.ToolTipText = details;
        tab.Controls.Add(new Label
        {
            BackColor = Color.Black,
            Dock = DockStyle.Fill,
            ForeColor = Color.Gold,
            Padding = new Padding(32),
            Text = $"{title}\r\n\r\n{details}\r\n\r\nInstale ou repare o Microsoft Edge WebView2 Runtime e tente novamente.",
            TextAlign = ContentAlignment.MiddleCenter
        });
        UpdateNavigationControls();
    }

    private void UpdateTabTitle(TabPage tab, string title)
    {
        if (tab.IsDisposed)
            return;

        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? "DtudoSite" : title.Trim();
        tab.Text = normalizedTitle.Length <= 40 ? normalizedTitle : normalizedTitle[..37] + "...";
        tab.ToolTipText = normalizedTitle;
    }

    private void UpdateNavigationControls()
    {
        if (IsDisposed)
            return;

        var coreWebView = GetActiveWebView()?.CoreWebView2;
        _backButton.Enabled = coreWebView?.CanGoBack == true;
        _forwardButton.Enabled = coreWebView?.CanGoForward == true;
        _refreshButton.Enabled = coreWebView is not null;
        _closeTabButton.Enabled = _tabs.SelectedTab is not null;
    }

    private WebView2? GetActiveWebView() => _tabs.SelectedTab?.Tag as WebView2;
}
