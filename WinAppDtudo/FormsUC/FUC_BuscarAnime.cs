using LibDtudo.Shared.Dtos.MyAnimeList;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public sealed class FUC_BuscarAnime : UserControl
{
    public event EventHandler<int>? AnimeLocalSelecionado;
    public event EventHandler<int>? AnimeMyAnimeListSelecionado;

    private readonly IAnimeSearchSource _fonteLocal;
    private readonly IAnimeSearchSource _fonteExterna;
    private readonly Action<string, string, MessageBoxIcon> _mostrarMensagem;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly ToolTip _toolTip = new();
    private readonly TextBox _txbBusca;
    private readonly Button _btnBuscaLocal;
    private readonly Button _btnBuscaExterna;
    private readonly Label _lblStatus;
    private readonly FlowLayoutPanel _flpCards;
    private readonly Button _btnAnterior;
    private readonly Button _btnProximo;

    private AnimeSearchSourceKind? _origemAtual;
    private AnimeSearchResult? _resultadoAtual;
    private string _consultaAtual = string.Empty;
    private bool _carregando;
    private bool _encerrado;

    public FUC_BuscarAnime(
        ApiMyAnimesService? apiMyAnimesService = null,
        WinAppAuthenticationService? authenticationService = null)
        : this(
            new DBLocalAnimeSearchSource(apiMyAnimesService ?? new ApiMyAnimesService(authenticationService)),
            new MyAnimeListAnimeSearchSource(new MyAnimeListApiService(authenticationService)))
    {
    }

    internal FUC_BuscarAnime(
        IAnimeSearchSource fonteLocal,
        IAnimeSearchSource fonteExterna,
        Action<string, string, MessageBoxIcon>? mostrarMensagem = null)
    {
        _fonteLocal = fonteLocal;
        _fonteExterna = fonteExterna;
        _mostrarMensagem = mostrarMensagem ?? ((mensagem, titulo, icone) =>
            DarkMessageBox.Show(mensagem, titulo, MessageBoxButtons.OK, icone));

        Name = nameof(FUC_BuscarAnime);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        var lblInput = new Label
        {
            Name = "Lbl_InputBusca",
            Text = "Digite o nome ou ID do anime:",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };
        _txbBusca = new TextBox
        {
            Name = "Txb_Busca",
            Font = new Font("Segoe UI", 14F),
            AccessibleName = "Nome ou ID do anime"
        };
        _btnBuscaLocal = new Button
        {
            Name = "Btn_BuscaDBLocal",
            Text = "Busca DB Local",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            AccessibleDescription = "Busca por título no banco local, sem consultar a ApiMyAnimeList."
        };
        _btnBuscaExterna = new Button
        {
            Name = "Btn_BuscaApiMyAnimeList",
            Text = "Busca ApiMyAnimeList",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            AccessibleDescription = "Busca por nome ou ID exclusivamente na ApiMyAnimeList."
        };
        _lblStatus = new Label
        {
            Name = "Lbl_StatusBusca",
            Font = new Font("Segoe UI", 12F),
            AccessibleName = "Status da busca"
        };
        _flpCards = new FlowLayoutPanel { Name = "Flp_ResultadosBusca" };
        _btnAnterior = new NavigationTextButton
        {
            Name = "Btn_Anterior",
            Text = "◄ Anterior",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        _btnProximo = new NavigationTextButton
        {
            Name = "Btn_Proximo",
            Text = "Próximo ►",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        AnimeSearchLayout.Build(
            this, lblInput, _txbBusca, _btnBuscaLocal, _btnBuscaExterna,
            _lblStatus, _flpCards, _btnAnterior, _btnProximo);
        ThemeManager.ApplyDarkModeToUserControl(this);
        AtualizarStatus("Informe o nome ou ID e escolha a fonte da busca.");
        _toolTip.SetToolTip(_btnBuscaLocal, _btnBuscaLocal.AccessibleDescription);
        _toolTip.SetToolTip(_btnBuscaExterna, _btnBuscaExterna.AccessibleDescription);

        _btnBuscaLocal.Click += async (_, _) => await BuscarPrimeiraPaginaAsync(AnimeSearchSourceKind.DBLocal);
        _btnBuscaExterna.Click += async (_, _) => await BuscarPrimeiraPaginaAsync(AnimeSearchSourceKind.ApiMyAnimeList);
        _btnAnterior.Click += async (_, _) => await BuscarPaginaAsync(-1);
        _btnProximo.Click += async (_, _) => await BuscarPaginaAsync(1);
        _txbBusca.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            _btnBuscaLocal.Focus();
        };
    }

    internal Task BuscarPrimeiraPaginaAsync(AnimeSearchSourceKind origem)
    {
        if (_carregando || _encerrado)
            return Task.CompletedTask;

        var fonte = ObterFonte(origem);
        var consulta = _txbBusca.Text.Trim();
        if (fonte.ValidarConsulta(consulta) is { } aviso)
        {
            _mostrarMensagem(aviso, "Aviso", MessageBoxIcon.Warning);
            _txbBusca.Focus();
            return Task.CompletedTask;
        }

        return ExecutarBuscaAsync(origem, consulta, 1);
    }

    internal Task BuscarPaginaAsync(int direcao)
    {
        if (_carregando || _encerrado || _origemAtual is not { } origem || _resultadoAtual is null)
            return Task.CompletedTask;

        if (direcao is not (-1 or 1))
            throw new ArgumentOutOfRangeException(nameof(direcao));

        if (direcao < 0 ? !_btnAnterior.Enabled : !_btnProximo.Enabled)
            return Task.CompletedTask;

        return ExecutarBuscaAsync(origem, _consultaAtual, Math.Max(1, _resultadoAtual.CurrentPage) + direcao);
    }

    private async Task ExecutarBuscaAsync(AnimeSearchSourceKind origem, string consulta, int pagina)
    {
        var fonte = ObterFonte(origem);
        var cancellationToken = _lifetimeCancellation.Token;
        _carregando = true;
        _origemAtual = origem;
        _consultaAtual = consulta;
        _resultadoAtual = null;
        AtualizarControles();
        DestacarFonte(origem);
        LimparCards();
        AtualizarStatus(fonte.ObterStatusCarregando(consulta));

        try
        {
            var resultado = await fonte.BuscarAsync(consulta, pagina, cancellationToken);
            if (_encerrado || cancellationToken.IsCancellationRequested)
                return;

            if (resultado.TotalResults > 0 && resultado.Results.Count > 0)
                ExibirCards(resultado.Results, fonte, origem);

            _resultadoAtual = resultado;
            AtualizarStatus(fonte.ObterStatusResultado(resultado, consulta));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // O fechamento da aba cancela a consulta sem exibir dialogos na janela encerrada.
        }
        catch (Exception exception)
        {
            if (_encerrado)
                return;

            LimparCards();
            var erro = fonte.DescreverErro(exception);
            AtualizarStatus(erro.Status);
            _mostrarMensagem(erro.Mensagem, erro.Titulo, erro.Icone);
        }
        finally
        {
            _carregando = false;
            if (!_encerrado)
                AtualizarControles();
        }
    }

    private IAnimeSearchSource ObterFonte(AnimeSearchSourceKind origem) => origem switch
    {
        AnimeSearchSourceKind.DBLocal => _fonteLocal,
        AnimeSearchSourceKind.ApiMyAnimeList => _fonteExterna,
        _ => throw new ArgumentOutOfRangeException(nameof(origem))
    };

    private void ExibirCards(
        IEnumerable<AnimeSearchCard> animes,
        IAnimeSearchSource fonte,
        AnimeSearchSourceKind origem)
    {
        _flpCards.SuspendLayout();
        try
        {
            foreach (var anime in animes)
            {
                var card = new UC_AnimeCard();
                card.CarregarDados(anime, usarFallbackMyAnimeList: fonte.UsarFallbackMyAnimeList);
                card.CardClicado += (_, malId) =>
                {
                    if (malId <= 0)
                        return;

                    if (origem == AnimeSearchSourceKind.DBLocal)
                        AnimeLocalSelecionado?.Invoke(this, malId);
                    else
                        AnimeMyAnimeListSelecionado?.Invoke(this, malId);
                };
                _flpCards.Controls.Add(card);
            }
        }
        finally
        {
            _flpCards.ResumeLayout();
        }
    }

    private void AtualizarControles()
    {
        _txbBusca.Enabled = !_carregando;
        _btnBuscaLocal.Enabled = !_carregando;
        _btnBuscaExterna.Enabled = !_carregando;
        var resultado = !_carregando ? _resultadoAtual : null;
        _btnAnterior.Enabled = resultado is { TotalResults: > 0, Results.Count: > 0, CurrentPage: > 1 };
        _btnProximo.Enabled = resultado is { TotalResults: > 0, Results.Count: > 0, HasNextPage: true }
            && resultado.CurrentPage < resultado.TotalPages;
    }

    private void DestacarFonte(AnimeSearchSourceKind origem)
    {
        ConfigurarDestaque(_btnBuscaLocal, origem == AnimeSearchSourceKind.DBLocal);
        ConfigurarDestaque(_btnBuscaExterna, origem == AnimeSearchSourceKind.ApiMyAnimeList);
    }

    private static void ConfigurarDestaque(Button button, bool selecionado)
    {
        button.FlatAppearance.BorderSize = selecionado ? 2 : 1;
        button.BackColor = selecionado ? DarkModeColors.AccentColor : DarkModeColors.ElevatedColor;
        button.ForeColor = selecionado ? DarkModeColors.TextColor : Color.White;
    }

    private void AtualizarStatus(string mensagem)
    {
        _lblStatus.Text = mensagem;
        _lblStatus.AccessibleDescription = mensagem;
        _toolTip.SetToolTip(_lblStatus, mensagem);
    }

    private void LimparCards()
    {
        var cards = _flpCards.Controls.OfType<UC_AnimeCard>().ToList();
        _flpCards.Controls.Clear();
        foreach (var card in cards)
            card.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_encerrado)
        {
            _encerrado = true;
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
            _toolTip.Dispose();
        }
        base.Dispose(disposing);
    }
}
