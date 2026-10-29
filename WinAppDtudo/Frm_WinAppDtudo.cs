using WinAppDtudo.Controls;
using WinAppDtudo.Forms;
using WinAppDtudo.FormsUC;
using WinAppDtudo.Services;

namespace WinAppDtudo;

/// <summary>
/// Frm_WinAppDtudo é a janela principal do WinAppDtudo. As funcionalidades (MyAnimes, Monitoramento, MyMusicX,
/// Painel de Saúde, Administração de Usuários, DtudoWebView2, Nino T.I e a aba fixa "Início" com os atalhos) abrem
/// como abas de <see cref="Tbc_Principal"/>, cada uma com seu UserControl.
/// </summary>
public partial class Frm_WinAppDtudo : CustomFormNoBorder
{
    public const string InicioTabKey = "Inicio";
    public const string MonitoramentoTabKey = "Monitoramento";
    public const string NinoTITabKey = "NinoTI";
    public const string MyMusicXTabKey = "MyMusicX";
    public const string PainelSaudeTabKey = "PainelSaude";
    public const string AdministracaoTabKey = "AdministracaoIdentity";
    public const string DtudoWebViewTabKeyPrefix = "DtudoWebView:";

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
    private readonly TabWorkspace _workspace;
    private readonly MyAnimesTabController _myAnimesController;
    private readonly FUC_Inicio _inicio = new();
    private WinAppHealthSnapshot? _lastHealthSnapshot;
    private int _webViewTabCounter;
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
        _workspace = new TabWorkspace(Tbc_Principal);
        _myAnimesController = new MyAnimesTabController(
            this,
            _workspace,
            _identityAuthenticationService,
            UpdateStatusFooter);
        _myAnimesController.AnaliseEmAndamentoAlterada += MyAnimesController_AnaliseEmAndamentoAlterada;
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
        MnI_Monitoramento.Enabled = false;
        MnI_MyAnimes.Enabled = false;
        MnI_MyMusicX.Enabled = false;
        MnI_NinoTI.Enabled = false;
        MnI_Desconectar.Enabled = false;
        MnI_CadastrarUsuario.Enabled = false;
        MnI_Saude.Enabled = false;

