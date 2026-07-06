using LibDtudo.Shared.Dtos;
using Microsoft.VisualBasic;
using System.Net;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

/// <summary>
/// UserControl que exibe todos os detalhes disponíveis de um anime buscado por ID via ApiJikan.
/// Carrega os dados assincronamente ao ser exibido pela primeira vez.
/// </summary>
public partial class FUC_DetalhesAnime : UserControl
{
    /// <summary>Disparado quando o usuário clica em um mini card de anime relacionado. O argumento é o MalId.</summary>
    public event EventHandler<int>? CardClicado;

    private readonly JikanApiService _jikanService = new();
    private readonly ApiMyAnimesService _apiMyAnimesService = new();
    private readonly int _malId;
    private int _yOffset;
    private JikanAnimeDetalhes? _animeAtual;
    private List<JikanRelacaoEntry> _animesRelacionados = [];

    public FUC_DetalhesAnime(int malId)
    {
        InitializeComponent();
        _malId = malId;
        Btn_SalvarComoMyAnime.Click += Btn_SalvarComoMyAnime_Click;
        Btn_SalvarComoAnime.Click += Btn_SalvarComoAnime_Click;
        Load += async (s, e) => await CarregarAsync();
        // Melhora renderização do UserControl
        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }
    // ===================================================================
    /// <summary>
    /// Carrega os detalhes do anime via ApiJikan e popula a interface do usuário.
    /// </summary>
    private async Task CarregarAsync()
    {
        MostrarCarregando(true);
        JikanAnimeDetalhes? anime = null;
        string? erro = null;
        try
        { anime = await _jikanService.BuscarPorIdAsync(_malId); }
        catch (HttpRequestException ex)
        {
            erro = $"Não foi possível conectar à API Jikan.\n\n" +
                   $"Verifique se o ApiJikan está em execução em:\n{JikanApiService.ApiBase}\n\n" +
                   $"Detalhes: {ex.Message}";
        }
        catch (Exception ex)
        { erro = ex.Message; }
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

        _animeAtual = anime;
        PopularUI(anime);
        _ = CarregarRelacoesAsync();
    }
    // ===================================================================
    /// <summary>
    /// Popula a interface do usuário com os detalhes do anime fornecido.
    /// </summary>
    /// <param name="anime">Os detalhes do anime a serem exibidos.</param>
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
        AdicionarParDeLabels(campo, valor, Color.Gold, larguraValor, isLink: false);
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
            ForeColor = Color.Gold,
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
            ForeColor = Color.Gold,
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
            ForeColor = Color.FromArgb(255, 115, 0),
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
    // ===================================================================

