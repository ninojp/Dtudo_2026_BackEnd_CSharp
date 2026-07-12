using System.Net;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public sealed class FUC_ApiMyAnimeListBuscarNome : UserControl
{
    public event EventHandler<int>? AnimeMyAnimeListSelecionado;

    private readonly MyAnimeListApiService _apiService = new();
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

    public FUC_ApiMyAnimeListBuscarNome()
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
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));

        var pnlTopo = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
        var lblTitulo = new Label
        {
            AutoSize = true,
            Text = "🧭 Busca Externa (ApiMyAnimeList)",
            Font = new Font("Segoe UI Black", 12F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(20, 26)
        };
        var lblInput = new Label
        {
            AutoSize = true,
            Text = "Digite o nome do anime:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(650, 26)
        };

        _txbBusca = new TextBox { Location = new Point(580, 70), Width = 520 };
        _btnBuscar = new Button { Text = "🔍 Buscar", Location = new Point(1200, 60), Width = 200, Height = 44 };
        _lblStatus = new Label
        {
            AutoSize = true,
            ForeColor = Color.Gold,
            Location = new Point(1450, 60),
            Text = "Informe o nome e clique em Buscar."
        };

        pnlTopo.Controls.AddRange([lblTitulo, lblInput, _txbBusca, _btnBuscar, _lblStatus]);

        _flpCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Black,
            Padding = new Padding(8, 6, 8, 6)
        };

        var pnlPaginacao = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
        _btnAnterior = new Button { Text = "◄ Anterior", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Width = 160, Height = 48, Location = new Point(12, 10), Enabled = false };
        _btnProxima = new Button
        {
            Text = "Próxima ►",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Width = 160,
            Height = 48,
            Location = new Point(12, 10),
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

        pnlPaginacao.Controls.AddRange([_btnAnterior, _btnProxima, _lblPagina]);
        _lblPagina.SendToBack();
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
            _btnProxima.Left = Math.Max(12, pnlPaginacao.Width - _btnProxima.Width - 12);

        DoubleBuffered = true;
        Name = nameof(FUC_ApiMyAnimeListBuscarNome);
        BackColor = SystemColors.AppWorkspace;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    private async Task BuscarPrimeiraPaginaAsync()
    {
        var termo = _txbBusca.Text.Trim();
        if (string.IsNullOrWhiteSpace(termo))
        {
            MessageBox.Show("Digite o nome para buscar na ApiMyAnimeList.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txbBusca.Focus();
            return;
        }

        _consultaAtual = termo;
        await BuscarPaginaAsync(1);
    }

    private async Task BuscarPaginaAsync(int pagina)
    {
        if (_carregando || string.IsNullOrWhiteSpace(_consultaAtual)) return;

        _carregando = true;
        SetControlesBuscaHabilitados(false);
        LimparCards();
        _lblStatus.Text = "⏳ Buscando na ApiMyAnimeList...";
        _lblPagina.Text = "Carregando...";
        _btnAnterior.Enabled = false;
        _btnProxima.Enabled = false;

        try
        {
            var resultado = await _apiService.BuscarPorNomeAsync(_consultaAtual, pagina);
            _paginaAtual = Math.Max(1, resultado.CurrentPage);

            if (resultado.TotalResults == 0 || resultado.Results.Count == 0)
            {
                _lblStatus.Text = "Nenhum resultado encontrado na ApiMyAnimeList.";
                _lblPagina.Text = "Página 1 de 1 | 0 resultado(s)";
                return;
            }

            _flpCards.SuspendLayout();
            foreach (var anime in resultado.Results)
            {
                var card = new UC_AnimeCard();
                card.CarregarDados(anime);
                var malId = anime.MalId;
                card.CardClicado += (_, _) => AnimeMyAnimeListSelecionado?.Invoke(this, malId);
                _flpCards.Controls.Add(card);
            }
            _flpCards.ResumeLayout();

            _lblStatus.Text = $"✅ Busca na ApiMyAnimeList concluída. {resultado.TotalResults:N0} resultado(s).";
            _lblPagina.Text = $"Página {resultado.CurrentPage} de {resultado.TotalPages} | {resultado.TotalResults:N0} resultado(s)";
            _btnAnterior.Enabled = resultado.CurrentPage > 1;
            _btnProxima.Enabled = resultado.HasNextPage && resultado.CurrentPage < resultado.TotalPages;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.GatewayTimeout)
        {
            _lblStatus.Text = "⚠️ ApiMyAnimeList retornou 504 (Gateway Timeout).";
            _lblPagina.Text = "—";
            MessageBox.Show("A ApiMyAnimeList demorou para responder (504).\nTente novamente em instantes.", "ApiMyAnimeList indisponível", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (HttpRequestException ex)
        {
            _lblStatus.Text = "❌ Erro de conexão com ApiMyAnimeList.";
            _lblPagina.Text = "—";
            MessageBox.Show($"Não foi possível conectar à ApiMyAnimeList em:\n{MyAnimeListApiService.ApiBase}\n\nDetalhes: {ex.Message}", "Erro de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao buscar na ApiMyAnimeList.";
            _lblPagina.Text = "—";
            MessageBox.Show($"Erro ao buscar animes na ApiMyAnimeList:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        foreach (var card in cards) card.Dispose();
    }
}