        InitializeHomeTab();
        StartupDiagnostics.Mark("After home tab initialization");
        Shown += Frm_WinAppDtudo_Shown;
        FormClosing += Frm_WinAppDtudo_FormClosing;
        FormClosed += Frm_WinAppDtudo_FormClosed;
    }

    /// <summary>
    /// Atualiza o texto da barra de status no rodapé, usada para reportar progresso de operações
    /// demoradas sem bloquear a interface com um diálogo modal.
    /// </summary>
    public void UpdateStatusFooter(string message)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateStatusFooter(message));
            return;
        }

        Lbl_StatusRodape.Text = message;
    }

    //=========================================================
    // Aba "Início": fundo e atalhos que antes ficavam diretamente na janela principal. Aparece somente enquanto
    // nenhuma outra aba estiver aberta (ver TabWorkspace.SetHomePage) e usa fundo preto.
    private void InitializeHomeTab()
    {
        var inicioPage = new PinnedTabPage
        {
            Name = InicioTabKey,
            Text = "Início",
            AccessibleName = "Início",
            SurfaceColor = Color.Black,
            ImageIndex = _workspace.GetImageIndex(InicioTabKey, Properties.Resources.YingYang_HD)
        };
        inicioPage.Controls.Add(_inicio);
        _workspace.SetHomePage(inicioPage);
        _inicio.DefinirAcessoLiberado(_identityAuthenticationService.IsAuthenticated);

        _inicio.DtudoSiteSolicitado += async (_, _) => await ExecuteDtudoSiteActionAsync(OpenDtudoSiteInGoogleChromeAsync);
        _inicio.MyAnimesSolicitado += (_, _) => RequireAuthentication(_myAnimesController.AbrirBuscarAnime);
        _inicio.MyMusicXSolicitado += (_, _) => RequireAuthentication(OpenMyMusicX);
        _inicio.NinoTISolicitado += (_, _) => RequireAuthentication(OpenNinoTI);
    }

    // Os atalhos da tela inicial seguem a mesma regra dos menus: só funcionam com o usuário autenticado.
    private void RequireAuthentication(Action action)
    {
        if (IsDisposed)
            return;

        if (!_identityAuthenticationService.IsAuthenticated)
        {
            DarkMessageBox.Show(
                "Conecte-se pelo menu Arquivo > Conectar para acessar esta funcionalidade.",
                "WinAppDtudo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        action();
    }

    //=========================================================
    // Menu Arquivo > Monitoramento local - aba única com o acompanhamento das estruturas.
    private void MnI_Monitoramento_Click(object? sender, EventArgs args)
    {
        if (!_identityAuthenticationService.IsAuthenticated) return;

        _workspace.OpenOrActivate(MonitoramentoTabKey, () =>
        {
            var page = new TabPage("Monitoramento local")
            {
                Name = MonitoramentoTabKey,
                ImageIndex = _workspace.GetImageIndex("Monitoramento", Properties.Resources.RadioAtivo)
            };
            page.Controls.Add(new FUC_Monitoramento(
                _identityAuthenticationService,
                openMyAnimeStructure: OpenMyAnimeStructure));
            return page;
        });
    }

    //Menu MyAnimes > Buscar Anime - abre (ou reativa) a aba de busca.
    private void MnI_BuscarAnime_Click(object? sender, EventArgs e)
    {
        RequireAuthentication(_myAnimesController.AbrirBuscarAnime);
    }

    //Menu MyAnimes > Analizar Pastas - analisa uma pasta e cadastra os MyAnimes.
    private async void MnI_AnalizarPastas_Click(object? sender, EventArgs e)
    {
        if (!_identityAuthenticationService.IsAuthenticated)
            return;

        await _myAnimesController.AnalizarPastasAsync();
    }

    private void MyAnimesController_AnaliseEmAndamentoAlterada(object? sender, EventArgs e)
    {
        if (IsDisposed)
            return;

        MnI_AnalizarPastas.Enabled = !_myAnimesController.AnaliseEmAndamento;
    }

    private void OpenMyAnimeStructure(int myAnimeId)
    {
        if (!_identityAuthenticationService.IsAuthenticated || myAnimeId <= 0) return;
        _myAnimesController.AbrirEstruturaMyAnime(myAnimeId);
    }

    //Menu MyMusicX - abre (ou reativa) a aba MyMusicX.
    private void MnI_MyMusicX_Click(object sender, EventArgs e)
    {
        OpenMyMusicX();
    }

    private void OpenMyMusicX()
    {
        _workspace.OpenOrActivate(MyMusicXTabKey, () =>
        {
            var page = new TabPage("MyMusicX")
            {
                Name = MyMusicXTabKey,
                ImageIndex = _workspace.GetImageIndex(MyMusicXTabKey, Properties.Resources.violaoEmChamas)
            };
            page.Controls.Add(new FUC_MyMusicX(_identityAuthenticationService));
            return page;
        });
    }

    //Menu NinoTI - abre a aba Nino T.I.
    private void MnI_NinoTI_Click(object sender, EventArgs e)
    {
        OpenNinoTI();
    }

    private void OpenNinoTI()
    {
        _workspace.OpenOrActivate(NinoTITabKey, () =>
        {
            var page = new TabPage("Nino T.I")
            {
                Name = NinoTITabKey,
                ImageIndex = _workspace.GetImageIndex("NinoTI", Properties.Resources.OlhoBRHacker1024)
            };
            page.Controls.Add(new FUC_NinoTI());
            return page;
        });
    }
    //==============================================================
    //Menu Cadastrar Usuario - abre (ou reativa) a aba de administração protegida do Identity já no formulário de provisionamento.
    private void MnI_CadastrarUsuario_Click(object sender, EventArgs e)
    {
        if (!_identityAuthenticationService.IsAuthenticated) return;

        var administrationTab = _workspace.OpenOrActivate(AdministracaoTabKey, () =>
        {
            var page = new TabPage("Administração de Usuários")
            {
                Name = AdministracaoTabKey,
                ImageIndex = _workspace.GetImageIndex(AdministracaoTabKey, Properties.Resources.CaveraMetal)
            };
            page.Controls.Add(new FUC_IdentityAdministration(_identityAuthenticationService));
            return page;
        });
        administrationTab.Controls.OfType<FUC_IdentityAdministration>().FirstOrDefault()?.FocusProvisioning();
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
        _inicio.DefinirAcessoLiberado(isAuthenticated);
        MnI_Monitoramento.Enabled = isAuthenticated;
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
        _workspace.CloseAll();
        _workspace.Activate(InicioTabKey);
        UpdateStatusFooter(string.Empty);
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

        var page = _workspace.OpenOrActivate(PainelSaudeTabKey, () =>
        {
            var healthPage = new TabPage("Painel de Saúde")
            {
                Name = PainelSaudeTabKey,
                ImageIndex = _workspace.GetImageIndex(PainelSaudeTabKey, Properties.Resources.CaveraMetal)
            };
            healthPage.Controls.Add(new FUC_PainelSaude(_healthMonitoringService));
            return healthPage;
        });

        var panel = FindHealthPanel(page);
        if (panel is null)
        {
            return;
        }

        if (_lastHealthSnapshot is not null)
        {
            panel.ApplySnapshot(_lastHealthSnapshot);
        }

        await panel.RefreshAsync(_formClosingCancellationTokenSource.Token);
    }

    private static FUC_PainelSaude? FindHealthPanel(TabPage? page) =>
        page?.Controls.OfType<FUC_PainelSaude>().FirstOrDefault();

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

            FindHealthPanel(_workspace.Find(PainelSaudeTabKey))?.ApplySnapshot(snapshot);
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

    //Menu Sair - Fechar a aplicação Toda.
    private void MnI_Sair_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }
    //=================================================================
    //Menu DtudoSite > DtudoWebView2 - abre o DtudoSite em uma nova aba com WebView2.
    private async void MnI_DtudoWebView_Click(object sender, EventArgs e)
    {
        await ExecuteDtudoSiteActionAsync(OpenDtudoSiteInWebViewAsync);
    }

    //Menu DtudoSite > Site no Navegador - mesmo comportamento do atalho DtudoSite da aba Início (Google Chrome).
    private async void MnI_SiteNoNavegador_Click(object sender, EventArgs e)
    {
        await ExecuteDtudoSiteActionAsync(OpenDtudoSiteInGoogleChromeAsync);
    }

    private async Task ExecuteDtudoSiteActionAsync(Func<CancellationToken, Task> action)
    {
        if (_isOpeningDtudoSite || IsDisposed)
            return;

        _isOpeningDtudoSite = true;
        _inicio.DefinirDtudoSiteOcupado(true);
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
                _inicio.DefinirDtudoSiteOcupado(false);
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

        OpenDtudoWebViewTab(startUri);
    }

    // Cada página web ocupa a sua própria aba; "Nova guia" e links que abrem nova janela criam outra aba.
    private void OpenDtudoWebViewTab(Uri uri)
    {
        if (IsDisposed)
            return;

        var key = $"{DtudoWebViewTabKeyPrefix}{++_webViewTabCounter}";
        _workspace.OpenOrActivate(key, () =>
        {
            var page = new TabPage("DtudoSite")
            {
                Name = key,
                ToolTipText = uri.AbsoluteUri,
                ImageIndex = _workspace.GetImageIndex("DtudoWebView", Properties.Resources.DtudoFireLogo)
            };
            var webView = new FUC_DtudoWebView(uri);
            webView.TitleChanged += (_, e) =>
            {
                page.Text = e.Title;
                page.ToolTipText = e.ToolTip;
            };
            webView.NewTabRequested += (_, target) => OpenDtudoWebViewTab(target);
            page.Controls.Add(webView);
            return page;
        });
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

    private void Frm_WinAppDtudo_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _formClosingCancellationTokenSource.Cancel();
    }

    private void Frm_WinAppDtudo_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _healthRefreshTimer.Stop();
        _healthRefreshTimer.Dispose();
        _myAnimesController.AnaliseEmAndamentoAlterada -= MyAnimesController_AnaliseEmAndamentoAlterada;
        _myAnimesController.Dispose();
        _workspace.CloseAll();
        _workspace.Dispose();
        _healthNotificationService.Dispose();
        _healthMonitoringService.Dispose();
        _dtudoSiteStartupService.Dispose();
        _apiFileStorageStartupService.Dispose();
        _identityAuthenticationService.Dispose();
    }

    private async void Frm_WinAppDtudo_Shown(object? sender, EventArgs e)
    {
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
}
