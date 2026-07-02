using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

/// <summary>
/// UserControl que exibe todos os detalhes disponíveis de um anime buscado por ID via ApiJikan.
/// Carrega os dados assincronamente ao ser exibido pela primeira vez.
/// </summary>
public partial class FUC_DetalhesAnime : UserControl
{
    private readonly JikanApiService _jikanService = new();
    private readonly int _malId;
    private int _yOffset;

    public FUC_DetalhesAnime(int malId)
    {
        InitializeComponent();
        _malId = malId;
        Load += async (s, e) => await CarregarAsync();
    }

    // ===================================================================

    private async Task CarregarAsync()
    {
        MostrarCarregando(true);

        JikanAnimeDetalhes? anime = null;
        string? erro = null;

        try
        {
            anime = await _jikanService.BuscarPorIdAsync(_malId);
        }
        catch (HttpRequestException ex)
        {
            erro = $"Não foi possível conectar à API Jikan.\n\n" +
                   $"Verifique se o ApiJikan está em execução em:\n{JikanApiService.ApiBase}\n\n" +
                   $"Detalhes: {ex.Message}";
        }
        catch (Exception ex)
        {
            erro = ex.Message;
        }

        // Torna o painel visível ANTES de popular para que ClientSize seja válido
        MostrarCarregando(false);

        if (erro != null)
        {
            MessageBox.Show($"Erro ao carregar detalhes:\n\n{erro}", "Erro de Conexão",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (anime == null)
        {
            MessageBox.Show($"Anime com ID {_malId} não encontrado.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        PopularUI(anime);
    }

    // ===================================================================

    private void PopularUI(JikanAnimeDetalhes anime)
    {
        // Header
        Lbl_TituloAnime.Text = anime.Title ?? $"Anime #{anime.MalId}";
        var exibicao = anime.Airing ? " (Em exibição)" : string.Empty;
        Lbl_TipoStatus.Text = $"{anime.Type ?? "?"}  •  {anime.Status ?? "?"}{exibicao}";

        // Imagem de capa
        _ = ImageLoaderService.CarregarEmPictureBoxAsync(Pbx_Capa,
            anime.Images?.Jpg?.LargeImageUrl ?? anime.Images?.Jpg?.ImageUrl);

        // Estatísticas rápidas (painel esquerdo)
        Lbl_Ano.Text = anime.Year.HasValue ? $"📅 {anime.Year}" : string.Empty;
        Lbl_ScoreStat.Text = anime.Score.HasValue ? $"⭐ {anime.Score:0.00}" : string.Empty;
        Lbl_Rank.Text = anime.Rank.HasValue ? $"🏆 Rank #{anime.Rank}" : string.Empty;
        Lbl_Popularidade.Text = anime.Popularity.HasValue ? $"👥 Pop. #{anime.Popularity}" : string.Empty;
        Lbl_Episodios.Text = anime.Episodes.HasValue ? $"📺 {anime.Episodes} ep." : string.Empty;
        Lbl_Duracao.Text = !string.IsNullOrWhiteSpace(anime.Duration) ? $"⏱ {anime.Duration}" : string.Empty;

        // Painel direito: detalhes dinâmicos
        Pnl_Info.SuspendLayout();
        Pnl_Info.Controls.Clear();
        _yOffset = 10;

        int larguraValor = Math.Max(Pnl_Info.ClientSize.Width - 165, 300);

        AdicionarDetalhe("MAL ID", anime.MalId.ToString(), larguraValor);
        AdicionarDetalhe("Título Original", anime.Title, larguraValor);
        AdicionarDetalhe("Título Inglês", anime.TitleEnglish, larguraValor);
        AdicionarDetalhe("Título Japonês", anime.TitleJapanese, larguraValor);
        if (anime.TitleSynonyms?.Count > 0)
            AdicionarDetalhe("Sinônimos", string.Join(", ", anime.TitleSynonyms), larguraValor);

        AdicionarDetalhe("Tipo", anime.Type, larguraValor);
        AdicionarDetalhe("Fonte", anime.Source, larguraValor);
        AdicionarDetalhe("Episódios", anime.Episodes?.ToString(), larguraValor);
        AdicionarDetalhe("Status", anime.Status, larguraValor);
        AdicionarDetalhe("Exibição", anime.Aired, larguraValor);
        AdicionarDetalhe("Duração", anime.Duration, larguraValor);
        AdicionarDetalhe("Classificação", anime.Rating, larguraValor);

        if (!string.IsNullOrWhiteSpace(anime.Season) && anime.Year.HasValue)
            AdicionarDetalhe("Temporada", $"{anime.Season} {anime.Year}", larguraValor);

        if (anime.Score.HasValue)
            AdicionarDetalhe("Pontuação",
                $"{anime.Score:0.00} (por {anime.ScoredBy?.ToString("N0") ?? "?"} usuários)", larguraValor);

        AdicionarDetalhe("Rank", anime.Rank?.ToString(), larguraValor);
        AdicionarDetalhe("Popularidade", anime.Popularity?.ToString(), larguraValor);
        AdicionarDetalhe("Membros", anime.Members?.ToString("N0"), larguraValor);
        AdicionarDetalhe("Favoritos", anime.Favorites?.ToString("N0"), larguraValor);

        if (anime.Studios?.Count > 0)
            AdicionarDetalhe("Estúdios", string.Join(", ", anime.Studios), larguraValor);
        if (anime.Producers?.Count > 0)
            AdicionarDetalhe("Produtoras", string.Join(", ", anime.Producers), larguraValor);
        if (anime.Licensors?.Count > 0)
            AdicionarDetalhe("Licenciadores", string.Join(", ", anime.Licensors), larguraValor);
        if (anime.Genres?.Count > 0)
            AdicionarDetalhe("Gêneros", string.Join(", ", anime.Genres), larguraValor);
        if (anime.Themes?.Count > 0)
            AdicionarDetalhe("Temas", string.Join(", ", anime.Themes), larguraValor);
        if (anime.Demographics?.Count > 0)
            AdicionarDetalhe("Público-alvo", string.Join(", ", anime.Demographics), larguraValor);
        if (anime.ExplicitGenres?.Count > 0)
            AdicionarDetalhe("Gêneros +18", string.Join(", ", anime.ExplicitGenres), larguraValor);

        if (!string.IsNullOrWhiteSpace(anime.Trailer))
            AdicionarLink("Trailer", anime.Trailer, larguraValor);
        if (!string.IsNullOrWhiteSpace(anime.Url))
            AdicionarLink("MAL URL", anime.Url, larguraValor);

        AdicionarSeparador(larguraValor);
        AdicionarTextoLongo("Sinopse", anime.Synopsis, larguraValor);
        AdicionarTextoLongo("Contexto / Fundo", anime.Background, larguraValor);

        Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
        Pnl_Info.ResumeLayout(true);
    }

    // ===================================================================

    private void AdicionarDetalhe(string campo, string? valor, int larguraValor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        AdicionarParDeLabels(campo, valor, Color.FromArgb(20, 20, 20), larguraValor, isLink: false);
    }

    private void AdicionarLink(string campo, string url, int larguraValor)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        AdicionarParDeLabels(campo, url, Color.RoyalBlue, larguraValor, isLink: true);
    }

    private void AdicionarParDeLabels(string campo, string valor, Color corValor,
        int larguraValor, bool isLink)
    {
        var lblCampo = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            Location = new Point(4, _yOffset + 2),
            Size = new Size(148, 20),
            Text = campo + ":",
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblValor = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 8.5F,
                isLink ? FontStyle.Underline : FontStyle.Regular),
            ForeColor = corValor,
            Location = new Point(156, _yOffset + 2),
            Size = new Size(larguraValor, 20),
            Text = valor,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = isLink ? Cursors.Hand : Cursors.Default
        };

        if (isLink)
        {
            string urlCapturada = valor;
            lblValor.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(urlCapturada)
                        { UseShellExecute = true });
                }
                catch { }
            };
        }

        Pnl_Info.Controls.Add(lblCampo);
        Pnl_Info.Controls.Add(lblValor);
        _yOffset += 24;
    }

    private void AdicionarTextoLongo(string campo, string? texto, int larguraValor)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;

        _yOffset += 6;
        var lblCampo = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 30, 110),
            Location = new Point(4, _yOffset),
            Size = new Size(148 + larguraValor, 22),
            Text = campo + ":"
        };
        _yOffset += 24;

        int larguraTexto = Math.Max(148 + larguraValor - 12, 350);
        var lblTexto = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(larguraTexto, 0),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(35, 35, 35),
            Location = new Point(8, _yOffset),
            Text = texto
        };

        Pnl_Info.Controls.Add(lblCampo);
        Pnl_Info.Controls.Add(lblTexto);
        // Força o cálculo do tamanho antes de ler a altura
        lblTexto.CreateControl();
        _yOffset += lblTexto.Height + 14;
        AdicionarSeparador(larguraValor);
    }

    private void AdicionarSeparador(int larguraValor)
    {
        var sep = new Panel
        {
            BackColor = Color.LightSteelBlue,
            Location = new Point(4, _yOffset),
            Size = new Size(148 + larguraValor, 1)
        };
        Pnl_Info.Controls.Add(sep);
        _yOffset += 10;
    }

    private void MostrarCarregando(bool carregando)
    {
        if (carregando)
        {
            Lbl_Carregando.Visible = true;
            Pnl_Conteudo.Visible = false;
            Lbl_Carregando.BringToFront();
        }
        else
        {
            Pnl_Conteudo.Visible = true;
            Pnl_Conteudo.BringToFront();
            Lbl_Carregando.Visible = false;
        }
    }
}
