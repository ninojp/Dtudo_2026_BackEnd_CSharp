using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public partial class Frm_MyMusicX : CustomFormNoBorder
{
    private readonly WinAppAuthenticationService? _authenticationService;
    private readonly ApiMusicXService _apiMusicXService;
    private readonly LegacyMusicCollectionMigrationService _legacyMigrationService;
    private readonly CancellationTokenSource _closingCancellationTokenSource = new();
    private readonly Label _apiStatusLabel;
    private readonly Button _refreshButton;
    private readonly Button _importButton;
    private readonly Button _cancelImportButton;
    private readonly CheckBox _dryRunCheckBox;
    private readonly Button _discogsButton;
    private readonly ListView _collectionsListView;
    private readonly RichTextBox _operationLog;
    private CancellationTokenSource? _migrationCancellationTokenSource;
    private bool _isRefreshing;
    private bool _isImporting;

    public Frm_MyMusicX(
        WinAppAuthenticationService? authenticationService = null,
        ApiMusicXService? apiMusicXService = null,
        LegacyMusicCollectionMigrationService? legacyMigrationService = null)
    {
        InitializeComponent();
        _authenticationService = authenticationService;
        _apiMusicXService = apiMusicXService ?? new ApiMusicXService(authenticationService);
        _legacyMigrationService = legacyMigrationService ?? new LegacyMusicCollectionMigrationService(_apiMusicXService);
        (_apiStatusLabel, _refreshButton, _importButton, _cancelImportButton, _dryRunCheckBox, _discogsButton, _collectionsListView, _operationLog) = CreateMusicSurface();
        abrirToolStripMenuItem.Text = "Migrar JSON legado";
        abrirToolStripMenuItem.Click += ImportLegacyButton_Click;
        Load += Frm_MyMusicX_Load;
        FormClosed += Frm_MyMusicX_FormClosed;
        // Aplicar o tema Dark Mode ao formulário e seus componentes
        ThemeManager.ApplyDarkModeToForm(this);
        // Inicializa o formulário customizado sem barra de título
        MenuStrip? menuStrip = this.Controls.OfType<MenuStrip>().FirstOrDefault();
        if (menuStrip != null)
        {
            InitializeCustomFormNoBorder(menuStrip);
            AddControlButtonsToMenuStrip(menuStrip);
        }
        else
        {
            InitializeCustomFormNoBorder();
        }

        Mnu_MenuMyMusiX.BringToFront();
    }

    private async void Frm_MyMusicX_Load(object? sender, EventArgs e)
    {
        await RefreshCollectionsAsync();
    }

    private async void RefreshButton_Click(object? sender, EventArgs e)
    {
        await RefreshCollectionsAsync();
    }

    private void DiscogsButton_Click(object? sender, EventArgs e)
    {
        if (_isRefreshing || _isImporting || IsDisposed)
        {
            return;
        }

        using var form = new Frm_DiscogsImport(
            _authenticationService,
            apiMusicXService: _apiMusicXService);
        form.ShowDialog(this);
    }

    private async Task RefreshCollectionsAsync()
    {
        if (_isRefreshing || _isImporting || IsDisposed)
        {
            return;
        }

        _isRefreshing = true;
        UpdateActionState();
        _collectionsListView.Items.Clear();
        _operationLog.Clear();
        _apiStatusLabel.Text = "ApiMusicX: verificando disponibilidade...";
        var progress = new Progress<string>(AppendOperationLog);

        try
        {
            AppendOperationLog("Etapa 1/3: iniciando leitura das Colecoes locais.");
            var result = await _apiMusicXService.ObterColecoesAsync(
                progress: progress,
                cancellationToken: _closingCancellationTokenSource.Token);

            AppendCollections(result.Items);
            _apiStatusLabel.Text = $"ApiMusicX: disponivel | {result.TotalCount} Colecao(oes) encontrada(s).";
            AppendOperationLog("Etapa 3/3: Colecoes carregadas no WinAppDtudo.");
        }
        catch (OperationCanceledException) when (_closingCancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (WinAppAuthenticationException exception)
        {
            _apiStatusLabel.Text = "ApiMusicX: autenticacao ou disponibilidade pendente.";
            AppendOperationLog($"Falha: {exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            _apiStatusLabel.Text = "ApiMusicX: indisponivel.";
            var status = exception.StatusCode is null
                ? "sem status HTTP"
                : $"HTTP {(int)exception.StatusCode.Value}";
            AppendOperationLog($"Falha de comunicacao ({status}). O erro foi registrado sem credenciais.");
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Frm_MyMusicX leitura ApiMusicX", exception);
            _apiStatusLabel.Text = "ApiMusicX: falha inesperada.";
            AppendOperationLog("Falha inesperada registrada sem expor credenciais.");
        }
        finally
        {
            _isRefreshing = false;
            UpdateActionState();
        }
    }

    private async void ImportLegacyButton_Click(object? sender, EventArgs e)
    {
        if (_isImporting || IsDisposed)
        {
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = "JSON legado (*.json)|*.json|Todos os arquivos (*.*)|*.*",
            Title = "Selecionar JSON legado do MyMusicX",
            CheckFileExists = true,
            Multiselect = false,
            RestoreDirectory = true
        };
        var defaultPath = FindLegacyJsonPath();
        if (defaultPath is not null)
        {
            dialog.InitialDirectory = Path.GetDirectoryName(defaultPath);
            dialog.FileName = Path.GetFileName(defaultPath);
        }

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var dryRun = _dryRunCheckBox.Checked;
        if (!dryRun)
        {
            var confirmation = DarkMessageBox.Show(
                this,
                "A importacao real enviara dados normalizados para a ApiMusicX. " +
                "Nenhum arquivo de musica sera criado, movido ou excluido. Deseja continuar?",
                "Confirmar importacao da Colecao",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirmation != DialogResult.Yes)
            {
                AppendOperationLog("Importacao real cancelada antes do envio para a ApiMusicX.");
                return;
            }
        }

        await ImportLegacyJsonAsync(dialog.FileName, dryRun);
    }

    private async Task ImportLegacyJsonAsync(string filePath, bool dryRun)
    {
        _isImporting = true;
        _migrationCancellationTokenSource = new CancellationTokenSource();
        UpdateActionState();
        _operationLog.Clear();
        _apiStatusLabel.Text = dryRun
            ? "Migracao: executando dry-run..."
            : "Migracao: enviando dados para a ApiMusicX...";

        var progress = new Progress<LegacyMusicMigrationProgress>(ReportMigrationProgress);
        try
        {
            var result = await _legacyMigrationService.ExecutarAsync(
                filePath,
                dryRun,
                progress,
                _migrationCancellationTokenSource.Token);
            AppendMigrationSummary(result);
        }
        catch (OperationCanceledException) when (_migrationCancellationTokenSource.IsCancellationRequested)
        {
            AppendOperationLog("Migracao cancelada pelo operador. Nenhum item ainda nao enviado foi processado.");
            _apiStatusLabel.Text = "Migracao: cancelada.";
        }
        catch (LegacyMusicMigrationException exception)
        {
            AppendOperationLog($"Falha de validacao do JSON legado: {exception.Message}");
            _apiStatusLabel.Text = "Migracao: arquivo invalido.";
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Migracao JSON legado MyMusicX", exception);
            AppendOperationLog("Falha inesperada registrada sem expor credenciais.");
            _apiStatusLabel.Text = "Migracao: falha inesperada.";
        }
        finally
        {
            _migrationCancellationTokenSource.Dispose();
            _migrationCancellationTokenSource = null;
            _isImporting = false;
            UpdateActionState();
        }
    }

    private void ReportMigrationProgress(LegacyMusicMigrationProgress progress)
    {
        AppendOperationLog(
            $"[{progress.Stage}] {progress.Percentual}% - {progress.Message}");
    }

    private void AppendMigrationSummary(LegacyMusicMigrationResult result)
    {
        var summary = result.Summary;
        AppendOperationLog(
            $"Resumo final: lidos={summary.Lidos}, importados={summary.Importados}, " +
            $"atualizados={summary.Atualizados}, ignorados={summary.Ignorados}, " +
            $"falhos={summary.Falhos}, simulados={summary.Simulados}.");
        foreach (var error in summary.Erros.Take(20))
        {
            AppendOperationLog($"Detalhe: {error}");
        }

        _apiStatusLabel.Text = summary.Cancelada
            ? "Migracao: cancelada."
            : summary.DryRun
                ? $"Migracao: dry-run concluido | {summary.Simulados} item(ns) simulado(s)."
                : $"Migracao: concluida | {summary.Importados} importado(s), {summary.Atualizados} atualizado(s), " +
                  $"{summary.Falhos} falho(s). Atualize a lista para consultar o estado local.";
    }

    private void AppendCollections(IReadOnlyList<ApiMusicXCollectionSummaryDto> collections)
    {
        foreach (var collection in collections)
        {
            var item = new ListViewItem(collection.DisplayName);
            item.SubItems.Add(string.Join(", ", collection.Artists.Select(artist => artist.DisplayName)));
            item.SubItems.Add(collection.ReleaseCount.ToString());
            _collectionsListView.Items.Add(item);
        }

        if (collections.Count == 0)
        {
            AppendOperationLog("Etapa 3/3: a ApiMusicX respondeu, mas nao ha Colecoes locais para exibir.");
        }
    }

    private void AppendOperationLog(string message)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => AppendOperationLog(message));
            return;
        }

        _operationLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _operationLog.SelectionStart = _operationLog.TextLength;
        _operationLog.ScrollToCaret();
    }

    private (Label Status, Button Refresh, Button Import, Button CancelImport, CheckBox DryRun, Button Discogs, ListView Collections, RichTextBox Log) CreateMusicSurface()
    {
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            BackColor = DarkModeColors.BackgroundColor
        };
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 92,
            BackColor = DarkModeColors.BackgroundColor
        };
        var titleLabel = new Label
        {
            AutoSize = true,
            Location = new Point(0, 2),
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = DarkModeColors.TextColor,
            BackColor = Color.Transparent,
            Text = "Colecoes locais"
        };
        var statusLabel = new Label
        {
            AutoSize = true,
            Location = new Point(0, 38),
            Font = new Font("Segoe UI", 10F),
            ForeColor = DarkModeColors.TextSecondaryColor,
            BackColor = Color.Transparent,
            Text = "ApiMusicX: aguardando leitura..."
        };
        var refreshButton = new Button
        {
            Size = new Size(190, 42),
            BackColor = DarkModeColors.AccentColor,
            ForeColor = DarkModeColors.TextColor,
            FlatStyle = FlatStyle.Flat,
            Text = "Atualizar Colecoes"
        };
        refreshButton.FlatAppearance.BorderColor = DarkModeColors.ActiveBorderColor;
        refreshButton.Click += RefreshButton_Click;
        var importButton = new Button
        {
            Size = new Size(190, 42),
            BackColor = DarkModeColors.AccentColor,
            ForeColor = DarkModeColors.TextColor,
            FlatStyle = FlatStyle.Flat,
            Text = "Migrar JSON legado"
        };
        importButton.FlatAppearance.BorderColor = DarkModeColors.ActiveBorderColor;
        importButton.Click += ImportLegacyButton_Click;
        var discogsButton = new Button
        {
            Size = new Size(190, 42),
            BackColor = DarkModeColors.AccentColor,
            ForeColor = DarkModeColors.TextColor,
            FlatStyle = FlatStyle.Flat,
            Text = "Buscar Discogs"
        };
        discogsButton.FlatAppearance.BorderColor = DarkModeColors.ActiveBorderColor;
        discogsButton.Click += DiscogsButton_Click;
        var cancelImportButton = new Button
        {
            Size = new Size(130, 42),
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            ForeColor = DarkModeColors.TextColor,
            FlatStyle = FlatStyle.Flat,
            Text = "Cancelar",
            Enabled = false
        };
        cancelImportButton.FlatAppearance.BorderColor = DarkModeColors.ActiveBorderColor;
        cancelImportButton.Click += CancelImportButton_Click;
        var dryRunCheckBox = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            CheckState = CheckState.Checked,
            ForeColor = DarkModeColors.TextColor,
            Text = "Dry-run",
            Margin = new Padding(8, 14, 8, 0)
        };
        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 770,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0),
            BackColor = DarkModeColors.BackgroundColor
        };
        actionPanel.Controls.Add(cancelImportButton);
        actionPanel.Controls.Add(importButton);
        actionPanel.Controls.Add(discogsButton);
        actionPanel.Controls.Add(refreshButton);
        actionPanel.Controls.Add(dryRunCheckBox);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(statusLabel);
        headerPanel.Controls.Add(actionPanel);

        var collectionsListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HideSelection = false,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            ForeColor = DarkModeColors.TextColor
        };
        collectionsListView.Columns.Add("Colecao", 360);
        collectionsListView.Columns.Add("Artistas", 420);
        collectionsListView.Columns.Add("Releases", 120);

        var operationLog = new RichTextBox
        {
            Dock = DockStyle.Bottom,
            Height = 220,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            ForeColor = DarkModeColors.TextSecondaryColor,
            Font = new Font("Consolas", 10F),
            DetectUrls = false,
            Text = "Aguardando operacao..."
        };

        contentPanel.Controls.Add(collectionsListView);
        contentPanel.Controls.Add(operationLog);
        contentPanel.Controls.Add(headerPanel);
        Controls.Add(contentPanel);

        ThemeManager.ApplyDarkModeToControl(contentPanel);
        ThemeManager.ApplyDarkModeToControl(headerPanel);
        ThemeManager.ApplyDarkModeToControl(actionPanel);
        ThemeManager.ApplyDarkModeToControl(titleLabel);
        ThemeManager.ApplyDarkModeToControl(statusLabel);
        ThemeManager.ApplyDarkModeToControl(refreshButton);
        ThemeManager.ApplyDarkModeToControl(importButton);
        ThemeManager.ApplyDarkModeToControl(discogsButton);
        ThemeManager.ApplyDarkModeToControl(cancelImportButton);
        ThemeManager.ApplyDarkModeToControl(dryRunCheckBox);
        ThemeManager.ApplyDarkModeToControl(collectionsListView);
        ThemeManager.ApplyDarkModeToControl(operationLog);

        return (statusLabel, refreshButton, importButton, cancelImportButton, dryRunCheckBox, discogsButton, collectionsListView, operationLog);
    }

    private void CancelImportButton_Click(object? sender, EventArgs e)
    {
        if (!_isImporting)
        {
            return;
        }

        AppendOperationLog("Cancelamento solicitado; aguardando a etapa atual terminar...");
        _migrationCancellationTokenSource?.Cancel();
    }

    private void UpdateActionState()
    {
        if (IsDisposed)
        {
            return;
        }

        var canStart = !_isRefreshing
            && !_isImporting
            && !_closingCancellationTokenSource.IsCancellationRequested;
        _refreshButton.Enabled = canStart;
        _importButton.Enabled = canStart;
        _discogsButton.Enabled = canStart;
        _dryRunCheckBox.Enabled = canStart;
        _cancelImportButton.Enabled = _isImporting;
        abrirToolStripMenuItem.Enabled = canStart;
    }

    private static string? FindLegacyJsonPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "ApiNode", "mymusicx", "mymusicx.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private void Frm_MyMusicX_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _closingCancellationTokenSource.Cancel();
        _migrationCancellationTokenSource?.Cancel();
    }
    //Menu Windows - Exibir janelas em cascata.
    private void CascataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.LayoutMdi(MdiLayout.Cascade);
    }
    //Menu Windows - Exibir janelas na horizontal.
    private void HorizontalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.LayoutMdi(MdiLayout.TileHorizontal);
    }
    //Menu Windows - Exibir janelas em vertical.
    private void VerticalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.LayoutMdi(MdiLayout.TileVertical);
    }
    //private void FormTestToolStripMenuItem_Click(object sender, EventArgs e)
    //{
    //    Frm_FormTest formTest = new();
    //    formTest.MdiParent = this;
    //    formTest.Show();
    //}
}
