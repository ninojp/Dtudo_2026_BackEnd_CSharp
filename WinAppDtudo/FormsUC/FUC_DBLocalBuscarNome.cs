using LibDtudo.Shared.Dtos;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public class FUC_DBLocalBuscarNome : UserControl
{
    public event EventHandler<int>? MyAnimeSelecionado;

    private readonly ApiMyAnimesService _apiMyAnimesService = new();

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

    public FUC_DBLocalBuscarNome()
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

        var lblTitulo = new Label
        {
            AutoSize = true,
            Text = "📁 Busca Local (ApiMyAnimes)",
            Font = new Font("Segoe UI Black", 12F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(20, 26)
        };

        var lblInput = new Label
        {
            AutoSize = true,
            Text = "Digite o título da coleção MyAnime:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(610, 26)
        };

        _txbBusca = new TextBox
        {
            Location = new Point(600, 70),
            Width = 500
        };

        _btnBuscar = new Button
        {
            Text = "🔍 Buscar",
            Location = new Point(1200, 60),
            Width = 200,
            Height = 44
        };

        _lblStatus = new Label
        {
            AutoSize = true,
            ForeColor = Color.DarkGray,
            Location = new Point(1450, 60),
            Text = "Informe o nome e clique em Buscar."
        };

        pnlTopo.Controls.Add(lblTitulo);
        pnlTopo.Controls.Add(lblInput);
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

        _btnBuscar.Click += async (_, _) => await BuscarPrimeiraPaginaAsync();
        _btnAnterior.Click += async (_, _) => await BuscarPaginaAsync(Math.Max(1, _paginaAtual - 1));
        _btnProxima.Click += async (_, _) => await BuscarPaginaAsync(_paginaAtual + 1);
        _txbBusca.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await BuscarPrimeiraPaginaAsync();
        };

        pnlPaginacao.Resize += (_, _) =>
        {
            _btnProxima.Left = Math.Max(12, pnlPaginacao.Width - _btnProxima.Width - 12);
        };

        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    private async Task BuscarPrimeiraPaginaAsync()
    {
        var termo = _txbBusca.Text.Trim();
        if (string.IsNullOrWhiteSpace(termo))
        {
            MessageBox.Show("Digite o nome para buscar na ApiMyAnimes.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txbBusca.Focus();
            return;
        }

        _consultaAtual = termo;
        await BuscarPaginaAsync(1);
    }

    private async Task BuscarPaginaAsync(int pagina)
    {
        if (_carregando) return;
        if (string.IsNullOrWhiteSpace(_consultaAtual))
            return;

        _carregando = true;
        SetControlesBuscaHabilitados(false);
        LimparCards();
        _lblStatus.Text = "⏳ Buscando na ApiMyAnimes...";
        _lblPagina.Text = "Carregando...";
        _btnAnterior.Enabled = false;
        _btnProxima.Enabled = false;

        try
        {
            var resultado = await _apiMyAnimesService.BuscarMyAnimesPorTituloAsync(_consultaAtual, pagina, 20);
            _paginaAtual = Math.Max(1, resultado.CurrentPage);

            if (resultado.TotalResults == 0 || resultado.Results.Count == 0)
            {
                _lblStatus.Text = "Nenhum MyAnime encontrado. Procurando em ANIMES...";
                var resultadoAnimes = await _apiMyAnimesService.BuscarAnimesPorTituloAsync(_consultaAtual, pagina, 20);
                _paginaAtual = Math.Max(1, resultadoAnimes.CurrentPage);

                if (resultadoAnimes.TotalResults == 0 || resultadoAnimes.Results.Count == 0)
                {
                    _lblStatus.Text = "Nenhum resultado encontrado em MyAnime ou ANIMES.";
                    _lblPagina.Text = "Página 1 de 1 | 0 resultado(s)";
                    return;
                }

                _flpCards.SuspendLayout();
                foreach (var anime in resultadoAnimes.Results)
                {
                    var card = new UC_AnimeCard();
                    card.CarregarDados(MapearParaCard(anime), malIdParaImagem: anime.MalId > 0 ? anime.MalId : null);
                    var myAnimeIdSelecionado = anime.MyAnimeID;
                    card.CardClicado += (_, _) =>
                    {
                        if (myAnimeIdSelecionado > 0)
                            MyAnimeSelecionado?.Invoke(this, myAnimeIdSelecionado);
                    };
                    _flpCards.Controls.Add(card);
                }
                _flpCards.ResumeLayout();

                _lblStatus.Text = $"✅ Busca local em ANIMES concluída. {resultadoAnimes.TotalResults:N0} resultado(s).";
                _lblPagina.Text =
                    $"Página {resultadoAnimes.CurrentPage} de {resultadoAnimes.TotalPages} | {resultadoAnimes.TotalResults:N0} resultado(s)";
                _btnAnterior.Enabled = resultadoAnimes.CurrentPage > 1;
                _btnProxima.Enabled = resultadoAnimes.HasNextPage && resultadoAnimes.CurrentPage < resultadoAnimes.TotalPages;
                return;
            }

            _flpCards.SuspendLayout();
            foreach (var myAnime in resultado.Results)
            {
                var card = new UC_AnimeCard();
                var malIdDaCapa = myAnime.AnimesMalId.FirstOrDefault();
                card.CarregarDados(MapearParaCard(myAnime), malIdParaImagem: malIdDaCapa > 0 ? malIdDaCapa : null);
                var myAnimeIdSelecionado = myAnime.Id;
                card.CardClicado += (_, _) =>
                {
                    if (myAnimeIdSelecionado > 0)
                        MyAnimeSelecionado?.Invoke(this, myAnimeIdSelecionado);
                };
                _flpCards.Controls.Add(card);
            }
            _flpCards.ResumeLayout();

            _lblStatus.Text = $"✅ Busca local concluída. {resultado.TotalResults:N0} resultado(s).";
            _lblPagina.Text =
                $"Página {resultado.CurrentPage} de {resultado.TotalPages} | {resultado.TotalResults:N0} resultado(s)";
            _btnAnterior.Enabled = resultado.CurrentPage > 1;
            _btnProxima.Enabled = resultado.HasNextPage && resultado.CurrentPage < resultado.TotalPages;
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
            _lblStatus.Text = "❌ Erro ao buscar na ApiMyAnimes.";
            _lblPagina.Text = "—";
            MessageBox.Show($"Erro ao buscar coleções locais:\n\n{ex.Message}", "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _carregando = false;
            SetControlesBuscaHabilitados(true);
        }
    }

    private void SetControlesBuscaHabilitados(bool habilitado)
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

    private void InitializeComponent()
    {

    }

    private static JikanAnimeCard MapearParaCard(ObterMyAnimeDto myAnime)
    {
        return new JikanAnimeCard
        {
            MalId = myAnime.Id,
            Title = myAnime.Titulo,
            TitleEnglish = null,
            Type = "Coleção MyAnime",
            Score = null,
            Year = null,
            ImageUrl = null
        };
    }

    private static JikanAnimeCard MapearParaCard(ObterAnimeDto anime)
    {
        return new JikanAnimeCard
        {
            MalId = anime.MalId,
            Title = !string.IsNullOrWhiteSpace(anime.Titulo) ? anime.Titulo : anime.Title,
            TitleEnglish = anime.TitleEnglish,
            Type = anime.Type ?? "Anime local",
            Score = anime.Score,
            Year = anime.Year,
            ImageUrl = anime.ImagensUrlMal?.FirstOrDefault()
        };
    }
}
