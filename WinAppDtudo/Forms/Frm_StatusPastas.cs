using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.FileMonitoring;
using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

/// <summary>
/// Diálogo (Dark Mode) que dispara a verificação das pastas dos animes na ApiMyAnimes e mostra, em tempo real, o texto de cada
/// etapa, o progresso e o resultado por anime (estado, vídeos/episódios e última verificação) nas cores cinza, azul e verde do site.
/// Fechar a janela durante a verificação a cancela; os animes já verificados permanecem gravados.
/// </summary>
public sealed class Frm_StatusPastas : CustomFormNoBorder
{
    private const int WmNcLButtonDown = 0xA1;
    private const int HtCaption = 0x2;
    private const string Bullet = "●";

    // Evita duas verificacoes disparadas ao mesmo tempo por esta instancia do WinApp.
    private static int _emExecucao;

    private readonly AnimeFolderRefreshCoordinator _coordinator;
    private readonly int? _myAnimeId;
    private readonly CancellationTokenSource _cancelamento = new();
    private readonly List<AnimeFolderStatusRow> _linhas = [];
    private List<AnimeFolderStatusRow> _visiveis = [];

    private readonly Label _lblTitulo;
    private readonly Label _lblMensagem;
    private readonly ProgressBar _progresso;
    private readonly Label _lblContagem;
    private readonly ComboBox _cmbFiltro;
    private readonly DataGridView _grade;
    private readonly Label _lblResumo;
    private readonly Button _btnCancelar;
    private readonly Button _btnFechar;

    private bool _executando;
    private bool _fecharAoConcluir;
    private int _totalConhecido;
    private readonly Font _fonteEstado;
    private Task? _tarefa;

    private Frm_StatusPastas(AnimeFolderRefreshCoordinator coordinator, int? myAnimeId, string? tituloColecao)
    {
        _coordinator = coordinator;
        _myAnimeId = myAnimeId;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(1500, 900);
        Font = new Font("Segoe UI", 12F);
        _fonteEstado = new Font(Font, FontStyle.Bold);

        var nomeEscopo = myAnimeId is null
            ? "todas as coleções"
            : $"coleção {(string.IsNullOrWhiteSpace(tituloColecao) ? $"MyAnime {myAnimeId}" : tituloColecao.Trim())}";

        _lblTitulo = CriarRotulo($"Atualizar status das pastas — {nomeEscopo}", 18F, FontStyle.Bold);
        _lblTitulo.AutoEllipsis = true;
        _lblTitulo.MouseDown += ArrastarJanela;
        _lblMensagem = CriarRotulo("Preparando a verificação…", 14F, FontStyle.Regular);
        _lblMensagem.TextAlign = ContentAlignment.TopLeft;
        _lblMensagem.AutoEllipsis = true;

        _progresso = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 1,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Margin = new Padding(0, 6, 16, 6)
        };
        _lblContagem = CriarRotulo(string.Empty, 13F, FontStyle.Regular);
        _lblContagem.TextAlign = ContentAlignment.MiddleRight;

