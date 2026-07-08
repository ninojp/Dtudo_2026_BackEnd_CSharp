using LibDtudo.Shared.Dtos;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public class FUC_BuscarPorNomeLocal : UserControl
{
    public event EventHandler<int>? MyAnimeSelecionado;

    private readonly ApiMyAnimesService _apiMyAnimesService = new();

    private readonly Label _lblTitulo;
    private readonly Label _lblInput;
    private readonly TextBox _txbBusca;
    private readonly Button _btnBuscar;
    private readonly Label _lblStatus;
    private readonly FlowLayoutPanel _flpCards;
    private readonly Button _btnAnterior;
    private readonly Button _btnProxima;
    private readonly Label _lblPagina;

    private int _paginaAtual = 1;
    private string _consultaAtual = string.Empty;
    private bool _carregando;

    public FUC_BuscarPorNomeLocal()
    {
        var tlpMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Black
        };
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 126F));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

        var pnlTopo = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };

        _lblTitulo = new Label
        {
            AutoSize = true,
            Text = "📁 Busca Local (ApiMyAnimes)",
            Font = new Font("Segoe UI Black", 12F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(20, 16)
        };

        _lblInput = new Label
        {
            AutoSize = true,
            Text = "Digite o nome do anime:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(20, 62)
        };

        _txbBusca = new TextBox
        {
            Location = new Point(260, 56),
            Width = 320
        };

        _btnBuscar = new Button
        {
            Text = "🔍 Buscar",
            Location = new Point(590, 54),
            Width = 120,
            Height = 34
        };

        _lblStatus = new Label
        {
            AutoSize = true,
            ForeColor = Color.DarkGray,
            Location = new Point(20, 96)
        };

        pnlTopo.Controls.Add(_lblTitulo);
        pnlTopo.Controls.Add(_lblInput);
        pnlTopo.Controls.Add(_txbBusca);
        pnlTopo.Controls.Add(_btnBuscar);
        pnlTopo.Controls.Add(_lblStatus);

        _flpCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Black,
            Padding = new Padding(8, 6, 8, 6)
        };

        var pnlPaginacao = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };

        _btnAnterior = new Button
        {
            Text = "◄ Anterior",
            Width = 130,
            Height = 34,
            Location = new Point(12, 8),
            Enabled = false
        };

        _btnProxima = new Button
        {
            Text = "Próxima ►",
            Width = 130,
            Height = 34,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Enabled = false
        };

        _lblPagina = new Label
        {
            Text = "—",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            ForeColor = Color.Gold
        };

        pnlPaginacao.Controls.Add(_lblPagina);
        pnlPaginacao.Controls.Add(_btnAnterior);
        pnlPaginacao.Controls.Add(_btnProxima);

        tlpMain.Controls.Add(pnlTopo, 0, 0);
        tlpMain.Controls.Add(_flpCards, 0, 1);
        tlpMain.Controls.Add(pnlPaginacao, 0, 2);

        Controls.Add(tlpMain);

        _btnBuscar.Click += BtnBuscar_Click;
        _btnAnterior.Click += BtnAnterior_Click;
        _btnProxima.Click += BtnProxima_Click;
        _txbBusca.KeyDown += TxbBusca_KeyDown;
        pnlPaginacao.Resize += (_, _) =>
        {
            _btnProxima.Left = Math.Max(12, pnlPaginacao.Width - _btnProxima.Width - 12);
        };

        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    private async void BtnBuscar_Click(object? sender, EventArgs e)
    {
        var query = _txbBusca.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            MessageBox.Show("Digite o nome do anime para buscar na ApiMyAnimes.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txbBusca.Focus();
            return;
        }

        _consultaAtual = query;
        _paginaAtual = 1;
        await ExecutarBuscaAsync();
    }

    private async void BtnAnterior_Click(object? sender, EventArgs e)
    {
        if (_paginaAtual <= 1) return;
        _paginaAtual--;
        await ExecutarBuscaAsync();
    }

    private async void BtnProxima_Click(object? sender, EventArgs e)
    {
        _paginaAtual++;
        await ExecutarBuscaAsync();
    }

    private async void TxbBusca_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;

        e.SuppressKeyPress = true;
        _btnBuscar.PerformClick();
        await Task.CompletedTask;
    }

    private async Task ExecutarBuscaAsync()
    {
        if (_carregando) return;

        _carregando = true;
        SetControlesHabilitados(false);
        LimparCards();
        _lblStatus.Text = "⏳ Buscando na ApiMyAnimes...";
        _lblPagina.Text = "Carregando...";

        try
        {
            var resultado = await _apiMyAnimesService.BuscarAnimesPorNomeAsync(_consultaAtual, _paginaAtual, 20);

            if (resultado.Results.Count == 0)
            {
                _lblStatus.Text = "Nenhum anime local encontrado.";
                _lblPagina.Text = "—";
                _btnAnterior.Enabled = false;
                _btnProxima.Enabled = false;
                return;
            }

            _flpCards.SuspendLayout();
            foreach (var animeLocal in resultado.Results)
            {
                var card = new UC_AnimeCard();
                card.CarregarDados(MapearParaCard(animeLocal));
                var myAnimeIdSelecionado = animeLocal.MyAnimeID;
                card.CardClicado += (_, _) =>
                {
                    if (myAnimeIdSelecionado > 0)
                        MyAnimeSelecionado?.Invoke(this, myAnimeIdSelecionado);
                };
                _flpCards.Controls.Add(card);
            }
            _flpCards.ResumeLayout();

            _lblStatus.Text = $"✅ {resultado.TotalResults:N0} anime(s) local(is) encontrado(s).";
            _lblPagina.Text = $"Página {resultado.CurrentPage} de {resultado.TotalPages} | {resultado.TotalResults:N0} resultado(s)";
            _btnAnterior.Enabled = resultado.CurrentPage > 1;
            _btnProxima.Enabled = resultado.HasNextPage;
        }
        catch (HttpRequestException ex)
        {
            _lblStatus.Text = "❌ Erro de conexão com ApiMyAnimes.";
            _lblPagina.Text = "—";
            MessageBox.Show(
                $"Não foi possível conectar à ApiMyAnimes em:\n{ApiMyAnimesService.ApiBase}\n\nDetalhes: {ex.Message}",
                "Erro de Conexão",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao buscar localmente.";
            _lblPagina.Text = "—";
            MessageBox.Show($"Erro ao buscar animes locais:\n\n{ex.Message}", "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _carregando = false;
            SetControlesHabilitados(true);
        }
    }

    private void SetControlesHabilitados(bool habilitado)
    {
        _btnBuscar.Enabled = habilitado;
        _txbBusca.Enabled = habilitado;
    }

    private void LimparCards()
    {
        var cards = _flpCards.Controls.OfType<UC_AnimeCard>().ToList();
        _flpCards.Controls.Clear();
        foreach (var card in cards)
            card.Dispose();
    }

    private static JikanAnimeCard MapearParaCard(ObterAnimeDto anime)
    {
        return new JikanAnimeCard
        {
            MalId = anime.MalId,
            Title = anime.Titulo,
            TitleEnglish = anime.TitleEnglish,
            Type = anime.Type,
            Score = anime.Score,
            Year = anime.Year,
            ImageUrl = anime.ImagensUrlMal.FirstOrDefault()
        };
    }
}
