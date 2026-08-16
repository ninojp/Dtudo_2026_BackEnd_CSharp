using WinAppDtudo.Forms;
using WinAppDtudo.Services;

namespace WinAppDtudo;

/// <summary>
/// Frm_WinAppDtudo é a classe principal do aplicativo WinForms, representando o formulário principal da aplicação Dtudo.
/// </summary>
public partial class Frm_WinAppDtudo : CustomFormNoBorder
{
    private const float DesignWidth = 1272F;
    private const float DesignContentHeight = 652F;
    private readonly WinAppAuthenticationService _identityAuthenticationService;
    private readonly ApiFileStorageStartupService _apiFileStorageStartupService = new();
    private readonly ApiMyAnimesHealthCheckService _apiMyAnimesHealthCheckService;
    private readonly ApiMyAnimesStartupService _apiMyAnimesStartupService;
    private readonly ApiMusicXHealthCheckService _apiMusicXHealthCheckService;
    private readonly ApiMusicXStartupService _apiMusicXStartupService;
    private readonly ApiMyAnimeListStartupService _apiMyAnimeListStartupService = new();
    private readonly ApiDiscogsStartupService _apiDiscogsStartupService = new();
    private readonly WinAppHealthMonitoringService _healthMonitoringService;
    private readonly CancellationTokenSource _formClosingCancellationTokenSource = new();
    private readonly DtudoSiteStartupService _dtudoSiteStartupService;
    private readonly System.Windows.Forms.Timer _healthRefreshTimer = new() { Interval = 60_000 };
    private WindowsHealthNotificationService _healthNotificationService = null!;
    private Frm_DtudoSiteBrowser? _dtudoSiteBrowser;
    private Frm_HealthDashboard? _healthDashboard;
    private WinAppHealthSnapshot? _lastHealthSnapshot;
    private bool _isApplyingMainLayout;
    private bool _isOpeningDtudoSite;
    private bool _isRefreshingHealth;