        _cmbFiltro = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 13F),
            DisplayMember = nameof(OpcaoFiltro.Texto)
        };
        _cmbFiltro.Items.AddRange(
        [
            new OpcaoFiltro("Todos os animes", AnimeFolderStatusFilter.All),
            new OpcaoFiltro("Somente completos", AnimeFolderStatusFilter.Complete),
            new OpcaoFiltro("Somente parciais", AnimeFolderStatusFilter.Partial),
            new OpcaoFiltro("Somente sem pasta", AnimeFolderStatusFilter.None),
            new OpcaoFiltro("Indisponíveis e falhas", AnimeFolderStatusFilter.Attention)
        ]);
        _cmbFiltro.SelectedIndex = 0;
        _cmbFiltro.SelectedIndexChanged += (_, _) => AplicarFiltro();

        _grade = CriarGrade();

        _lblResumo = CriarRotulo(string.Empty, 14F, FontStyle.Bold);
        _lblResumo.TextAlign = ContentAlignment.MiddleLeft;

        _btnCancelar = CriarBotao("Cancelar verificação");
        _btnCancelar.Click += (_, _) => SolicitarCancelamento();
        _btnFechar = CriarBotao("Fechar");
        _btnFechar.Enabled = false;
        _btnFechar.Click += (_, _) => Close();

        Controls.Add(MontarLayout());
        ThemeManager.ApplyDarkModeToForm(this);
        InitializeCustomFormNoBorder();
        AplicarCores();
    }

    /// <summary>
    /// Executa a verificação das pastas de uma coleção (<paramref name="myAnimeId"/>) ou de todas (<c>null</c>, com confirmação)
    /// e exibe o resultado. Retorna quando o diálogo é fechado.
    /// </summary>
    public static async Task ExecutarAsync(
        IWin32Window? owner,
        WinAppAuthenticationService authenticationService,
        int? myAnimeId,
        string? tituloColecao = null)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);

        if (myAnimeId is null)
        {
            var confirmacao = DarkMessageBox.Show(
                owner,
                "Atualizar o status das pastas de TODAS as coleções?\n\n" +
                "A ApiMyAnimes consultará a ApiFileStorage para cada anime e isso pode levar vários minutos. " +
                "Você poderá acompanhar cada etapa e cancelar; os animes já verificados permanecem gravados.",
                "Atualizar todas",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (confirmacao != DialogResult.Yes) return;
        }

        if (Interlocked.CompareExchange(ref _emExecucao, 1, 0) != 0)
        {
            DarkMessageBox.Show(
                owner,
                "Já existe uma verificação de pastas aberta neste WinAppDtudo. Aguarde o término ou cancele-a antes de iniciar outra.",
                "Verificação em andamento",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            using var api = new AnimeFolderStatusApiClient(authenticationService);
            var catalogo = new ApiMyAnimesFolderCatalog(new ApiMyAnimesService(authenticationService));
            if (myAnimeId is { } colecaoId && string.IsNullOrWhiteSpace(tituloColecao))
                tituloColecao = await ObterTituloAsync(catalogo, colecaoId);
            using var form = new Frm_StatusPastas(new AnimeFolderRefreshCoordinator(api, catalogo), myAnimeId, tituloColecao);
            form.ShowDialog(owner);
            // ShowDialog devolve depois que a verificacao termina (o fechamento so e permitido ao final); aguardar evita tarefa solta.
            if (form._tarefa is { } tarefa) await tarefa;
        }
        finally
        {
            Interlocked.Exchange(ref _emExecucao, 0);
        }
    }

    private static async Task<string?> ObterTituloAsync(IAnimeFolderCatalog catalogo, int myAnimeId)
    {
        try
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            return await catalogo.GetCollectionTitleAsync(myAnimeId, limite.Token);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or OperationCanceledException or TimeoutException
                or WinAppAuthenticationException or System.Text.Json.JsonException)
        {
            // O titulo e apenas informativo; sem ele o cabecalho usa o codigo MyAnime.
            return null;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Em telas pequenas ou DPI alto o tamanho em pixels pode exceder a area util.
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Size = new Size(Math.Min(Width, (int)(area.Width * 0.96)), Math.Min(Height, (int)(area.Height * 0.96)));
        CenterToParent();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _tarefa = ExecutarVerificacaoAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_executando)
        {
            e.Cancel = true;
            _fecharAoConcluir = true;
            SolicitarCancelamento();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancelamento.Dispose();
            _fonteEstado.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task ExecutarVerificacaoAsync()
    {
        DefinirExecutando(true);
        var progresso = new Progress<AnimeFolderRefreshProgress>(AoProgredir);
        try
        {
            var resultado = await _coordinator.RunAsync(_myAnimeId, progresso, _cancelamento.Token);
            MostrarResultado(resultado);
        }
        catch (OperationCanceledException)
        {
            MostrarFalha("Verificação cancelada antes de começar.", DarkModeColors.WarningColor, exibirDialogo: false);
        }
        catch (FolderRefreshException exception)
        {
            MostrarFalha(exception.Message, CorDaFalha(exception.Kind), exibirDialogo: true);
        }
        catch (Exception exception)
        {
            // Limite de UI: qualquer falha inesperada vira mensagem em vez de derrubar o WinApp.
            MostrarFalha($"Falha inesperada durante a verificação: {exception.Message}", DarkModeColors.ErrorColor, exibirDialogo: true);
        }
        finally
        {
            DefinirExecutando(false);
        }
    }

    private void AoProgredir(AnimeFolderRefreshProgress progresso)
    {
        if (IsDisposed || Disposing) return;

        _lblMensagem.Text = progresso.Message;
        _lblMensagem.ForeColor = progresso.Phase is AnimeFolderRefreshPhase.Retrying or AnimeFolderRefreshPhase.Cancelling
            ? DarkModeColors.WarningColor
            : DarkModeColors.TextColor;

        if (progresso.Total > 0)
        {
            if (_progresso.Style != ProgressBarStyle.Continuous) _progresso.Style = ProgressBarStyle.Continuous;
            _progresso.Maximum = progresso.Total;
            _progresso.Value = Math.Clamp(progresso.Processed, 0, progresso.Total);
        }

        if (progresso.Row is { } linha) AdicionarLinha(linha);
        AtualizarContagem(progresso.Processed, progresso.Total);
    }

    private void AdicionarLinha(AnimeFolderStatusRow linha)
    {
        _linhas.Add(linha);
        if (!AnimeFolderStatusPresenter.Matches(linha, FiltroAtual())) return;

        var acompanhar = _grade.RowCount == 0
            || _grade.FirstDisplayedScrollingRowIndex + _grade.DisplayedRowCount(false) >= _grade.RowCount - 1;
        _visiveis.Add(linha);
        _grade.RowCount = _visiveis.Count;
        if (acompanhar && _grade.RowCount > 0) _grade.FirstDisplayedScrollingRowIndex = _grade.RowCount - 1;
    }

    private void AplicarFiltro()
    {
        var filtro = FiltroAtual();
        _visiveis = _linhas.Where(linha => AnimeFolderStatusPresenter.Matches(linha, filtro)).ToList();
        _grade.RowCount = 0;
        _grade.RowCount = _visiveis.Count;
        AtualizarContagem(_linhas.Count, _totalConhecido);
    }

    private void AtualizarContagem(int processados, int total)
    {
        if (total > 0) _totalConhecido = total;
        var texto = _totalConhecido > 0 ? $"{processados} de {_totalConhecido} animes" : $"{processados} animes verificados";
        _lblContagem.Text = _visiveis.Count == _linhas.Count ? texto : $"{texto} · exibindo {_visiveis.Count}";
    }

    private void MostrarResultado(AnimeFolderRefreshResult resultado)
    {
        var run = resultado.Run;
        var comRessalvas = resultado.Rows.Any(linha => linha.PreservedPrevious)
            || run.Status is AnimeFolderRefreshStatuses.CompletedWithErrors or AnimeFolderRefreshStatuses.Interrupted;
        var cor = run.Status switch
        {
            AnimeFolderRefreshStatuses.Failed => DarkModeColors.ErrorColor,
            AnimeFolderRefreshStatuses.Cancelled or AnimeFolderRefreshStatuses.Interrupted => DarkModeColors.WarningColor,
            _ => resultado.CancelledByUser || comRessalvas || resultado.Warnings.Count > 0
                ? DarkModeColors.WarningColor
                : DarkModeColors.SuccessColor
        };

        _progresso.Style = ProgressBarStyle.Continuous;
        _progresso.Maximum = Math.Max(1, Math.Max(run.Total, resultado.Rows.Count));
        _progresso.Value = Math.Min(_progresso.Maximum, resultado.Rows.Count);
        _lblMensagem.Text = resultado.CancelledByUser
            ? "Verificação cancelada."
            : resultado.AttachedToExisting
                ? "Verificação concluída (acompanhada de uma execução já em andamento)."
                : "Verificação encerrada.";
        _lblMensagem.ForeColor = cor;

        var texto = resultado.Summary;
        if (resultado.Warnings.Count > 0) texto += "\n" + string.Join(" ", resultado.Warnings);
        _lblResumo.Text = texto;
        _lblResumo.ForeColor = cor;
        AtualizarContagem(resultado.Rows.Count, Math.Max(run.Total, resultado.Rows.Count));
    }

    private void MostrarFalha(string mensagem, Color cor, bool exibirDialogo)
    {
        if (IsDisposed || Disposing) return;
        _progresso.Style = ProgressBarStyle.Continuous;
        _progresso.Value = 0;
        _lblMensagem.Text = "Não foi possível concluir a verificação.";
        _lblMensagem.ForeColor = cor;
        _lblResumo.Text = mensagem;
        _lblResumo.ForeColor = cor;

        if (exibirDialogo && !_fecharAoConcluir)
        {
            DarkMessageBox.Show(this, mensagem, "Status das pastas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static Color CorDaFalha(FolderRefreshFailureKind tipo) =>
        tipo is FolderRefreshFailureKind.Busy or FolderRefreshFailureKind.NotFound
            ? DarkModeColors.WarningColor
            : DarkModeColors.ErrorColor;

    private void DefinirExecutando(bool executando)
    {
        _executando = executando;
        if (IsDisposed) return;

        _btnCancelar.Enabled = executando;
        _btnFechar.Enabled = !executando;
        if (!executando && _progresso.Style == ProgressBarStyle.Marquee) _progresso.Style = ProgressBarStyle.Continuous;
        if (!executando && _fecharAoConcluir) BeginInvoke(Close);
    }

    private void SolicitarCancelamento()
    {
        if (!_executando || _cancelamento.IsCancellationRequested) return;
        _btnCancelar.Enabled = false;
        _lblMensagem.Text = "Cancelando a verificação… os animes já verificados permanecem gravados.";
        _lblMensagem.ForeColor = DarkModeColors.WarningColor;
        _cancelamento.Cancel();
    }

    private AnimeFolderStatusFilter FiltroAtual() =>
        _cmbFiltro.SelectedItem is OpcaoFiltro opcao ? opcao.Filtro : AnimeFolderStatusFilter.All;

    private void ArrastarJanela(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
    }

    private TableLayoutPanel MontarLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(28)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));

        var linhaProgresso = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        linhaProgresso.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        linhaProgresso.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));
        linhaProgresso.Controls.Add(_progresso, 0, 0);
        linhaProgresso.Controls.Add(_lblContagem, 1, 0);

        var linhaFiltro = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        linhaFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        linhaFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));
        linhaFiltro.Controls.Add(CriarLegenda(), 0, 0);
        linhaFiltro.Controls.Add(_cmbFiltro, 1, 0);

        var botoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        botoes.Controls.Add(_btnFechar);
        botoes.Controls.Add(_btnCancelar);

        layout.Controls.Add(_lblTitulo, 0, 0);
        layout.Controls.Add(_lblMensagem, 0, 1);
        layout.Controls.Add(linhaProgresso, 0, 2);
        layout.Controls.Add(linhaFiltro, 0, 3);
        layout.Controls.Add(_grade, 0, 4);
        layout.Controls.Add(_lblResumo, 0, 5);
        layout.Controls.Add(botoes, 0, 6);
        return layout;
    }

    private static FlowLayoutPanel CriarLegenda()
    {
        var legenda = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        legenda.Controls.Add(CriarItemLegenda("Sem pasta", AnimeFolderState.None));
        legenda.Controls.Add(CriarItemLegenda("Parcial", AnimeFolderState.Partial));
        legenda.Controls.Add(CriarItemLegenda("Completo", AnimeFolderState.Complete));
        return legenda;
    }

    private static Label CriarItemLegenda(string texto, string estado) =>
        new()
        {
            AutoSize = true,
            Text = $"{Bullet} {texto}",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = AnimeFolderStatusPresenter.StateColor(estado),
            Margin = new Padding(0, 8, 28, 0)
        };

    private static Label CriarRotulo(string texto, float tamanho, FontStyle estilo) =>
        new()
        {
            Dock = DockStyle.Fill,
            Text = texto,
            Font = new Font("Segoe UI", tamanho, estilo),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };

    private static Button CriarBotao(string texto) =>
        new()
        {
            Text = texto,
            Font = new Font("Segoe UI", 14F),
            Size = new Size(320, 68),
            Margin = new Padding(16, 8, 0, 8)
        };

    private DataGridView CriarGrade()
    {
        var grade = new DataGridView
        {
            Dock = DockStyle.Fill,
            VirtualMode = true,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            Font = new Font("Segoe UI", 12F),
            Margin = Padding.Empty
        };
        grade.RowTemplate.Height = 36;
        AdicionarColuna(grade, "Colecao", "Coleção", 22);
        AdicionarColuna(grade, "MalId", "MAL ID", 8);
        AdicionarColuna(grade, "Anime", "Anime", 28);
        AdicionarColuna(grade, "Estado", "Estado", 11);
        AdicionarColuna(grade, "Videos", "Vídeos/Episódios", 11);
        AdicionarColuna(grade, "Verificacao", "Última verificação", 14);
        AdicionarColuna(grade, "Resultado", "Resultado", 30);
        grade.CellValueNeeded += AoPedirValor;
        grade.CellFormatting += AoFormatarCelula;
        return grade;
    }

    private static void AdicionarColuna(DataGridView grade, string nome, string cabecalho, float peso) =>
        grade.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = nome,
            HeaderText = cabecalho,
            FillWeight = peso,
            MinimumWidth = 60,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

    private void AoPedirValor(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _visiveis.Count) return;
        var linha = _visiveis[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => linha.CollectionTitle,
            1 => linha.MalId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            2 => linha.Title,
            3 => $"{Bullet} {AnimeFolderStatusPresenter.StateText(linha.State)}",
            4 => AnimeFolderStatusPresenter.FormatVideos(linha.Videos, linha.Expected),
            5 => AnimeFolderStatusPresenter.FormatCheckedAt(linha.CheckedAtUtc),
            6 => AnimeFolderStatusPresenter.OutcomeText(linha),
            _ => null
        };
    }

    private void AoFormatarCelula(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _visiveis.Count) return;
        var linha = _visiveis[e.RowIndex];
        switch (e.ColumnIndex)
        {
            case 3:
                e.CellStyle!.ForeColor = AnimeFolderStatusPresenter.StateColor(linha.State);
                e.CellStyle.SelectionForeColor = Color.Black;
                e.CellStyle.Font = _fonteEstado;
                break;
            case 6 when linha.PreservedPrevious:
                e.CellStyle!.ForeColor = string.Equals(linha.Outcome, AnimeFolderRefreshOutcomes.Failed, StringComparison.OrdinalIgnoreCase)
                    ? DarkModeColors.ErrorColor
                    : DarkModeColors.WarningColor;
                break;
        }
    }

    private void AplicarCores()
    {
        BackColor = DarkModeColors.ActiveTabBackgroundColor;
        foreach (var painel in Controls.OfType<TableLayoutPanel>().SelectMany(EnumerarPaineis))
            painel.BackColor = DarkModeColors.ActiveTabBackgroundColor;

        foreach (var rotulo in new[] { _lblTitulo, _lblMensagem, _lblContagem, _lblResumo })
        {
            rotulo.BackColor = DarkModeColors.ActiveTabBackgroundColor;
            rotulo.ForeColor = DarkModeColors.TextColor;
        }

        foreach (var botao in new[] { _btnCancelar, _btnFechar })
        {
            botao.BackColor = DarkModeColors.AccentColor;
            botao.ForeColor = DarkModeColors.TextColor;
        }

        _cmbFiltro.BackColor = DarkModeColors.SurfaceColor;
        _cmbFiltro.ForeColor = DarkModeColors.TextColor;
        _grade.DefaultCellStyle.Font = _grade.Font;
    }

    private static IEnumerable<Control> EnumerarPaineis(Control raiz)
    {
        yield return raiz;
        foreach (Control filho in raiz.Controls)
        {
            if (filho is TableLayoutPanel or FlowLayoutPanel or Panel)
            {
                foreach (var neto in EnumerarPaineis(filho)) yield return neto;
            }
        }
    }

    private sealed record OpcaoFiltro(string Texto, AnimeFolderStatusFilter Filtro)
    {
        public override string ToString() => Texto;
    }
}
