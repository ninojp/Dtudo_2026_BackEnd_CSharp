using System.Net;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public class FUC_ApiJikanBuscarNome : UserControl
{
    public event EventHandler<int>? AnimeJikanSelecionado;

    private readonly JikanApiService _jikanService = new();

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

    public FUC_ApiJikanBuscarNome()
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
            Text = "🧭 Busca Externa (ApiJikan)",
            Font = new Font("Segoe UI Black", 12F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(20, 16)
        };

        var lblInput = new Label
        {
            AutoSize = true,
            Text = "Digite o nome do anime:",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(20, 62)
        };

        _txbBusca = new TextBox
        {
            Location = new Point(250, 56),
            Width = 320
        };

        _btnBuscar = new Button
        {
            Text = "🔍 Buscar",
            Location = new Point(580, 54),
            Width = 120,
            Height = 34
        };

        _lblStatus = new Label
        {
            AutoSize = true,
            ForeColor = Color.DarkGray,
            Location = new Point(20, 96),
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
            MessageBox.Show("Digite o nome para buscar na ApiJikan.", "Aviso",
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
        _lblStatus.Text = "⏳ Buscando na ApiJikan...";
        _lblPagina.Text = "Carregando...";
        _btnAnterior.Enabled = false;
        _btnProxima.Enabled = false;

        try
        {
            var resultado = await _jikanService.BuscarPorNomeAsync(_consultaAtual, pagina);
            _paginaAtual = Math.Max(1, resultado.CurrentPage);

            if (resultado.TotalResults == 0 || resultado.Results.Count == 0)
            {
                _lblStatus.Text = "Nenhum resultado encontrado na ApiJikan.";
                _lblPagina.Text = "Página 1 de 1 | 0 resultado(s)";
                return;
            }

            _flpCards.SuspendLayout();
            foreach (var anime in resultado.Results)
            {
                var card = new UC_AnimeCard();
                card.CarregarDados(anime);
                var malId = anime.MalId;
                card.CardClicado += (_, _) => AnimeJikanSelecionado?.Invoke(this, malId);
                _flpCards.Controls.Add(card);
            }
            _flpCards.ResumeLayout();

            _lblStatus.Text = $"✅ Busca na ApiJikan concluída. {resultado.TotalResults:N0} resultado(s).";
            _lblPagina.Text =
                $"Página {resultado.CurrentPage} de {resultado.TotalPages} | {resultado.TotalResults:N0} resultado(s)";
            _btnAnterior.Enabled = resultado.CurrentPage > 1;
            _btnProxima.Enabled = resultado.HasNextPage && resultado.CurrentPage < resultado.TotalPages;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.GatewayTimeout)
        {
            _lblStatus.Text = "⚠️ ApiJikan retornou 504 (Gateway Timeout).";
            _lblPagina.Text = "—";
            MessageBox.Show(
                "A ApiJikan demorou para responder (504).\nTente novamente em instantes.",
                "ApiJikan indisponível momentaneamente",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (HttpRequestException ex)
        {
            _lblStatus.Text = "❌ Erro de conexão com ApiJikan.";
            _lblPagina.Text = "—";
            MessageBox.Show(
                $"Não foi possível conectar à ApiJikan em:\n{JikanApiService.ApiBase}\n\nDetalhes: {ex.Message}",
                "Erro de Conexão",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao buscar na ApiJikan.";
            _lblPagina.Text = "—";
            MessageBox.Show($"Erro ao buscar animes na ApiJikan:\n\n{ex.Message}", "Erro",
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
}
