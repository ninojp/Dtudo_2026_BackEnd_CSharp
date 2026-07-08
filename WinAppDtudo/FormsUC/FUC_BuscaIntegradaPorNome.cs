using LibDtudo.Shared.Dtos;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public class FUC_BuscaIntegradaPorNome : UserControl
{
    public event EventHandler<int>? AnimeJikanSelecionado;
    public event EventHandler<int>? MyAnimeSelecionado;

    private readonly ApiMyAnimesService _apiMyAnimesService = new();
    private readonly JikanApiService _jikanApiService = new();

    private readonly TextBox _txbBusca;
    private readonly Button _btnBuscarLocal;
    private readonly Button _btnBuscarJikan;

    private readonly TabControl _tabResultados;
    private readonly FlowLayoutPanel _flpLocal;
    private readonly FlowLayoutPanel _flpJikan;

    private readonly Button _btnLocalAnterior;
    private readonly Button _btnLocalProxima;
    private readonly Label _lblLocalPagina;

    private readonly Button _btnJikanAnterior;
    private readonly Button _btnJikanProxima;
    private readonly Label _lblJikanPagina;

    private readonly Label _lblStatus;

    private string _consultaAtual = string.Empty;

    private int _paginaLocal = 1;
    private int _totalPaginasLocal = 1;
    private int _paginaJikan = 1;

    private bool _carregandoLocal;
    private bool _carregandoJikan;

    public FUC_BuscaIntegradaPorNome()
    {
        var tlpMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Black
        };
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));

        var pnlTopo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = Color.Black,
            Padding = new Padding(12, 10, 12, 6)
        };
        pnlTopo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        pnlTopo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        pnlTopo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        pnlTopo.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        pnlTopo.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        var lblLocal = new Label
        {
            Text = "Buscar Anime (ApiMyAnimes)",
            AutoSize = true,
            ForeColor = Color.Gold,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Anchor = AnchorStyles.Left
        };

        _txbBusca = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            Margin = new Padding(8, 5, 8, 5)
        };

        var lblJikan = new Label
        {
            Text = "Buscar Anime (ApiJikan)",
            AutoSize = true,
            ForeColor = Color.Gold,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Anchor = AnchorStyles.Right
        };

        _btnBuscarLocal = new Button
        {
            Text = "Buscar",
            Width = 130,
            Height = 34,
            Anchor = AnchorStyles.Left
        };

        _btnBuscarJikan = new Button
        {
            Text = "Buscar",
            Width = 130,
            Height = 34,
            Anchor = AnchorStyles.Right
        };

        pnlTopo.Controls.Add(lblLocal, 0, 0);
        pnlTopo.Controls.Add(_txbBusca, 1, 0);
        pnlTopo.Controls.Add(lblJikan, 2, 0);
        pnlTopo.Controls.Add(_btnBuscarLocal, 0, 1);
        pnlTopo.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 1, 1);
        pnlTopo.Controls.Add(_btnBuscarJikan, 2, 1);

        _tabResultados = new TabControl
        {
            Dock = DockStyle.Fill,
            Alignment = TabAlignment.Top,
            Appearance = TabAppearance.Normal
        };

        var tabLocal = new TabPage("ApiMyAnimes");
        var tabJikan = new TabPage("ApiJikan");

        (_flpLocal, _btnLocalAnterior, _lblLocalPagina, _btnLocalProxima) = CriarConteudoAba();
        (_flpJikan, _btnJikanAnterior, _lblJikanPagina, _btnJikanProxima) = CriarConteudoAba();

        tabLocal.Controls.Add(_flpLocal.Parent!);
        tabJikan.Controls.Add(_flpJikan.Parent!);

        _tabResultados.TabPages.Add(tabLocal);
        _tabResultados.TabPages.Add(tabJikan);

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.DarkGray,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Text = "Digite o nome e escolha a API para buscar."
        };

        tlpMain.Controls.Add(pnlTopo, 0, 0);
        tlpMain.Controls.Add(_tabResultados, 0, 1);
        tlpMain.Controls.Add(_lblStatus, 0, 2);

        Controls.Add(tlpMain);

        _btnBuscarLocal.Click += async (_, _) => await BuscarLocalAsync(1);
        _btnBuscarJikan.Click += async (_, _) => await BuscarJikanAsync(1);

        _btnLocalAnterior.Click += async (_, _) => await BuscarLocalAsync(Math.Max(1, _paginaLocal - 1));
        _btnLocalProxima.Click += async (_, _) => await BuscarLocalAsync(_paginaLocal + 1);

        _btnJikanAnterior.Click += async (_, _) => await BuscarJikanAsync(Math.Max(1, _paginaJikan - 1));
        _btnJikanProxima.Click += async (_, _) => await BuscarJikanAsync(_paginaJikan + 1);

        _txbBusca.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (_tabResultados.SelectedTab?.Text == "ApiMyAnimes")
                await BuscarLocalAsync(1);
            else
                await BuscarJikanAsync(1);
        };

        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    private static (FlowLayoutPanel flpCards, Button btnAnterior, Label lblPagina, Button btnProxima) CriarConteudoAba()
    {
        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Black
        };
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

        var flp = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Black,
            Padding = new Padding(8, 6, 8, 6)
        };

        var pnlPag = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };

        var btnAnterior = new Button
        {
            Text = "◄ Anterior",
            Width = 130,
            Height = 34,
            Location = new Point(10, 8),
            Enabled = false
        };

        var lblPagina = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gold,
            Text = "—"
        };

        var btnProxima = new Button
        {
            Text = "Próxima ►",
            Width = 130,
            Height = 34,
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        pnlPag.Controls.Add(lblPagina);
        pnlPag.Controls.Add(btnAnterior);
        pnlPag.Controls.Add(btnProxima);
        pnlPag.Resize += (_, _) => btnProxima.Left = Math.Max(10, pnlPag.Width - btnProxima.Width - 10);

        tlp.Controls.Add(flp, 0, 0);
        tlp.Controls.Add(pnlPag, 0, 1);

        return (flp, btnAnterior, lblPagina, btnProxima);
    }

    private async Task BuscarLocalAsync(int pagina)
    {
        if (_carregandoLocal) return;

        var termo = _txbBusca.Text.Trim();
        if (string.IsNullOrWhiteSpace(termo))
        {
            MessageBox.Show("Digite o nome do anime.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txbBusca.Focus();
            return;
        }

        _carregandoLocal = true;
        _consultaAtual = termo;
        _lblStatus.Text = "⏳ Buscando na ApiMyAnimes...";
        LimparCards(_flpLocal);
        _btnBuscarLocal.Enabled = false;

        try
        {
            var resultado = await _apiMyAnimesService.BuscarAnimesPorNomeAsync(_consultaAtual, pagina, 20);
            _paginaLocal = resultado.CurrentPage;
            _totalPaginasLocal = resultado.TotalPages;

            foreach (var anime in resultado.Results)
            {
                var card = new UC_AnimeCard();
                card.CarregarDados(MapearCardLocal(anime));
                var myAnimeId = anime.MyAnimeID;
                card.CardClicado += (_, _) =>
                {
                    if (myAnimeId > 0)
                        MyAnimeSelecionado?.Invoke(this, myAnimeId);
                };
                _flpLocal.Controls.Add(card);
            }

            _lblLocalPagina.Text = $"Página {_paginaLocal} de {_totalPaginasLocal} | {resultado.TotalResults:N0} resultado(s)";
            _btnLocalAnterior.Enabled = _paginaLocal > 1;
            _btnLocalProxima.Enabled = resultado.HasNextPage;

            _lblStatus.Text = resultado.TotalResults == 0
                ? "Nenhum resultado na ApiMyAnimes."
                : "✅ Busca na ApiMyAnimes concluída.";

            _tabResultados.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro na busca ApiMyAnimes.";
            _lblLocalPagina.Text = "—";
            _btnLocalAnterior.Enabled = false;
            _btnLocalProxima.Enabled = false;
            MessageBox.Show($"Erro ao buscar na ApiMyAnimes:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnBuscarLocal.Enabled = true;
            _carregandoLocal = false;
        }
    }

    private async Task BuscarJikanAsync(int pagina)
    {
        if (_carregandoJikan) return;

        var termo = _txbBusca.Text.Trim();
        if (string.IsNullOrWhiteSpace(termo))
        {
            MessageBox.Show("Digite o nome do anime.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txbBusca.Focus();
            return;
        }

        _carregandoJikan = true;
        _consultaAtual = termo;
        _lblStatus.Text = "⏳ Buscando na ApiJikan...";
        LimparCards(_flpJikan);
        _btnBuscarJikan.Enabled = false;

        try
        {
            var resultado = await _jikanApiService.BuscarPorNomeAsync(_consultaAtual, pagina);
            _paginaJikan = resultado.CurrentPage;

            foreach (var anime in resultado.Results)
            {
                var card = new UC_AnimeCard();
                card.CarregarDados(anime);
                var malId = anime.MalId;
                card.CardClicado += (_, _) => AnimeJikanSelecionado?.Invoke(this, malId);
                _flpJikan.Controls.Add(card);
            }

            _lblJikanPagina.Text = $"Página {resultado.CurrentPage} de {resultado.TotalPages} | {resultado.TotalResults:N0} resultado(s)";
            _btnJikanAnterior.Enabled = resultado.CurrentPage > 1;
            _btnJikanProxima.Enabled = resultado.HasNextPage;

            _lblStatus.Text = resultado.TotalResults == 0
                ? "Nenhum resultado na ApiJikan."
                : "✅ Busca na ApiJikan concluída.";

            _tabResultados.SelectedIndex = 1;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro na busca ApiJikan.";
            _lblJikanPagina.Text = "—";
            _btnJikanAnterior.Enabled = false;
            _btnJikanProxima.Enabled = false;
            MessageBox.Show($"Erro ao buscar na ApiJikan:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnBuscarJikan.Enabled = true;
            _carregandoJikan = false;
        }
    }

    private static void LimparCards(FlowLayoutPanel flp)
    {
        var cards = flp.Controls.OfType<UC_AnimeCard>().ToList();
        flp.Controls.Clear();
        foreach (var card in cards)
            card.Dispose();
    }

    private static JikanAnimeCard MapearCardLocal(ObterAnimeDto anime)
    {
        return new JikanAnimeCard
        {
            MalId = anime.MalId,
            Title = anime.Titulo,
            TitleEnglish = anime.TitleEnglish,
            Type = anime.Type,
            Year = anime.Year,
            Score = anime.Score,
            ImageUrl = anime.ImagensUrlMal.FirstOrDefault()
        };
    }
}
