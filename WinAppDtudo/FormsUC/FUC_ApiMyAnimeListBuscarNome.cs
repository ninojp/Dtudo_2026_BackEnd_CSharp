using System.Net;
using LibDtudo.Shared.Dtos.MyAnimeList;
using System.Text.RegularExpressions;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public sealed class FUC_ApiMyAnimeListBuscarNome : UserControl
{
    public event EventHandler<int>? AnimeMyAnimeListSelecionado;

    private readonly MyAnimeListApiService _apiService;
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

    public FUC_ApiMyAnimeListBuscarNome(WinAppAuthenticationService? authenticationService = null)
    {
        _apiService = new MyAnimeListApiService(authenticationService);
        var lblTitulo = new Label
        {
            AutoSize = true,
            Text = "🧭 Busca Externa - ApiMyAnimeList",
            Font = new Font("Segoe UI Black", 16F, FontStyle.Bold),
            ForeColor = Color.Gold
        };
        var lblInput = new Label
        {
            AutoSize = true,
            Text = "Digite o nome ou ID do anime:",
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
            ForeColor = Color.Gold,
            Font = new Font("Segoe UI", 12F),
            Text = "Informe o nome e clique em Buscar."
        };

        _flpCards = new FlowLayoutPanel
        {
            BackColor = Color.Black
        };
        _btnAnterior = new Button { 
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
        Name = nameof(FUC_ApiMyAnimeListBuscarNome);
        BackColor = SystemColors.AppWorkspace;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    private async Task BuscarPrimeiraPaginaAsync()
    {
        var texto = _txbBusca.Text;
        if (Regex.IsMatch(texto, "^[0-9]+$"))
        {
            if (!int.TryParse(texto, out var malId) || malId is < 1 or > 100000)
            {
                WinAppDtudo.Services.DarkMessageBox.Show("Informe um ID de anime entre 1 e 100000.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txbBusca.Focus();
                return;
            }

            _consultaAtual = string.Empty;
            await BuscarPorIdAsync(malId);
            return;
        }

        var termo = texto.Trim();
        if (string.IsNullOrWhiteSpace(termo))
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Digite o nome para buscar na ApiMyAnimeList.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txbBusca.Focus();
            return;
        }

        _consultaAtual = termo;
        await BuscarPaginaAsync(1);
    }

    private async Task BuscarPorIdAsync(int malId)
    {
        if (_carregando) return;

        _carregando = true;
        SetControlesBuscaHabilitados(false);
        LimparCards();
        _lblStatus.Text = "⏳ Buscando ID na ApiMyAnimeList...";
        _lblPagina.Text = "Carregando...";
        _btnAnterior.Enabled = false;
        _btnProxima.Enabled = false;

        try
        {
            var anime = await _apiService.BuscarPorIdAsync(malId);
            if (anime is null)
            {
                _lblStatus.Text = "Nenhum anime encontrado na ApiMyAnimeList.";
                _lblPagina.Text = "—";
                return;
            }

            var card = new UC_AnimeCard();
            card.CarregarDados(MapearParaCard(anime));
            card.CardClicado += (_, _) => AnimeMyAnimeListSelecionado?.Invoke(this, anime.MalId);
            _flpCards.Controls.Add(card);

            _lblStatus.Text = "✅ Busca por ID na ApiMyAnimeList concluída.";
            _lblPagina.Text = "Resultado único";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _lblStatus.Text = "Nenhum anime encontrado na ApiMyAnimeList.";
            _lblPagina.Text = "—";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.GatewayTimeout)
        {
            _lblStatus.Text = "⚠️ ApiMyAnimeList retornou 504 (Gateway Timeout).";
            _lblPagina.Text = "—";
            WinAppDtudo.Services.DarkMessageBox.Show("A ApiMyAnimeList demorou para responder (504).\nTente novamente em instantes.", "ApiMyAnimeList indisponível", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (HttpRequestException ex)
        {
            _lblStatus.Text = "❌ Erro de conexão com ApiMyAnimeList.";
            _lblPagina.Text = "—";
            WinAppDtudo.Services.DarkMessageBox.Show($"Não foi possível conectar à ApiMyAnimeList em:\n{MyAnimeListApiService.ApiBase}\n\nDetalhes: {ex.Message}", "Erro de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao buscar na ApiMyAnimeList.";
            _lblPagina.Text = "—";
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao buscar o anime na ApiMyAnimeList:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _carregando = false;
            SetControlesBuscaHabilitados(true);
        }
    }

    private static AnimeSearchCard MapearParaCard(AnimeDetails anime)
        => new()
        {
            MalId = anime.MalId,
            Url = anime.Url,
            Title = anime.Title,
            TitleEnglish = anime.TitleEnglish,
            TitleJapanese = anime.TitleJapanese,
            TitleSynonyms = anime.TitleSynonyms,
            ImageUrl = anime.Images?.Jpg?.LargeImageUrl ?? anime.Images?.Jpg?.ImageUrl,
            Type = anime.Type,
            Episodes = anime.Episodes,
            Status = anime.Status,
            Score = anime.Score,
            Year = anime.Year,
            Genres = anime.Genres
        };

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
            WinAppDtudo.Services.DarkMessageBox.Show("A ApiMyAnimeList demorou para responder (504).\nTente novamente em instantes.", "ApiMyAnimeList indisponível", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (HttpRequestException ex)
        {
            _lblStatus.Text = "❌ Erro de conexão com ApiMyAnimeList.";
            _lblPagina.Text = "—";
            WinAppDtudo.Services.DarkMessageBox.Show($"Não foi possível conectar à ApiMyAnimeList em:\n{MyAnimeListApiService.ApiBase}\n\nDetalhes: {ex.Message}", "Erro de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao buscar na ApiMyAnimeList.";
            _lblPagina.Text = "—";
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao buscar animes na ApiMyAnimeList:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