    public Frm_WinAppDtudo()
    {
        StartupDiagnostics.Mark("Frm constructor entered");
        StartupDiagnostics.Mark("Before InitializeComponent");
        InitializeComponent();
        StartupDiagnostics.Mark("After InitializeComponent");
        _identityAuthenticationService = new WinAppAuthenticationService(
            browserClient: new WinAppPkceBrowserClient(
                browserLauncher: OpenWinAppLoginAsync));
        _apiMyAnimesHealthCheckService = new ApiMyAnimesHealthCheckService(_identityAuthenticationService);
        _apiMyAnimesStartupService = new ApiMyAnimesStartupService(_apiMyAnimesHealthCheckService);
        _apiMusicXHealthCheckService = new ApiMusicXHealthCheckService(_identityAuthenticationService);
        _apiMusicXStartupService = new ApiMusicXStartupService(_apiMusicXHealthCheckService);
        _healthMonitoringService = new WinAppHealthMonitoringService(_identityAuthenticationService);
        _healthNotificationService = new WindowsHealthNotificationService(Icon);
        _healthNotificationService.OpenRequested += HealthNotificationService_OpenRequested;
        _healthRefreshTimer.Tick += HealthRefreshTimer_Tick;
        _dtudoSiteStartupService = new DtudoSiteStartupService(_apiMyAnimesHealthCheckService);
        StartupDiagnostics.Mark("After startup services construction");
        // Aplicar o tema Dark Mode ao formulário e seus componentes
        StartupDiagnostics.Mark("Before ThemeManager.ApplyDarkModeToForm");
        ThemeManager.ApplyDarkModeToForm(this);
        StartupDiagnostics.Mark("After ThemeManager.ApplyDarkModeToForm");
        // Inicializa o formulário customizado sem barra de título
        InitializeCustomFormNoBorder(Mnu_Principal);
        AddControlButtonsToMenuStrip(Mnu_Principal);
        StartupDiagnostics.Mark("After custom form initialization");

        //Opções de inicialização do formulário, após a inicialização dos componentes.
        MnI_MyAnimes.Enabled = false;
        MnI_MyMusicX.Enabled = false;
        MnI_NinoTI.Enabled = false;
        MnI_Desconectar.Enabled = false;
        MnI_CadastrarUsuario.Enabled = false;
        MnI_Saude.Enabled = false;

        InitializeMainLayout();
        StartupDiagnostics.Mark("After main layout initialization");
        FormClosing += Frm_WinAppDtudo_FormClosing;
        FormClosed += Frm_WinAppDtudo_FormClosed;
    }
    //=========================================================
    //Menu MyAnimes - Abrir formulário Frm_MyAnimes.
    private void MnI_MyAnimes_Click(object sender, EventArgs e)
    {
        Frm_MyAnimes formMyAnimes = new(_identityAuthenticationService);
        formMyAnimes.Show();
    }
    //Menu MyMusicX - Abrir formulário Frm_MyMusicX.
    private void MnI_MyMusicX_Click(object sender, EventArgs e)
    {
        Frm_MyMusicX formMyMusicX = new(_identityAuthenticationService);
        formMyMusicX.Show();
    }
    //Menu NinoTI - Abrir formulário Frm_NinoTI.
    private void MnI_NinoTI_Click(object sender, EventArgs e)
    {
        Frm_NinoTI formNinoTI = new();
        formNinoTI.Show();
    }
    //==============================================================
    //Menu Cadastrar Usuario - abrir a administracao protegida do Identity.
    private void MnI_CadastrarUsuario_Click(object sender, EventArgs e)
    {
        using var formAdministration = new Frm_IdentityAdministration(_identityAuthenticationService);
        formAdministration.ShowDialog(this);
    }
    //Menu Conectar - iniciar autenticacao dentro do WinAppDtudo.
    private async void MnI_Conectar_Click(object sender, EventArgs e)
    {
        if (_identityAuthenticationService.IsAuthenticated)
        {
            return;
        }

        try
        {
            UseWaitCursor = true;
            var session = await _identityAuthenticationService.SignInAsync(
                _formClosingCancellationTokenSource.Token);
            SetAuthenticatedUi(true);
            _healthRefreshTimer.Start();
            await EnsureMonitoredServicesReadyAsync(_formClosingCancellationTokenSource.Token);
            await RefreshHealthAsync(notify: true);
            DarkMessageBox.Show(
                $"Login realizado no WinAppDtudo.\nSessao: {session.SessionId:D}",
                "WinAppDtudo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (OperationCanceledException) when (_formClosingCancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (WinAppAuthenticationException exception)
        {
            DarkMessageBox.Show(
                exception.Message,
                "Falha na autenticacao",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (HttpRequestException exception)
        {
            DarkMessageBox.Show(
                $"Nao foi possivel conectar ao ApiIdentity em {AppConfigurationService.ApiIdentityBaseUrl}.\n\n{exception.Message}",
                "Erro de conexao",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private Task OpenWinAppLoginAsync(Uri authorizationUri)
    {
        using var loginForm = new Frm_WinAppLogin(
            authorizationUri,
            AppConfigurationService.IdentityRedirectUri);
        var result = loginForm.ShowDialog(this);
        if (result != DialogResult.OK)
        {
            throw new WinAppAuthenticationException(
                "A autenticacao do WinAppDtudo foi cancelada.");
        }

        return Task.CompletedTask;
    }
    //Menu Desconectar - revogar a sessao atual e limpar o armazenamento local.
    private async void MnI_Desconectar_Click(object sender, EventArgs e)
    {
        Frm_Questao formQuestao = new("InterrogacaoBrasil", "Deseja realmente se desconectar?");
        var resultado = formQuestao.ShowDialog();
        if (resultado == DialogResult.OK)
        {
            try
            {
                UseWaitCursor = true;
                var revoked = await _identityAuthenticationService.SignOutAsync(
                    _formClosingCancellationTokenSource.Token);
                ClearAuthenticatedUi();

                DarkMessageBox.Show(
                    revoked
                        ? "Sessao encerrada e revogada no Identity."
                        : "Sessao local encerrada. A revogacao remota nao foi confirmada.",
                    "Identity",
                    MessageBoxButtons.OK,
                    revoked ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (OperationCanceledException) when (_formClosingCancellationTokenSource.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                ClearAuthenticatedUi();
                DarkMessageBox.Show(
                    $"A sessao local nao pode ser encerrada com seguranca.\n\n{exception.Message}",
                    "Falha ao desconectar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }
        else if (resultado == DialogResult.Cancel)
        { DarkMessageBox.Show("Desconexao cancelada."); }
    }

    private void SetAuthenticatedUi(bool isAuthenticated)
    {
        MnI_Conectar.Enabled = !isAuthenticated;
        MnI_CadastrarUsuario.Enabled = isAuthenticated;
        MnI_MyAnimes.Enabled = isAuthenticated;
        MnI_MyMusicX.Enabled = isAuthenticated;
        MnI_NinoTI.Enabled = isAuthenticated;
        MnI_Desconectar.Enabled = isAuthenticated;
        MnI_Saude.Enabled = isAuthenticated;
    }

    private void ClearAuthenticatedUi()
    {
        SetAuthenticatedUi(false);
        _healthRefreshTimer.Stop();
        _lastHealthSnapshot = null;
        _healthNotificationService.Reset();
        foreach (Form form in Application.OpenForms.Cast<Form>().ToList())
        {
            if (form != this)
            {
                form.Close();
            }
        }
    }

    private async void MnI_Saude_Click(object? sender, EventArgs e)
    {
        await OpenHealthDashboardAsync();
    }

    private async Task OpenHealthDashboardAsync()
    {
        if (!_identityAuthenticationService.IsAuthenticated || IsDisposed)
        {
            return;
        }

        if (_healthDashboard is null || _healthDashboard.IsDisposed)
        {
            _healthDashboard = new Frm_HealthDashboard(_healthMonitoringService);
            _healthDashboard.FormClosed += HealthDashboard_FormClosed;
            _healthDashboard.Show(this);
        }
        else
        {
            if (_healthDashboard.WindowState == FormWindowState.Minimized)
            {
                _healthDashboard.WindowState = FormWindowState.Normal;
            }

            _healthDashboard.Activate();
        }

        if (_lastHealthSnapshot is not null)
        {
            _healthDashboard.ApplySnapshot(_lastHealthSnapshot);
        }

        await _healthDashboard.RefreshAsync(_formClosingCancellationTokenSource.Token);
    }

    private async void HealthRefreshTimer_Tick(object? sender, EventArgs e)
    {
        await RefreshHealthAsync(notify: true);
    }

    private async Task RefreshHealthAsync(bool notify)
    {
        if (_isRefreshingHealth || !_identityAuthenticationService.IsAuthenticated || IsDisposed)
        {
            return;
        }

        _isRefreshingHealth = true;
        try
        {
            var snapshot = await _healthMonitoringService.CheckAsync(
                _formClosingCancellationTokenSource.Token);
            _lastHealthSnapshot = snapshot;
            if (!_identityAuthenticationService.IsAuthenticated)
            {
                ClearAuthenticatedUi();
                return;
            }

            if (notify)
            {
                _healthNotificationService.Notify(snapshot);
            }

            if (_healthDashboard is not null && !_healthDashboard.IsDisposed)
            {
                _healthDashboard.ApplySnapshot(snapshot);
            }
        }
        catch (OperationCanceledException) when (_formClosingCancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (WinAppAuthenticationException)
        {
            ClearAuthenticatedUi();
        }
        finally
        {
            _isRefreshingHealth = false;
        }
    }

    private async Task EnsureMonitoredServicesReadyAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            TryEnsureServiceReadyAsync(
                "ApiMyAnimes",
                () => _apiMyAnimesStartupService.EnsureReadyAsync(cancellationToken),
                cancellationToken),
            TryEnsureServiceReadyAsync(
                "ApiMusicX",
                () => _apiMusicXStartupService.EnsureReadyAsync(cancellationToken),
                cancellationToken),
            TryEnsureServiceReadyAsync(
                "ApiMyAnimeList",
                () => _apiMyAnimeListStartupService.EnsureReadyAsync(cancellationToken),
                cancellationToken),
            TryEnsureServiceReadyAsync(
                "ApiDiscogs",
                () => _apiDiscogsStartupService.EnsureReadyAsync(cancellationToken),
                cancellationToken),
            TryEnsureServiceReadyAsync(
                "ApiFileStorage",
                () => _apiFileStorageStartupService.EnsureReadyAsync(cancellationToken),
                cancellationToken));
    }

    private static async Task TryEnsureServiceReadyAsync(
        string serviceName,
        Func<Task> ensureReady,
        CancellationToken cancellationToken)
    {
        try
        {
            await ensureReady();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record($"{serviceName} startup", exception);
        }
    }

    private void HealthNotificationService_OpenRequested(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(OpenHealthDashboardFromNotification);
    }

    private async void OpenHealthDashboardFromNotification()
    {
        await OpenHealthDashboardAsync();
    }

    private void HealthDashboard_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (ReferenceEquals(sender, _healthDashboard))
        {
            _healthDashboard = null;
        }
    }
    //Menu Sair - Fechar a aplicação Toda.
    private void MnI_Sair_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }
    //===========================================================================================
    //Captura o evento MouseDown do formulário Frm_WinAppDtudo e exibe um menu de contexto ao clicar com o botão direito do mouse.
    private void Frm_WinAppDtudo_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            //string message = $"MouseDown, na posição ({e.X}, {e.Y}) com o botão {e.Button}";
            //WinAppDtudo.Services.DarkMessageBox.Show(message);
            ContextMenuStrip contextMenu = new();
            ToolStripMenuItem menuFlutuanteItem1 = CriaMenuFlutuanteItem("Opção 1", "CaveraMetal");
            ToolStripMenuItem menuFlutuanteItem2 = CriaMenuFlutuanteItem("Opção 2", "CaveraMetal");
            ToolStripMenuItem menuFlutuanteItem3 = CriaMenuFlutuanteItem("Opção 3", "CaveraMetal");
            contextMenu.Items.Add(menuFlutuanteItem1);
            contextMenu.Items.Add(menuFlutuanteItem2);
            contextMenu.Items.Add(menuFlutuanteItem3);
            contextMenu.Show(this, new Point(e.X, e.Y));
            menuFlutuanteItem1.Click += new EventHandler(MenuFlutuanteItem1_Click);
            menuFlutuanteItem2.Click += new EventHandler(MenuFlutuanteItem2_Click);
            menuFlutuanteItem3.Click += new EventHandler(MenuFlutuanteItem3_Click);
        }
    }
    private static ToolStripMenuItem CriaMenuFlutuanteItem(string textMenuItem, string imageName)
    {
        ToolStripMenuItem menuFlutuanteItem = new(textMenuItem);
        if (Properties.Resources.ResourceManager.GetObject(imageName) is Image imgMenuItem)
        { menuFlutuanteItem.Image = imgMenuItem; }
        return menuFlutuanteItem;
    }
    void MenuFlutuanteItem1_Click(object? sender, EventArgs e)
    {
        WinAppDtudo.Services.DarkMessageBox.Show("Opção 1 selecionada");
    }
    void MenuFlutuanteItem2_Click(object? sender, EventArgs e)
    {
        WinAppDtudo.Services.DarkMessageBox.Show("Opção 2 selecionada");
    }
    void MenuFlutuanteItem3_Click(object? sender, EventArgs e)
    {
        WinAppDtudo.Services.DarkMessageBox.Show("Opção 3 selecionada");
    }
    //=================================================================
    private async void Btn_DtudoSite_Click(object sender, EventArgs e)
    {
        await ExecuteDtudoSiteActionAsync(OpenDtudoSiteInGoogleChromeAsync);
    }

    private async void MnI_DtudoSite_Click(object sender, EventArgs e)
    {
        await ExecuteDtudoSiteActionAsync(OpenDtudoSiteInWebViewAsync);
    }

    private async Task ExecuteDtudoSiteActionAsync(Func<CancellationToken, Task> action)
    {
        if (_isOpeningDtudoSite || IsDisposed)
            return;

        _isOpeningDtudoSite = true;
        Btn_DtudoSite.Enabled = false;
        MnI_DtudoSite.Enabled = false;
        var wasUsingWaitCursor = UseWaitCursor;
        UseWaitCursor = true;

        try
        {
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                _formClosingCancellationTokenSource.Token);
            await action(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (_formClosingCancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!IsDisposed)
            {
                DarkMessageBox.Show(
                    $"Nao foi possivel abrir o DtudoSite.\n\n{exception.Message}",
                    "Erro ao abrir DtudoSite",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                UseWaitCursor = wasUsingWaitCursor;
                Btn_DtudoSite.Enabled = true;
                MnI_DtudoSite.Enabled = true;
            }

            _isOpeningDtudoSite = false;
        }
    }

    private async Task OpenDtudoSiteInGoogleChromeAsync(CancellationToken cancellationToken)
    {
        if (!TryGetDtudoSiteStartUri(out var startUri))
            return;

        var startupResult = await _dtudoSiteStartupService.EnsureReadyAsync(startUri, cancellationToken);
        if (!startupResult.Succeeded)
        {
            ShowDtudoSiteFailure("Servicos locais indisponiveis", startupResult.Message);
            return;
        }

        var chromeResult = _dtudoSiteStartupService.OpenInGoogleChrome(startUri);
        if (!chromeResult.Succeeded)
            ShowDtudoSiteFailure("Google Chrome indisponivel", chromeResult.Message);
    }

    private async Task OpenDtudoSiteInWebViewAsync(CancellationToken cancellationToken)
    {
        if (!TryGetDtudoSiteStartUri(out var startUri))
            return;

        var startupResult = await _dtudoSiteStartupService.EnsureReadyAsync(startUri, cancellationToken);
        if (!startupResult.Succeeded)
        {
            ShowDtudoSiteFailure("Servicos locais indisponiveis", startupResult.Message);
            return;
        }

        if (_dtudoSiteBrowser is null || _dtudoSiteBrowser.IsDisposed)
        {
            _dtudoSiteBrowser = new Frm_DtudoSiteBrowser(startUri);
            _dtudoSiteBrowser.FormClosed += DtudoSiteBrowser_FormClosed;
            _dtudoSiteBrowser.Show(this);
        }
        else
        {
            if (_dtudoSiteBrowser.WindowState == FormWindowState.Minimized)
                _dtudoSiteBrowser.WindowState = FormWindowState.Normal;

            _dtudoSiteBrowser.Activate();
        }

        await _dtudoSiteBrowser.OpenNewTabAsync(startUri, cancellationToken);
    }

    private bool TryGetDtudoSiteStartUri(out Uri startUri)
    {
        if (Uri.TryCreate(AppConfigurationService.DtudoSiteStartUrl, UriKind.Absolute, out var parsedUri)
            && parsedUri is not null)
        {
            startUri = parsedUri;
            return true;
        }

        startUri = null!;
        ShowDtudoSiteFailure("Configuracao invalida", "A URL configurada para o DtudoSite e invalida.");
        return false;
    }

    private void ShowDtudoSiteFailure(string title, string message)
    {
        if (!IsDisposed)
            DarkMessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void DtudoSiteBrowser_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (ReferenceEquals(sender, _dtudoSiteBrowser))
            _dtudoSiteBrowser = null;
    }

    private void Frm_WinAppDtudo_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _formClosingCancellationTokenSource.Cancel();
    }

    private void Frm_WinAppDtudo_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _healthRefreshTimer.Stop();
        _healthRefreshTimer.Dispose();
        _healthDashboard?.Close();
        _healthNotificationService.Dispose();
        _healthMonitoringService.Dispose();
        _dtudoSiteStartupService.Dispose();
        _identityAuthenticationService.Dispose();
    }

    private void Btn_MyAnimesForm_Click(object sender, EventArgs e)
    {
        Frm_MyAnimes formMyAnimes = new(_identityAuthenticationService);
        formMyAnimes.Show();
    }

    private void Btn_NinoTIForm_Click(object sender, EventArgs e)
    {
        Frm_NinoTI formNinoTI = new();
        formNinoTI.Show();
    }

    private void Btn_MyMusicxForm_Click(object sender, EventArgs e)
    {
        Frm_MyMusicX formMyMusicX = new();
        formMyMusicX.Show();
    }

    private void InitializeMainLayout()
    {
        ConfigureLayoutControl(Lbl_Titulo);
        ConfigureLayoutControl(Btn_DtudoSite);
        ConfigureLayoutControl(Btn_MyAnimesForm);
        ConfigureLayoutControl(Btn_MyMusicxForm);
        ConfigureLayoutControl(Btn_NinoTIForm);
        ConfigureLayoutControl(Lbl_DescricaoMyMusicX);
        ConfigureLayoutControl(Lbl_DescricaoMyAnimes);
        ConfigureLayoutControl(label1);
        ConfigureLayoutControl(label2);

        Resize += Frm_WinAppDtudo_Resize;
        DpiChanged += Frm_WinAppDtudo_DpiChanged;
        Shown += Frm_WinAppDtudo_Shown;
        ApplyMainLayout();
    }

    private static void ConfigureLayoutControl(Control control)
    {
        control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        control.Dock = DockStyle.None;
    }

    private void Frm_WinAppDtudo_Resize(object? sender, EventArgs e)
    {
        ApplyMainLayout();
    }

    private void Frm_WinAppDtudo_DpiChanged(object? sender, EventArgs e)
    {
        ApplyMainLayout();
    }

    private async void Frm_WinAppDtudo_Shown(object? sender, EventArgs e)
    {
        ApplyMainLayout();
        try
        {
            await _apiFileStorageStartupService.EnsureReadyAsync(
                _formClosingCancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (_formClosingCancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (WinAppAuthenticationException exception)
        {
            StartupDiagnostics.Record("ApiFileStorage startup", exception);
        }
    }

    private void ApplyMainLayout()
    {
        if (_isApplyingMainLayout || IsDisposed || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        _isApplyingMainLayout = true;
        SuspendLayout();
        try
        {
            var contentTop = Mnu_Principal.Visible ? Mnu_Principal.Bottom : 0;
            var contentWidth = ClientSize.Width;
            var contentHeight = Math.Max(1, ClientSize.Height - contentTop);
            var scale = Math.Min(contentWidth / DesignWidth, contentHeight / DesignContentHeight);
            var offsetX = (contentWidth - DesignWidth * scale) / 2F;
            var offsetY = contentTop + (contentHeight - DesignContentHeight * scale) / 2F;

            SetScaledBounds(Lbl_Titulo, 801F, 12F, 296F, 90F, scale, offsetX, offsetY);
            SetScaledBounds(Btn_DtudoSite, 280F, 85F, 242F, 86F, scale, offsetX, offsetY);
            SetScaledBounds(label2, 333F, 51F, 157F, 42F, scale, offsetX, offsetY);
            SetScaledBounds(Btn_NinoTIForm, 1044F, 233F, 144F, 120F, scale, offsetX, offsetY);
            SetScaledBounds(label1, 1058F, 204F, 111F, 37F, scale, offsetX, offsetY);
            SetScaledBounds(Btn_MyMusicxForm, 86F, 322F, 85F, 213F, scale, offsetX, offsetY);
            SetScaledBounds(Lbl_DescricaoMyMusicX, 42F, 537F, 160F, 53F, scale, offsetX, offsetY);
            SetScaledBounds(Btn_MyAnimesForm, 532F, 420F, 272F, 232F, scale, offsetX, offsetY);
            SetScaledBounds(Lbl_DescricaoMyAnimes, 588F, 385F, 157F, 46F, scale, offsetX, offsetY);
        }
        finally
        {
            ResumeLayout(false);
        }

        _isApplyingMainLayout = false;
    }

    private static void SetScaledBounds(
        Control control,
        float x,
        float y,
        float width,
        float height,
        float scale,
        float offsetX,
        float offsetY)
    {
        var bounds = new Rectangle(
            (int)Math.Round(offsetX + x * scale),
            (int)Math.Round(offsetY + y * scale),
            Math.Max(1, (int)Math.Round(width * scale)),
            Math.Max(1, (int)Math.Round(height * scale)));

        if (control.Bounds != bounds)
            control.Bounds = bounds;
    }

}