    private async Task CarregarRelacoesAsync()
    {
        int largura = Math.Max(Pnl_Info.ClientSize.Width - 12, 300);

        // Fase 1: exibir cabeçalho e indicador de carregamento
        Pnl_Info.SuspendLayout();
        AdicionarSeparador(largura);

        var lblTituloSecao = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 50, 120),
            Location = new Point(4, _yOffset),
            Size = new Size(148 + largura, 26),
            Text = "🔗 Animes Relacionados"
        };
        _yOffset += 30;

        var lblCarregando = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
            ForeColor = Color.Gray,
            Location = new Point(8, _yOffset),
            Size = new Size(400, 22),
            Text = "⏳ Carregando animes relacionados..."
        };
        int yBaseRelacoes = _yOffset;
        _yOffset += 26;

        Pnl_Info.Controls.Add(lblTituloSecao);
        Pnl_Info.Controls.Add(lblCarregando);
        Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
        Pnl_Info.ResumeLayout(true);

        // Fase 2: buscar relações assincronamente
        List<JikanAnimeRelacaoGroup> relacoes = [];
        try
        {
            relacoes = await _jikanService.BuscarRelacoesAsync(_malId);
        }
        catch
        {
            lblCarregando.Text = "⚠️ Não foi possível carregar os animes relacionados.";
            return;
        }

        // Fase 3: substituir indicador pelos mini cards
        Pnl_Info.Controls.Remove(lblCarregando);
        lblCarregando.Dispose();
        _yOffset = yBaseRelacoes;

        var entradasAnime = relacoes
            .SelectMany(g => g.Entry)
            .Where(e => e.Type == "anime")
            .ToList();

        _animesRelacionados = entradasAnime;

        if (entradasAnime.Count == 0)
        {
            var lblSemRel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Location = new Point(8, _yOffset),
                Size = new Size(400, 22),
                Text = "Nenhum anime relacionado encontrado."
            };
            Pnl_Info.SuspendLayout();
            Pnl_Info.Controls.Add(lblSemRel);
            _yOffset += 26;
            Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
            Pnl_Info.ResumeLayout(true);
            return;
        }

        int larguraFlp = Math.Max(Pnl_Info.ClientSize.Width - 16, 300);
        var flp = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Location = new Point(4, _yOffset),
            MinimumSize = new Size(larguraFlp, 0),
            MaximumSize = new Size(larguraFlp, 0),
            Padding = new Padding(4),
            //Aqui foi modificado para usar a cor de fundo secundária do DarkModeColors
            BackColor = DarkModeColors.BackgroundSecondaryColor
        };

        foreach (var entry in entradasAnime)
        {
            var card = new UC_MiniAnimeCard();
            card.CarregarDados(entry);
            card.CardClicado += (s, id) => CardClicado?.Invoke(this, id);
            flp.Controls.Add(card);
        }

        Pnl_Info.SuspendLayout();
        Pnl_Info.Controls.Add(flp);
        flp.CreateControl();
        // Usa GetPreferredSize para estimar a altura antes da escala de DPI ser aplicada;
        // AutoSize=true no FLP corrige o tamanho final quando o controle é exibido.
        int alturaEstimada = flp.GetPreferredSize(new Size(larguraFlp, int.MaxValue)).Height;
        _yOffset += Math.Max(alturaEstimada, flp.Height) + 12;
        Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
        Pnl_Info.ResumeLayout(true);
    }

    private async void Btn_SalvarComoMyAnime_Click(object? sender, EventArgs e)
    {
        if (_animeAtual is null)
        {
            MessageBox.Show("Os detalhes do anime ainda não foram carregados.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var tituloMyAnime = ObterTituloMyAnime(_animeAtual);
        var malIdsRelacionados = ObterMalIdsRelacionados();

        Btn_SalvarComoMyAnime.Enabled = false;
        try
        {
            var tituloJaExiste = await ExisteMyAnimeComTituloAsync(tituloMyAnime);
            if (tituloJaExiste)
            {
                MessageBox.Show(
                    $"Já existe uma coleção MyAnime com o título '{tituloMyAnime}'.",
                    "Cadastro bloqueado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var dto = new AdicionaMyAnimeDto
            {
                Titulo = tituloMyAnime,
                AnimesMalId = malIdsRelacionados
            };

            await _apiMyAnimesService.AdicionarMyAnimeAsync(dto);

            MessageBox.Show(
                $"Coleção '{tituloMyAnime}' salva com sucesso em MyAnime.",
                "Sucesso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (HttpRequestException)
        {
            MessageBox.Show(
                $"Falha ao salvar em MyAnime.",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Btn_SalvarComoMyAnime.Enabled = true;
        }
    }

    private async void Btn_SalvarComoAnime_Click(object? sender, EventArgs e)
    {
        if (_animeAtual is null)
        {
            MessageBox.Show("Os detalhes do anime ainda não foram carregados.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var entrada = Interaction.InputBox(
            "Informe o ID de um MyAnime já existente para relacionar este anime:",
            "Salvar como Anime",
            "");

        if (string.IsNullOrWhiteSpace(entrada)) return;

        if (!int.TryParse(entrada, out var myAnimeId) || myAnimeId <= 0)
        {
            MessageBox.Show("Informe um MyAnimeId válido (número inteiro positivo).", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Btn_SalvarComoAnime.Enabled = false;
        try
        {
            var myAnimeExistente = await _apiMyAnimesService.ObterMyAnimePorIdAsync(myAnimeId);
            if (myAnimeExistente is null)
            {
                MessageBox.Show($"MyAnime com ID {myAnimeId} não encontrado.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dtoAnime = MapearParaAdicionaAnimeDto(_animeAtual, myAnimeId);
            await _apiMyAnimesService.AdicionarAnimeAsync(dtoAnime);

            var malIdsAtualizados = myAnimeExistente.AnimesMalId
                .Concat(ObterMalIdsRelacionados())
                .Distinct()
                .ToList();

            if (malIdsAtualizados.Count != myAnimeExistente.AnimesMalId.Count)
            {
                var atualizaMyAnime = new AtualizaMyAnimeDto
                {
                    Titulo = myAnimeExistente.Titulo,
                    AnimesMalId = malIdsAtualizados
                };
                await _apiMyAnimesService.AtualizarMyAnimeAsync(myAnimeId, atualizaMyAnime);
            }

            MessageBox.Show(
                $"Anime salvo com sucesso e relacionado ao MyAnime ID {myAnimeId}.",
                "Sucesso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            MessageBox.Show(
                $"Este anime já existe na base local (MalId {_animeAtual.MalId}).",
                "Conflito",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (HttpRequestException)
        {
            MessageBox.Show(
                "Falha ao salvar anime na ApiMyAnimes.",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Btn_SalvarComoAnime.Enabled = true;
        }
    }

    private async Task<bool> ExisteMyAnimeComTituloAsync(string titulo)
    {
        const int take = 100;
        var skip = 0;

        while (true)
        {
            var pagina = await _apiMyAnimesService.ObterMyAnimesAsync(skip, take);
            if (pagina.Count == 0) return false;

            var existe = pagina.Any(item =>
                string.Equals(item.Titulo?.Trim(), titulo.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existe) return true;

            if (pagina.Count < take) return false;
            skip += take;
        }
    }

    private string ObterTituloMyAnime(JikanAnimeDetalhes anime)
    {
        return !string.IsNullOrWhiteSpace(anime.Title)
            ? anime.Title
            : !string.IsNullOrWhiteSpace(anime.TitleEnglish)
                ? anime.TitleEnglish
                : $"Anime_{anime.MalId}";
    }

    private List<int> ObterMalIdsRelacionados()
    {
        var ids = _animesRelacionados
            .Select(a => a.MalId)
            .Where(id => id > 0)
            .ToList();

        if (_animeAtual is not null && _animeAtual.MalId > 0)
            ids.Add(_animeAtual.MalId);

        return ids.Distinct().ToList();
    }

    private static AdicionaAnimeDto MapearParaAdicionaAnimeDto(JikanAnimeDetalhes anime, int myAnimeId)
    {
        var episodios = anime.Episodes.HasValue && anime.Episodes.Value > 0
            ? anime.Episodes.Value
            : 1;

        var imagens = new List<string>();
        if (!string.IsNullOrWhiteSpace(anime.Images?.Jpg?.ImageUrl)) imagens.Add(anime.Images.Jpg.ImageUrl);
        if (!string.IsNullOrWhiteSpace(anime.Images?.Jpg?.SmallImageUrl)) imagens.Add(anime.Images.Jpg.SmallImageUrl);
        if (!string.IsNullOrWhiteSpace(anime.Images?.Jpg?.LargeImageUrl)) imagens.Add(anime.Images.Jpg.LargeImageUrl);

        var subtitulos = new List<string>();
        if (!string.IsNullOrWhiteSpace(anime.TitleEnglish)) subtitulos.Add(anime.TitleEnglish);
        if (!string.IsNullOrWhiteSpace(anime.TitleJapanese)) subtitulos.Add(anime.TitleJapanese);
        subtitulos.AddRange(anime.TitleSynonyms);

        return new AdicionaAnimeDto
        {
            MalId = anime.MalId,
            Titulo = !string.IsNullOrWhiteSpace(anime.Title) ? anime.Title : $"Anime_{anime.MalId}",
            Episodios = episodios,
            MyAnimeID = myAnimeId,
            MalUrl = anime.Url ?? string.Empty,
            ImagensUrlMal = imagens.Distinct().ToList(),
            SubTitulos = subtitulos.Distinct().ToList(),
            Trailer = anime.Trailer,
            Approved = anime.Approved,
            Title = anime.Title,
            TitleEnglish = anime.TitleEnglish,
            TitleJapanese = anime.TitleJapanese,
            TitleSynonyms = [.. anime.TitleSynonyms],
            Type = anime.Type,
            Source = anime.Source,
            Episodes = anime.Episodes,
            Status = anime.Status,
            Airing = anime.Airing,
            Aired = anime.Aired,
            Duration = anime.Duration,
            Rating = anime.Rating,
            Score = anime.Score,
            ScoredBy = anime.ScoredBy,
            Rank = anime.Rank,
            Popularity = anime.Popularity,
            Members = anime.Members,
            Favorites = anime.Favorites,
            Synopsis = anime.Synopsis,
            Background = anime.Background,
            Season = anime.Season,
            Year = anime.Year,
            Producers = [.. anime.Producers],
            Licensors = [.. anime.Licensors],
            Studios = [.. anime.Studios],
            Genres = [.. anime.Genres],
            ExplicitGenres = [.. anime.ExplicitGenres],
            Themes = [.. anime.Themes],
            Demographics = [.. anime.Demographics]
        };
    }

    // ===================================================================

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
