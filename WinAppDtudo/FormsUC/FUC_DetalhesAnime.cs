using LibDtudo.Shared.Dtos;
using Microsoft.VisualBasic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

/// <summary>
/// UserControl que exibe todos os detalhes disponíveis de um anime buscado por ID via ApiMyAnimeList.
/// Carrega os dados assincronamente ao ser exibido pela primeira vez.
/// </summary>
public partial class FUC_DetalhesAnime : UserControl
{
    /// <summary>Disparado quando o usuário clica em um mini card de anime relacionado. O argumento é o MalId.</summary>
    public event EventHandler<int>? CardClicado;
    public event EventHandler<int>? MyAnimeAtualizado;

    private readonly MyAnimeListApiService _myAnimeListService = new();
    private readonly JikanApiService _jikanService = new();
    private readonly ApiMyAnimesService _apiMyAnimesService = new();
    private readonly int _malId;
    private readonly bool _usarJikan;
    private int _yOffset;
    private JikanAnimeDetalhes? _animeAtual;
    private List<JikanRelacaoEntry> _animesRelacionados = [];

    public FUC_DetalhesAnime(int malId, bool usarJikan = false)
    {
        InitializeComponent();
        _malId = malId;
        _usarJikan = usarJikan;
        Btn_SalvarComoMyAnime.Click += Btn_SalvarComoMyAnime_Click;
        Btn_SalvarComoAnime.Click += Btn_SalvarComoAnime_Click;
        Load += async (s, e) => await CarregarAsync();
        // Melhora renderização do UserControl
        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }
    // ===================================================================
    /// <summary>
    /// Carrega os detalhes do anime via ApiMyAnimeList e popula a interface do usuário.
    /// </summary>
    private async Task CarregarAsync()
    {
        MostrarCarregando(true);
        JikanAnimeDetalhes? anime = null;
        string? erro = null;
        try
        { anime = _usarJikan
                ? await _jikanService.BuscarPorIdAsync(_malId)
                : await _myAnimeListService.BuscarPorIdAsync(_malId); }
        catch (HttpRequestException ex)
        {
            var apiNome = _usarJikan ? "ApiJikan" : "ApiMyAnimeList";
            var apiBase = _usarJikan ? JikanApiService.ApiBase : MyAnimeListApiService.ApiBase;
            erro = $"Não foi possível conectar à {apiNome}.\n\n" +
                   $"Verifique se a {apiNome} está em execução em:\n{apiBase}\n\n" +
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

        List<JikanAnimeRelacaoGroup> relacoes = [];
        try
        {
            relacoes = _usarJikan
                ? await _jikanService.BuscarRelacoesAsync(_malId)
                : await _myAnimeListService.BuscarRelacoesAsync(_malId);
        }
        catch
        {
        }

        PopularUI(anime, relacoes);
    }
    // ===================================================================
    /// <summary>
    /// Popula a interface do usuário com os detalhes do anime fornecido.
    /// </summary>
    /// <param name="anime">Os detalhes do anime a serem exibidos.</param>
    private void PopularUI(JikanAnimeDetalhes anime, List<JikanAnimeRelacaoGroup> relacoes)
    {
        var anoLancamento = ExtrairAnoLancamentoPeloAired(anime.Aired);

        // Header
        Lbl_TituloAnime.Text = anime.Title ?? $"Anime #{anime.MalId}";
        var exibicao = anime.Airing ? " (Em exibição)" : string.Empty;
        Lbl_TipoStatus.Text = $"{anime.Type ?? "?"}  •  {anime.Status ?? "?"}{exibicao}";

        // Imagem de capa
        _ = CarregarCapaAsync(anime);

        // Estatísticas rápidas (painel esquerdo)
        Lbl_Ano.Text = anoLancamento.HasValue ? $"📅 {anoLancamento}" : string.Empty;
        Lbl_ScoreStat.Text = anime.Score.HasValue ? $"⭐ {anime.Score:0.00}" : string.Empty;
        Lbl_Rank.Text = anime.Rank.HasValue ? $"🏆 Rank #{anime.Rank}" : string.Empty;
        Lbl_Popularidade.Text = anime.Popularity.HasValue ? $"👥 Pop. #{anime.Popularity}" : string.Empty;
        Lbl_Episodios.Text = anime.Episodes.HasValue ? $"📺 {anime.Episodes} ep." : string.Empty;
        Lbl_Duracao.Text = !string.IsNullOrWhiteSpace(anime.Duration) ? $"⏱ {anime.Duration}" : string.Empty;

        // Painel direito: detalhes dinâmicos
        Pnl_Info.SuspendLayout();
        Pnl_Info.Controls.Clear();
        _yOffset = 10;

        int larguraValor = Math.Max(Pnl_Info.ClientSize.Width - 265, 600);
        
        AdicionarRelacoes(relacoes);

        AdicionarDetalhe("Mal ID", anime.MalId.ToString(), larguraValor);
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
        if (!string.IsNullOrWhiteSpace(anime.Season) && anoLancamento.HasValue)
            AdicionarDetalhe("Temporada", $"{anime.Season} {anoLancamento}", larguraValor);
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
        //AdicionarRelacoes(relacoes);
        AdicionarTextoLongo("Sinopse", anime.Synopsis, larguraValor);
        AdicionarTextoLongo("Contexto / Fundo", anime.Background, larguraValor);

        Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
        Pnl_Info.ResumeLayout(true);
    }

    private async Task CarregarCapaAsync(JikanAnimeDetalhes anime)
    {
        var urls = new[]
        {
            anime.Images?.Jpg?.LargeImageUrl,
            anime.Images?.Jpg?.ImageUrl,
            anime.Images?.Jpg?.SmallImageUrl
        }.Where(url => !string.IsNullOrWhiteSpace(url)).Distinct().ToList();
        foreach (var url in urls)
        {
            var image = _usarJikan
                ? await ImageLoaderService.DownloadAsync(url)
                : await ImageLoaderService.DownloadAnimeCoverAsync(url, _malId);
            if (image is null || Pbx_Capa.IsDisposed)
            {
                image?.Dispose();
                continue;
            }
            void AplicarImagem()
            {
                if (Pbx_Capa.IsDisposed)
                {
                    image.Dispose();
                    return;
                }
                var anterior = Pbx_Capa.Image;
                Pbx_Capa.Image = image;
                anterior?.Dispose();
            }
            if (Pbx_Capa.InvokeRequired)
                Pbx_Capa.BeginInvoke(AplicarImagem);
            else
                AplicarImagem();
            return;
        }
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
        int alturaValor = Math.Max(
            34,
            TextRenderer.MeasureText(
                valor,
                new Font("Segoe UI", 9F, isLink ? FontStyle.Underline : FontStyle.Regular),
                new Size(larguraValor, int.MaxValue),
                TextFormatFlags.WordBreak).Height + 4);

        var lblCampo = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(4, _yOffset + 2),
            Size = new Size(260, alturaValor),
            Text = campo + ":",
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblValor = new TextBox
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 9.5F,
                isLink ? FontStyle.Underline : FontStyle.Regular),
            ForeColor = corValor,
            Location = new Point(260, _yOffset + 2),
            Size = new Size(larguraValor, alturaValor),
            Text = valor,
            TextAlign = HorizontalAlignment.Left,
            Cursor = isLink ? Cursors.Hand : Cursors.IBeam,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Pnl_Info.BackColor,
            Multiline = true,
            TabStop = true
        };
        if (isLink)
        {
            string urlCapturada = valor;
            lblValor.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(urlCapturada)
                        { UseShellExecute = true });
                }
                catch { }
            };
        }
        Pnl_Info.Controls.Add(lblCampo);
        Pnl_Info.Controls.Add(lblValor);
        _yOffset += alturaValor + 6;
    }

    private void AdicionarTextoLongo(string campo, string? texto, int larguraValor)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        _yOffset += 12;
        var lblCampo = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(4, _yOffset),
            Size = new Size(148 + larguraValor, 32),
            Text = campo + ":",
            TextAlign = ContentAlignment.MiddleLeft
        };
        _yOffset += lblCampo.Height + 8;

        int larguraTexto = Math.Max(148 + larguraValor - 12, 450);
        var lblTexto = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(255, 115, 0),
            BackColor = Pnl_Info.BackColor,
            Location = new Point(8, _yOffset),
            Size = new Size(larguraTexto, Math.Max(60, TextRenderer.MeasureText(
                texto, new Font("Segoe UI", 9.5F), new Size(larguraTexto - 8, int.MaxValue),
                TextFormatFlags.WordBreak).Height + 12)),
            Text = texto,
            //ScrollBars = ScrollBars.Vertical,
            TabStop = true
        };
        Pnl_Info.Controls.Add(lblCampo);
        Pnl_Info.Controls.Add(lblTexto);
        _yOffset += lblTexto.Height + 18;
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

    private void AdicionarRelacoes(List<JikanAnimeRelacaoGroup> relacoes)
    {
        int larguraSecao = Math.Max(Pnl_Info.ClientSize.Width - 12, 370);

        var lblTituloSecao = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(4, _yOffset),
            Size = new Size(larguraSecao, 60),
            Text = "🔗 Animes Relacionados:",
            TextAlign = ContentAlignment.TopCenter
        };
        Pnl_Info.Controls.Add(lblTituloSecao);
        _yOffset += lblTituloSecao.Height + 8;

        var entradasAnime = relacoes
            .SelectMany(g => g.Entry ?? [])
            .Where(e => e.MalId > 0)
            .ToList();

        _animesRelacionados = entradasAnime;

        if (entradasAnime.Count == 0)
        {
            var lblSemRel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 12F, FontStyle.Italic),
                ForeColor = Color.Gold,
                Location = new Point(800, _yOffset),
                Size = new Size(900, 60),
                Text = "Nenhum anime relacionado ao atual foi encontrado."
            };
            Pnl_Info.SuspendLayout();
            Pnl_Info.Controls.Add(lblSemRel);
            _yOffset += 26;
            Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
            Pnl_Info.ResumeLayout(true);
            return;
        }

        int larguraContainer = Math.Max(Pnl_Info.ClientSize.Width - 16, 370);
        int larguraCards = Math.Max(larguraContainer - 8, 220);

        var pnlRelacoes = new Panel
        {
            Location = new Point(4, _yOffset),
            Size = new Size(larguraContainer, 370),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            //AutoScroll = true,
            BorderStyle = BorderStyle.FixedSingle
        };

        var flp = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Location = new Point(4, 4),
            MinimumSize = new Size(larguraCards, 0),
            MaximumSize = new Size(larguraCards, 0),
            Padding = new Padding(4),
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
        pnlRelacoes.Controls.Add(flp);
        Pnl_Info.Controls.Add(pnlRelacoes);
        flp.CreateControl();
        int alturaEstimada = flp.GetPreferredSize(new Size(larguraCards, int.MaxValue)).Height;
        int alturaContainer = Math.Max(360, Math.Min(1200, alturaEstimada + 12));
        pnlRelacoes.Height = alturaContainer;
        _yOffset += alturaContainer + 12;
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

            var myAnimeId = await AdicionarMyAnimeComRetornoDeIdAsync(dto);

            var importacao = await new ImportadorAnimesMyAnimeService().ImportarAsync(
                myAnimeId,
                tituloMyAnime,
                malIdsRelacionados);

            var mensagemSucesso =
                $"Coleção '{tituloMyAnime}' salva com sucesso em MyAnime.\n\n" +
                $"Animes salvos: {importacao.AnimesSalvos}\n" +
                $"Animes já existentes: {importacao.AnimesIgnorados}\n" +
                $"Animes salvos em modo degradação: {importacao.AnimesSalvosModoDegradacao}";
            var iconeSucesso = importacao.AnimesComFalha == 0
                ? MessageBoxIcon.Information
                : MessageBoxIcon.Warning;

            MessageBox.Show(
                mensagemSucesso,
                "Sucesso",
                MessageBoxButtons.OK,
                iconeSucesso);

            MyAnimeAtualizado?.Invoke(this, myAnimeId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            MessageBox.Show(
                $"Já existe uma coleção MyAnime com o título '{tituloMyAnime}'.",
                "Cadastro bloqueado",
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

            var dtoAnime = ConversorAnimeDtoService.CriarAdicionaAnimeDto(_animeAtual, myAnimeId);
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

            MyAnimeAtualizado?.Invoke(this, myAnimeId);
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

    private async Task<int> AdicionarMyAnimeComRetornoDeIdAsync(AdicionaMyAnimeDto dto)
    {
        var idRetornado = await _apiMyAnimesService.AdicionarMyAnimeAsync(dto);
        if (idRetornado.HasValue && idRetornado.Value > 0)
            return idRetornado.Value;

        var myAnimeCriado = await _apiMyAnimesService.ObterMyAnimePorTituloAsync(dto.Titulo);
        if (myAnimeCriado is not null && myAnimeCriado.Id > 0)
            return myAnimeCriado.Id;

        throw new InvalidOperationException("MyAnime criado, mas o ID não pôde ser recuperado para cadastrar o anime automaticamente.");
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

    private static int? ExtrairAnoLancamentoPeloAired(string? aired)
    {
        if (string.IsNullOrWhiteSpace(aired)) return null;

        var dataInicialTexto = aired.Split(" to ", StringSplitOptions.TrimEntries)[0];

        if (DateTime.TryParseExact(
            dataInicialTexto,
            ["MMM dd, yyyy", "MMM d, yyyy"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dataInicial))
        {
            return dataInicial.Year;
        }

        var matchAno = Regex.Match(dataInicialTexto, @"\b(19|20)\d{2}\b");
        if (matchAno.Success && int.TryParse(matchAno.Value, out var ano))
            return ano;

        return null;
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
