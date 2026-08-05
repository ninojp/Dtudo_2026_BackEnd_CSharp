using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.MyAnimeList;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public class FUC_DBLocalBuscarAnime : UserControl
{
    public event EventHandler<int>? AnimeLocalSelecionado;

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

    public FUC_DBLocalBuscarAnime()
    {
        var lblTitulo = new Label
        {
            AutoSize = true,
            Text = "📁 Busca de animes - DB_Local",
            Font = new Font("Segoe UI Black", 16F, FontStyle.Bold),
            ForeColor = Color.Gold
        };

        var lblInput = new Label
        {
            AutoSize = true,
            Text = "Digite o título do anime",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.Gold
        };

        _txbBusca = new TextBox
        {
            Font = new Font("Segoe UI", 14F)
        };

        _btnBuscar = new Button
        {
            Text = "🔍 Buscar"
        };

        _lblStatus = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 12F),
            ForeColor = Color.Gold,
            Text = "Informe o nome e clique em Buscar."
        };

        _flpCards = new FlowLayoutPanel
        {
            BackColor = Color.Black
        };

        _btnAnterior = new Button
        {
            Text = "◄ Anterior",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Enabled = false
        };

        _btnProxima = new Button
        {
            Text = "Próxima ►",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Enabled = false
        };

        _lblPagina = new Label
        {
            Text = "—",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gold
        };

        AnimeSearchLayout.Build(
            this,
            lblTitulo,
            lblInput,
            _txbBusca,
            _btnBuscar,
            _lblStatus,
            _flpCards,
            _btnAnterior,
            _lblPagina,
            _btnProxima);

        _btnBuscar.Click += async (_, _) => await BuscarPrimeiraPaginaAsync();
        _btnAnterior.Click += async (_, _) => await BuscarPaginaAsync(Math.Max(1, _paginaAtual - 1));
        _btnProxima.Click += async (_, _) => await BuscarPaginaAsync(_paginaAtual + 1);
        _txbBusca.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await BuscarPrimeiraPaginaAsync();
        };

        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    private async Task BuscarPrimeiraPaginaAsync()
    {
        var termo = _txbBusca.Text.Trim();
        if (string.IsNullOrWhiteSpace(termo))
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Digite o nome para buscar na ApiMyAnimes.", "Aviso",
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
        _lblStatus.Text = "⏳ Buscando animes no DB_Local...";
        _lblPagina.Text = "Carregando...";
        _btnAnterior.Enabled = false;
        _btnProxima.Enabled = false;

        try
        {
            var resultado = await _apiMyAnimesService.BuscarAnimesPorTituloAsync(_consultaAtual, pagina, 20);
            _paginaAtual = Math.Max(1, resultado.CurrentPage);

            if (resultado.TotalResults == 0 || resultado.Results.Count == 0)
            {
                _lblStatus.Text = "Nenhum anime encontrado no DB_Local.";
                _lblPagina.Text = "Página 1 de 1 | 0 resultado(s)";
                return;
            }

            _flpCards.SuspendLayout();
            foreach (var anime in resultado.Results)
            {
                var card = new UC_AnimeCard();
                card.CarregarDados(MapearParaCard(anime), usarFallbackMyAnimeList: false);
                var malIdSelecionado = anime.MalId;
                card.CardClicado += (_, _) =>
                {
                    if (malIdSelecionado > 0)
                        AnimeLocalSelecionado?.Invoke(this, malIdSelecionado);
                };
                _flpCards.Controls.Add(card);
            }
            _flpCards.ResumeLayout();

            _lblStatus.Text = $"✅ Busca local em ANIMES concluída. {resultado.TotalResults:N0} resultado(s).";
            _lblPagina.Text =
                $"Página {resultado.CurrentPage} de {resultado.TotalPages} | {resultado.TotalResults:N0} resultado(s)";
            _btnAnterior.Enabled = resultado.CurrentPage > 1;
            _btnProxima.Enabled = resultado.HasNextPage && resultado.CurrentPage < resultado.TotalPages;
        }
        catch (HttpRequestException ex)
        {
            _lblStatus.Text = "❌ Erro de conexão com ApiMyAnimes.";
            _lblPagina.Text = "—";
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Não foi possível conectar à ApiMyAnimes em:\n{ApiMyAnimesService.ApiBase}\n\nDetalhes: {ex.Message}",
                "Erro de Conexão",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao buscar na ApiMyAnimes.";
            _lblPagina.Text = "—";
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao buscar animes locais:\n\n{ex.Message}", "Erro",
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
        SuspendLayout();
        // 
        // FUC_DBLocalBuscarAnime
        // 
        BackColor = SystemColors.Desktop;
        Font = new Font("Microsoft Sans Serif", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
        ForeColor = Color.Gold;
        Name = "FUC_DBLocalBuscarAnime";
        Size = new Size(848, 640);
        ResumeLayout(false);

    }

    private static AnimeSearchCard MapearParaCard(ObterAnimeDto anime)
    {
        return new AnimeSearchCard
        {
            MalId = anime.MalId,
            Title = !string.IsNullOrWhiteSpace(anime.Titulo) ? anime.Titulo : anime.Title,
            TitleEnglish = anime.TitleEnglish,
            TitleJapanese = anime.TitleJapanese,
            TitleSynonyms = anime.TitleSynonyms,
            Type = anime.Type ?? "Anime local",
            Score = anime.Score,
            Year = anime.Year,
            ImageUrl = anime.ImagensUrlMal?.FirstOrDefault()
        };
    }
}
