using System.Net;
using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public sealed class Frm_DiscogsImport : CustomFormNoBorder
{
    private readonly ApiDiscogsService _discogsService;
    private readonly ApiMusicXService _apiMusicXService;
    private readonly ApiDiscogsImportCoordinator _coordinator;
    private readonly ApiDiscogsHealthCheckService _healthCheckService;
    private readonly CancellationTokenSource _closingCancellationTokenSource = new();
    private readonly TextBox _searchInput = new();
    private readonly Button _searchButton = new();
    private readonly Button _discographyButton = new();
    private readonly Button _previewButton = new();
    private readonly Button _confirmButton = new();
    private readonly Button _cancelOperationButton = new();
    private readonly Label _statusLabel = new();
    private readonly ListView _artistsListView = new();
    private readonly ListView _releasesListView = new();
    private readonly RichTextBox _previewTextBox = new();
    private readonly RichTextBox _operationLog = new();
    private CancellationTokenSource? _operationCancellationTokenSource;
    private ApiDiscogsArtistSearchItem? _selectedArtist;
    private ApiDiscogsImportPreview? _preview;
    private bool _isBusy;

    public Frm_DiscogsImport(
        WinAppAuthenticationService? authenticationService = null,
        ApiDiscogsService? discogsService = null,
        ApiMusicXService? apiMusicXService = null,
        ApiDiscogsImportCoordinator? coordinator = null)
    {
        _discogsService = discogsService ?? new ApiDiscogsService(authenticationService);
        _apiMusicXService = apiMusicXService ?? new ApiMusicXService(authenticationService);
        _coordinator = coordinator ?? new ApiDiscogsImportCoordinator(_discogsService, _apiMusicXService);
        _healthCheckService = new ApiDiscogsHealthCheckService(authenticationService);

        Text = "Importacao externa - Discogs";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1440, 900);
        MinimumSize = new Size(1050, 680);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;

        BuildLayout();
        ThemeManager.ApplyDarkModeToForm(this);
        InitializeCustomFormNoBorder();
        Load += Frm_DiscogsImport_Load;
        FormClosed += Frm_DiscogsImport_FormClosed;
    }

    private async void Frm_DiscogsImport_Load(object? sender, EventArgs e)
    {
        _statusLabel.Text = "ApiDiscogs: verificando disponibilidade...";
        try
        {
            var status = await _healthCheckService.CheckAsync(_closingCancellationTokenSource.Token);
            _statusLabel.Text = status.IsAvailable
                ? "ApiDiscogs: disponivel para consulta."
                : $"ApiDiscogs: indisponivel. {status.Message}";
            AppendOperationLog(status.IsAvailable
                ? "Health local confirmado; a busca usara somente a ApiDiscogs."
                : $"Health local pendente: {status.Message} A inicializacao sera tentada na busca.");
        }
        catch (OperationCanceledException) when (_closingCancellationTokenSource.IsCancellationRequested)
        {
        }
    }

    private async void SearchButton_Click(object? sender, EventArgs e)
    {
        var query = _searchInput.Text.Trim();
        if (query.Length == 0)
        {
            DarkMessageBox.Show(
                this,
                "Informe o nome de um artista ou banda para iniciar a busca.",
                "Busca Discogs",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var operationCancellation = BeginOperation();
        ClearWorkflow("Nova busca iniciada; resultados anteriores foram removidos.");
        try
        {
            var progress = new Progress<string>(AppendOperationLog);
            var result = await _coordinator.BuscarArtistasAsync(
                query,
                progress,
                operationCancellation.Token);
            foreach (var item in result.Items)
            {
                var listItem = new ListViewItem(item.Name)
                {
                    Tag = item
                };
                listItem.SubItems.Add(item.Type);
                listItem.SubItems.Add(item.Source.Id);
                _artistsListView.Items.Add(listItem);
            }

            _statusLabel.Text = result.Items.Count == 0
                ? "ApiDiscogs: nenhum artista ou banda encontrado."
                : $"ApiDiscogs: {result.Items.Count} resultado(s) encontrado(s); selecione um artista.";
            AppendOperationLog(result.Items.Count == 0
                ? "Etapa 1/5: resultado vazio; refine o nome e tente novamente."
                : $"Etapa 1/5: {result.Items.Count} resultado(s) disponivel(is) para selecao.");
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            _statusLabel.Text = "ApiDiscogs: busca cancelada.";
            AppendOperationLog("Busca cancelada pelo operador antes da selecao.");
        }
        catch (Exception exception)
        {
            HandleOperationException("busca de artistas", exception);
        }
        finally
        {
            EndOperation(operationCancellation);
        }
    }

    private async void DiscographyButton_Click(object? sender, EventArgs e)
    {
        if (_selectedArtist is null)
        {
            DarkMessageBox.Show(
                this,
                "Selecione um artista ou banda antes de consultar a discografia.",
                "Discografia Discogs",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var operationCancellation = BeginOperation();
        _releasesListView.Items.Clear();
        _preview = null;
        _previewTextBox.Clear();
        try
        {
            var result = await _coordinator.ObterDiscografiaAsync(
                _selectedArtist,
                new Progress<string>(AppendOperationLog),
                operationCancellation.Token);
            foreach (var release in result.Items)
            {
                var listItem = new ListViewItem(release.Title)
                {
                    Tag = release
                };
                listItem.SubItems.Add(release.Category);
                listItem.SubItems.Add(release.Year?.ToString() ?? "-");
                listItem.SubItems.Add(release.ResourceType);
                _releasesListView.Items.Add(listItem);
            }

            _statusLabel.Text = result.Items.Count == 0
                ? "ApiDiscogs: a discografia retornou vazia."
                : $"ApiDiscogs: {result.Items.Count} release(s); selecione um ou mais para o preview.";
            AppendOperationLog(result.Items.Count == 0
                ? "Etapa 2/5: nenhum release disponivel para selecao."
                : $"Etapa 2/5: {result.Items.Count} release(s) carregado(s).");
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            _statusLabel.Text = "ApiDiscogs: consulta de discografia cancelada.";
            AppendOperationLog("Consulta da discografia cancelada pelo operador.");
        }
        catch (Exception exception)
        {
            HandleOperationException("consulta de discografia", exception);
        }
        finally
        {
            EndOperation(operationCancellation);
        }
    }

    private async void PreviewButton_Click(object? sender, EventArgs e)
    {
        if (_selectedArtist is null)
        {
            return;
        }

        var selectedReleases = _releasesListView.SelectedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag as ApiDiscogsReleaseSummary)
            .Where(item => item is not null)
            .Cast<ApiDiscogsReleaseSummary>()
            .ToArray();
        if (selectedReleases.Length == 0)
        {
            DarkMessageBox.Show(
                this,
                "Selecione ao menos um release antes de gerar o preview.",
                "Preview da importacao",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var operationCancellation = BeginOperation();
        _preview = null;
        _previewTextBox.Clear();
        try
        {
            var preview = await _coordinator.PrepararPreviewAsync(
                _selectedArtist,
                selectedReleases,
                new Progress<string>(AppendOperationLog),
                operationCancellation.Token);
            _preview = preview;
            _previewTextBox.Text = BuildPreviewText(preview);
            _statusLabel.Text = preview.HasLocalConflict
                ? "Preview: conflito local bloqueia a confirmacao."
                : "Preview pronto; revise os dados e confirme explicitamente a importacao.";
            AppendOperationLog(preview.HasLocalConflict
                ? "Preview concluido com conflito local; nenhuma gravacao sera permitida."
                : "Preview concluido; aguardando confirmacao explicita do operador.");
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            _statusLabel.Text = "Preview: operacao cancelada.";
            AppendOperationLog("Geracao do preview cancelada pelo operador.");
        }
        catch (Exception exception)
        {
            HandleOperationException("geracao do preview", exception);
        }
        finally
        {
            EndOperation(operationCancellation);
        }
    }

    private async void ConfirmButton_Click(object? sender, EventArgs e)
    {
        if (_preview is null)
        {
            return;
        }

        if (_preview.HasLocalConflict)
        {
            DarkMessageBox.Show(
                this,
                _preview.LocalConflictMessage ?? "O conflito local precisa ser resolvido antes da importacao.",
                "Importacao bloqueada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var confirmation = DarkMessageBox.Show(
            this,
            "A confirmacao enviara o preview para a ApiMusicX e podera alterar a Colecao local. " +
            "Deseja confirmar esta importacao?",
            "Confirmar importacao Discogs",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes)
        {
            AppendOperationLog("Importacao nao confirmada; nenhuma chamada de gravacao foi realizada.");
            _statusLabel.Text = "Importacao: aguardando nova confirmacao.";
            return;
        }

        var operationCancellation = BeginOperation();
        try
        {
            var result = await _coordinator.ImportarConfirmadaAsync(
                _preview,
                confirmed: true,
                new Progress<string>(AppendOperationLog),
                operationCancellation.Token);
            _statusLabel.Text = result.Imported
                ? "Importacao confirmada e enviada para a ApiMusicX."
                : "Importacao nao realizada.";
            _confirmButton.Enabled = false;
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            _statusLabel.Text = "Importacao: cancelada antes da conclusao.";
            AppendOperationLog("Importacao cancelada; consulte a ApiMusicX antes de repetir se a requisicao ja foi enviada.");
        }
        catch (Exception exception)
        {
            HandleOperationException("importacao confirmada", exception);
        }
        finally
        {
            EndOperation(operationCancellation);
        }
    }

    private void CancelOperationButton_Click(object? sender, EventArgs e)
    {
        if (_operationCancellationTokenSource is null)
        {
            return;
        }

        AppendOperationLog("Cancelamento solicitado; aguardando a API encerrar a etapa atual...");
        _operationCancellationTokenSource.Cancel();
    }

    private void ArtistsListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedArtist = _artistsListView.SelectedItems.Count == 1
            ? _artistsListView.SelectedItems[0].Tag as ApiDiscogsArtistSearchItem
            : null;
        UpdateActionState();
    }

    private void ReleasesListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateActionState();
    }

    private CancellationTokenSource BeginOperation()
    {
        _operationCancellationTokenSource?.Cancel();
        _operationCancellationTokenSource?.Dispose();
        _operationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            _closingCancellationTokenSource.Token);
        _isBusy = true;
        UpdateActionState();
        return _operationCancellationTokenSource;
    }

    private void EndOperation(CancellationTokenSource operationCancellation)
    {
        if (!ReferenceEquals(_operationCancellationTokenSource, operationCancellation))
        {
            return;
        }

        _operationCancellationTokenSource = null;
        operationCancellation.Dispose();
        _isBusy = false;
        UpdateActionState();
    }

    private void ClearWorkflow(string message)
    {
        _artistsListView.Items.Clear();
        _releasesListView.Items.Clear();
        _previewTextBox.Clear();
        _selectedArtist = null;
        _preview = null;
        _operationLog.Clear();
        AppendOperationLog(message);
        UpdateActionState();
    }

    private void HandleOperationException(string operation, Exception exception)
    {
        if (exception is ApiDiscogsHttpException discogsException)
        {
            var retry = discogsException.RetryAfterSeconds is { } seconds
                ? $" Aguarde aproximadamente {seconds} segundo(s) antes de tentar novamente."
                : string.Empty;
            _statusLabel.Text = $"ApiDiscogs: falha HTTP {(int)discogsException.ResponseStatusCode}.";
            AppendOperationLog($"Falha em {operation}: HTTP {(int)discogsException.ResponseStatusCode}.{retry}");
            return;
        }

        if (exception is ApiDiscogsImportConflictException conflict)
        {
            _statusLabel.Text = "Importacao bloqueada por conflito local.";
            AppendOperationLog($"Falha em {operation}: {conflict.Message}");
            return;
        }

        if (exception is WinAppAuthenticationException authenticationException)
        {
            _statusLabel.Text = "ApiDiscogs: autenticacao ou inicializacao pendente.";
            AppendOperationLog($"Falha em {operation}: {authenticationException.Message}");
            return;
        }

        StartupDiagnostics.Record($"Frm_DiscogsImport {operation}", exception);
        _statusLabel.Text = "ApiDiscogs: falha inesperada registrada.";
        AppendOperationLog($"Falha em {operation}: detalhes registrados sem expor credenciais.");
    }

    private string BuildPreviewText(ApiDiscogsImportPreview preview)
    {
        var builder = new System.Text.StringBuilder(preview.DisplaySummary);
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("Dados que serao enviados:");
        builder.AppendLine($"Nome da Colecao: {preview.Request.DisplayName}");
        builder.AppendLine($"Artistas: {preview.Request.Artists.Count}");
        builder.AppendLine($"Releases: {preview.Request.Releases.Count}");
        builder.AppendLine($"Faixas: {preview.TrackCount}");
        if (preview.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Avisos da fonte externa:");
            foreach (var warning in preview.Warnings.Take(20))
            {
                builder.AppendLine($"- {warning}");
            }
        }

        if (preview.HasLocalConflict)
        {
            builder.AppendLine();
            builder.AppendLine($"BLOQUEADO: {preview.LocalConflictMessage}");
        }
        else
        {
            builder.AppendLine();
            builder.AppendLine("Nenhum conflito bloqueante foi identificado.");
            builder.AppendLine("A gravacao continua condicionada a confirmacao explicita.");
        }

        return builder.ToString();
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

    private void UpdateActionState()
    {
        if (IsDisposed)
        {
            return;
        }

        var canInteract = !_isBusy && !_closingCancellationTokenSource.IsCancellationRequested;
        _searchInput.Enabled = canInteract;
        _searchButton.Enabled = canInteract;
        _discographyButton.Enabled = canInteract && _selectedArtist is not null;
        _previewButton.Enabled = canInteract && _releasesListView.SelectedItems.Count > 0;
        _confirmButton.Enabled = canInteract && _preview is { HasLocalConflict: false };
        _cancelOperationButton.Enabled = _isBusy;
    }

    private void BuildLayout()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = DarkModeColors.BackgroundColor
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = DarkModeColors.BackgroundColor };
        var title = new Label
        {
            AutoSize = true,
            Text = "Consulta e importacao pela Discogs",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = DarkModeColors.TextColor,
            Location = new Point(0, 2)
        };
        _statusLabel.AutoSize = false;
        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 24;
        _statusLabel.Text = "ApiDiscogs: aguardando health...";
        _statusLabel.ForeColor = DarkModeColors.TextSecondaryColor;
        header.Controls.Add(_statusLabel);
        header.Controls.Add(title);

        var searchPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 5, 0, 5),
            BackColor = DarkModeColors.BackgroundColor
        };
        _searchInput.Width = 520;
        _searchInput.Height = 34;
        _searchInput.PlaceholderText = "Nome do artista ou banda";
        _searchInput.KeyDown += SearchInput_KeyDown;
        ConfigureButton(_searchButton, "Buscar artista/banda", 190);
        _searchButton.Click += SearchButton_Click;
        searchPanel.Controls.Add(_searchInput);
        searchPanel.Controls.Add(_searchButton);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 5, 0, 5),
            BackColor = DarkModeColors.BackgroundColor
        };
        ConfigureButton(_discographyButton, "Carregar discografia", 190);
        ConfigureButton(_previewButton, "Gerar preview", 150);
        ConfigureButton(_confirmButton, "Confirmar importacao", 190);
        ConfigureButton(_cancelOperationButton, "Cancelar operacao", 170);
        _discographyButton.Click += DiscographyButton_Click;
        _previewButton.Click += PreviewButton_Click;
        _confirmButton.Click += ConfirmButton_Click;
        _cancelOperationButton.Click += CancelOperationButton_Click;
        actionPanel.Controls.Add(_discographyButton);
        actionPanel.Controls.Add(_previewButton);
        actionPanel.Controls.Add(_confirmButton);
        actionPanel.Controls.Add(_cancelOperationButton);

        var workflowSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 420,
            BackColor = DarkModeColors.ActiveBorderColor
        };
        workflowSplit.Panel1.Controls.Add(CreateArtistsPanel());
        var detailsSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 270,
            BackColor = DarkModeColors.ActiveBorderColor
        };
        detailsSplit.Panel1.Controls.Add(CreateReleasesPanel());
        detailsSplit.Panel2.Controls.Add(CreatePreviewPanel());
        workflowSplit.Panel2.Controls.Add(detailsSplit);

        _operationLog.Dock = DockStyle.Fill;
        _operationLog.ReadOnly = true;
        _operationLog.BorderStyle = BorderStyle.FixedSingle;
        _operationLog.BackColor = DarkModeColors.BackgroundSecondaryColor;
        _operationLog.ForeColor = DarkModeColors.TextSecondaryColor;
        _operationLog.Font = new Font("Consolas", 10F);
        _operationLog.DetectUrls = false;
        _operationLog.Text = "Aguardando operacao...";

        content.Controls.Add(header, 0, 0);
        content.Controls.Add(searchPanel, 0, 1);
        content.Controls.Add(actionPanel, 0, 2);
        content.Controls.Add(workflowSplit, 0, 3);
        content.Controls.Add(_operationLog, 0, 4);
        Controls.Add(content);

        ThemeManager.ApplyDarkModeToControl(content);
        ThemeManager.ApplyDarkModeToControl(header);
        ThemeManager.ApplyDarkModeToControl(title);
        ThemeManager.ApplyDarkModeToControl(_statusLabel);
        ThemeManager.ApplyDarkModeToControl(searchPanel);
        ThemeManager.ApplyDarkModeToControl(_searchInput);
        ThemeManager.ApplyDarkModeToControl(_searchButton);
        ThemeManager.ApplyDarkModeToControl(actionPanel);
        ThemeManager.ApplyDarkModeToControl(_discographyButton);
        ThemeManager.ApplyDarkModeToControl(_previewButton);
        ThemeManager.ApplyDarkModeToControl(_confirmButton);
        ThemeManager.ApplyDarkModeToControl(_cancelOperationButton);
        ThemeManager.ApplyDarkModeToControl(workflowSplit);
        ThemeManager.ApplyDarkModeToControl(detailsSplit);
        ThemeManager.ApplyDarkModeToControl(_operationLog);
        UpdateActionState();
    }

    private Control CreateArtistsPanel()
    {
        var panel = CreateListPanel("Resultados de artistas e bandas", _artistsListView);
        ConfigureListView(_artistsListView, false, ["Nome", "Tipo", "ID Discogs"], [260, 100, 100]);
        _artistsListView.SelectedIndexChanged += ArtistsListView_SelectedIndexChanged;
        return panel;
    }

    private Control CreateReleasesPanel()
    {
        var panel = CreateListPanel("Discografia: selecione um ou mais releases", _releasesListView);
        ConfigureListView(_releasesListView, true, ["Titulo", "Categoria", "Ano", "Origem"], [310, 120, 80, 90]);
        _releasesListView.SelectedIndexChanged += ReleasesListView_SelectedIndexChanged;
        return panel;
    }

    private Control CreatePreviewPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = DarkModeColors.BackgroundColor
        };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = "Preview antes da gravacao",
            ForeColor = DarkModeColors.TextColor,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        };
        _previewTextBox.Dock = DockStyle.Fill;
        _previewTextBox.ReadOnly = true;
        _previewTextBox.BorderStyle = BorderStyle.FixedSingle;
        _previewTextBox.BackColor = DarkModeColors.BackgroundSecondaryColor;
        _previewTextBox.ForeColor = DarkModeColors.TextColor;
        _previewTextBox.Font = new Font("Consolas", 10F);
        panel.Controls.Add(_previewTextBox);
        panel.Controls.Add(title);
        ThemeManager.ApplyDarkModeToControl(panel);
        ThemeManager.ApplyDarkModeToControl(title);
        ThemeManager.ApplyDarkModeToControl(_previewTextBox);
        return panel;
    }

    private static Control CreateListPanel(string titleText, ListView listView)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = DarkModeColors.BackgroundColor
        };
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Text = titleText,
            ForeColor = DarkModeColors.TextColor,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        };
        listView.Dock = DockStyle.Fill;
        panel.Controls.Add(listView);
        panel.Controls.Add(title);
        ThemeManager.ApplyDarkModeToControl(panel);
        ThemeManager.ApplyDarkModeToControl(title);
        ThemeManager.ApplyDarkModeToControl(listView);
        return panel;
    }

    private static void ConfigureListView(
        ListView listView,
        bool multiSelect,
        IReadOnlyList<string> columns,
        IReadOnlyList<int> widths)
    {
        listView.View = View.Details;
        listView.FullRowSelect = true;
        listView.GridLines = true;
        listView.HideSelection = false;
        listView.MultiSelect = multiSelect;
        listView.BackColor = DarkModeColors.BackgroundSecondaryColor;
        listView.ForeColor = DarkModeColors.TextColor;
        for (var index = 0; index < columns.Count; index++)
        {
            listView.Columns.Add(columns[index], widths[index]);
        }
    }

    private static void ConfigureButton(Button button, string text, int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 38;
        button.BackColor = DarkModeColors.AccentColor;
        button.ForeColor = DarkModeColors.TextColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = DarkModeColors.ActiveBorderColor;
        button.Margin = new Padding(0, 0, 10, 0);
    }

    private void SearchInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter || !_searchButton.Enabled)
        {
            return;
        }

        e.SuppressKeyPress = true;
        SearchButton_Click(sender, EventArgs.Empty);
    }

    private void Frm_DiscogsImport_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _closingCancellationTokenSource.Cancel();
        _operationCancellationTokenSource?.Cancel();
        _healthCheckService.Dispose();
        _closingCancellationTokenSource.Dispose();
    }
}
