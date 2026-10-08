using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.MyAnimeList;
using Microsoft.VisualBasic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

/// <summary>
/// UserControl que exibe todos os detalhes disponíveis de um anime.
/// O modo externo consulta a ApiMyAnimeList; o modo local consulta apenas o DB_Local.
/// </summary>
public partial class FUC_DetalhesAnime : UserControl, IThemeAccentProvider
{
    /// <summary>Disparado quando o usuário clica em um mini card de anime relacionado. O argumento é o MalId.</summary>
    public event EventHandler<int>? CardClicado;
    public event EventHandler<int>? MyAnimeAtualizado;
    public event EventHandler<int>? MyAnimeExistenteSelecionado;
    public event EventHandler<int>? MyAnimeSolicitado;
    public event EventHandler<int>? EditarAnimeSolicitado;
    /// <summary>Disparado com o texto de progresso do salvamento, para ser exibido na barra de status do formulário hospedeiro.</summary>
    public event EventHandler<string>? StatusAtualizado;

    private readonly MyAnimeListApiService _myAnimeListService;
    private readonly ApiMyAnimesService _apiMyAnimesService;
    private readonly WinAppAuthenticationService _authenticationService;
    private readonly int _malId;
    private readonly bool _consultaLocal;
    private AnimeDetails? _animeAtual;
    private List<AnimeRelationEntry> _animesRelacionados = [];

    public FUC_DetalhesAnime(int malId)
        : this(malId, consultaLocal: false, apiMyAnimesService: null)
    {
    }

    public FUC_DetalhesAnime(
        int malId,
        bool consultaLocal,
        ApiMyAnimesService? apiMyAnimesService = null,
        WinAppAuthenticationService? authenticationService = null)
    {
        InitializeComponent();
        _malId = malId;
        _consultaLocal = consultaLocal;
        _authenticationService = authenticationService ?? new WinAppAuthenticationService();
        _myAnimeListService = new MyAnimeListApiService(_authenticationService);
        _apiMyAnimesService = apiMyAnimesService ?? new ApiMyAnimesService(_authenticationService);
        ConfigurarLayoutDaPagina();
        Btn_SalvarComoMyAnime.Click += Btn_SalvarComoMyAnime_Click;
        Btn_SalvarComoAnime.Click += Btn_SalvarComoAnime_Click;
        Btn_ExibirMyAnime.Click += (_, _) =>
        {
            if (_animeAtual?.MyAnimeID > 0)
                MyAnimeSolicitado?.Invoke(this, _animeAtual.MyAnimeID);
        };
        Btn_EditarAnime.Click += (_, _) =>
        {
            var malId = _animeAtual?.MalId ?? _malId;
            if (malId > 0)
                EditarAnimeSolicitado?.Invoke(this, malId);
        };
        AplicarFundoDaAba();
        if (_consultaLocal)
        {
            Btn_SalvarComoMyAnime.Visible = false;
            Btn_SalvarComoAnime.Visible = false;
            Pnl_Acoes.Visible = false;
            Btn_ExibirMyAnime.Visible = true;
            Btn_EditarAnime.Visible = true;
        }
        Load += async (_, _) =>
        {
            if (_carregamentoIniciado)
                return;
            _carregamentoIniciado = true;
            PerformLayout();
            await CarregarAsync();
        };
        // Melhora renderização do UserControl
        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
        AplicarFundoDaAba();
    }
    // ===================================================================
    /// <summary>
    /// Carrega os detalhes da fonte selecionada e popula a interface do usuário.
    /// </summary>
    private async Task CarregarAsync()
    {
        MostrarCarregando(true);
        AnimeDetails? anime = null;
        ObterAnimeDto? animeLocal = null;
        string? erro = null;
        try
        {
            if (_consultaLocal)
            {
                animeLocal = await _apiMyAnimesService.ObterAnimePorMalIdAsync(_malId);
                anime = animeLocal is null ? null : AnimeDetailsMapper.FromLocal(animeLocal);
            }
            else
            {
                anime = await _myAnimeListService.BuscarPorIdAsync(_malId);
            }
        }
        catch (HttpRequestException ex)
        {
            var apiNome = _consultaLocal ? "DB_Local" : "ApiMyAnimeList";
            var apiBase = _consultaLocal ? ApiMyAnimesService.ApiBase : MyAnimeListApiService.ApiBase;
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
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao carregar detalhes:\n\n{erro}", "Erro de Conexão",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (anime == null)
        {
            WinAppDtudo.Services.DarkMessageBox.Show($"Anime com ID {_malId} não encontrado.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _animeAtual = anime;

        List<AnimeRelationGroup> relacoes = [];
        List<ObterAnimeDto> relacoesLocais = [];
        if (!_consultaLocal)
        {
            try
            {
                relacoes = await _myAnimeListService.BuscarRelacoesAsync(_malId);
                _animesRelacionados = relacoes
                    .SelectMany(grupo => grupo.Entry ?? [])
                    .Where(entrada => entrada.MalId > 0)
                    .ToList();
            }
            catch (Exception ex) when (
                ex is HttpRequestException
                or WinAppAuthenticationException
                or System.Text.Json.JsonException)
            {
                _animesRelacionados = [];
                WinAppDtudo.Services.DarkMessageBox.Show(
                    $"Não foi possível carregar os animes relacionados.\n\nDetalhes: {ex.Message}",
                    "Relações indisponíveis",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        else if (animeLocal?.AnimesRelacionadosIds.Count > 0)
        {
            try
            {
                relacoesLocais = await _apiMyAnimesService.ObterAnimesRelacionadosAsync(
                    animeLocal.AnimesRelacionadosIds.Where(id => id != anime.MalId));
            }
            catch
            {
            }
        }

        PopularUI(anime, relacoes, !_consultaLocal, relacoesLocais);
    }
    // ===================================================================
    /// <summary>
    /// Popula a interface do usuário com os detalhes do anime fornecido.
    /// </summary>
    /// <param name="anime">Os detalhes do anime a serem exibidos.</param>
    private void PopularUI(
        AnimeDetails anime,
        List<AnimeRelationGroup> relacoes,
        bool exibirRelacoes,
        IReadOnlyList<ObterAnimeDto> relacoesLocais)
    {
        _animeAtual = anime;
        var anoLancamento = ExtrairAnoLancamentoPeloAired(anime.Aired);
        // Header
        Lbl_TituloAnime.Text = anime.Title ?? $"Anime #{anime.MalId}";
        ConfigurarTitulosSecundarios(anime);

        // Imagem de capa
        _ = CarregarCapaAsync(anime);

        // Estatísticas rápidas (painel esquerdo)
        var estatisticas = new List<string>();
        if (anoLancamento.HasValue)
            estatisticas.Add($"📅{anoLancamento}");
        if (!string.IsNullOrWhiteSpace(anime.Type))
            estatisticas.Add($"🎬{anime.Type}");
        if (anime.Score.HasValue)
            estatisticas.Add($"⭐{anime.Score:0.00}");
        Lbl_EstatisticasRapidas.Text = string.Join("  ", estatisticas);
        var generos = anime.Genres?
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .ToList() ?? [];
        Lbl_Generos.Text = generos.Count > 0
            ? $"🎭 {string.Join(" • ", generos)}"
            : string.Empty;
        Lbl_Generos.Visible = generos.Count > 0;
        Lbl_Episodios.Text = anime.Episodes is > 0 ? $"📺 {anime.Episodes} ep." : string.Empty;
        Lbl_TempoPorEpisodio.Text = !string.IsNullOrWhiteSpace(anime.Duration) ? $"⏱ {anime.Duration}" : string.Empty;
        Btn_ExibirMyAnime.Visible = _consultaLocal && anime.MyAnimeID > 0;
        Btn_EditarAnime.Visible = _consultaLocal;

        Pnl_Info.SuspendLayout();
        try
        {
            Pnl_Info.ClearContent();
            if (_consultaLocal)
                AdicionarRelacoesLocais(relacoesLocais);
            else if (exibirRelacoes)
                AdicionarRelacoes(relacoes);

            AdicionarDetalhe("Mal ID", anime.MalId.ToString());
            if (_consultaLocal && anime.MyAnimeID > 0)
                AdicionarDetalhe("MyAnime ID", anime.MyAnimeID.ToString());
            AdicionarDetalhe("Fonte", anime.Source);
            AdicionarDetalhe("Classificação", anime.Rating);
            AdicionarDetalhe("Exibição", anime.Aired);
            if (!string.IsNullOrWhiteSpace(anime.Season) && anoLancamento.HasValue)
                AdicionarDetalhe("Temporada", anime.Season);
            if (anime.Score.HasValue)
                AdicionarDetalhe("Votos da pontuação", anime.ScoredBy?.ToString("N0"));
            AdicionarDetalhe("Rank", anime.Rank?.ToString());
            AdicionarDetalhe("Popularidade", anime.Popularity?.ToString());
            AdicionarDetalhe("Membros", anime.Members?.ToString("N0"));
            AdicionarDetalhe("Favoritos", anime.Favorites?.ToString("N0"));
            if (anime.Studios?.Count > 0)
                AdicionarDetalhe("Estúdios", string.Join(", ", anime.Studios));
            if (anime.Producers?.Count > 0)
                AdicionarDetalhe("Produtoras", string.Join(", ", anime.Producers));
            if (anime.Licensors?.Count > 0)
                AdicionarDetalhe("Licenciadores", string.Join(", ", anime.Licensors));
            if (anime.Themes?.Count > 0)
                AdicionarDetalhe("Temas", string.Join(", ", anime.Themes));
            if (anime.Demographics?.Count > 0)
                AdicionarDetalhe("Público-alvo", string.Join(", ", anime.Demographics));
            if (anime.ExplicitGenres?.Count > 0)
                AdicionarDetalhe("Gêneros +18", string.Join(", ", anime.ExplicitGenres));
            if (!string.IsNullOrWhiteSpace(anime.Trailer))
                AdicionarLink("Trailer", anime.Trailer);
            if (!string.IsNullOrWhiteSpace(anime.Url))
                AdicionarLink("MAL URL", anime.Url);

            AdicionarSeparador();
            AdicionarTextoLongo("Sinopse", anime.Synopsis, exibirTitulo: false);
            AdicionarTextoLongo("Contexto / Fundo", anime.Background);
        }
        finally
        {
            Pnl_Info.ResumeLayout(true);
        }
        PerformLayout();
    }

    private void ConfigurarTitulosSecundarios(AnimeDetails anime)
    {
        Lbl_TituloAnime.Text = anime.Title ?? $"Anime #{anime.MalId}";

        Lbl_TituloIngles.Text = anime.TitleEnglish ?? string.Empty;
        Lbl_Sinonimo.Text = string.Join("  •  ", (anime.TitleSynonyms ?? [])
            .Where(titulo => !string.IsNullOrWhiteSpace(titulo))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        Lbl_TituloJapones.Text = anime.TitleJapanese ?? string.Empty;

        Lbl_TituloIngles.Visible = !string.IsNullOrWhiteSpace(Lbl_TituloIngles.Text);
        Lbl_Sinonimo.Visible = !string.IsNullOrWhiteSpace(Lbl_Sinonimo.Text);
        Lbl_TituloJapones.Visible = !string.IsNullOrWhiteSpace(Lbl_TituloJapones.Text);
        AplicarFundoDaAba();
    }

    private void OrganizarTitulosDoCabecalho()
    {
        if (Pnl_Header.ClientSize.Width <= Pnl_Header.Padding.Horizontal)
            return;

        int larguraUtil = Pnl_Header.ClientSize.Width -
            Pnl_Header.Padding.Left - Pnl_Header.Padding.Right;
        int espacamentoHorizontal = LogicalToDeviceUnits(20);
        int espacamentoVertical = LogicalToDeviceUnits(6);
        int larguraColuna = larguraUtil >= LogicalToDeviceUnits(900)
            ? Math.Max(1, (larguraUtil - espacamentoHorizontal) / 2)
            : larguraUtil;
        int xEsquerda = Pnl_Header.Padding.Left;
        int xDireita = xEsquerda + larguraColuna + espacamentoHorizontal;
        int yAtual = Pnl_Header.Padding.Top;

        int alturaTitulo = CalcularAlturaTitulo(Lbl_TituloAnime, larguraColuna);
        int alturaIngles = !string.IsNullOrWhiteSpace(Lbl_TituloIngles.Text)
            ? CalcularAlturaTitulo(Lbl_TituloIngles, larguraColuna)
            : 0;
        int alturaSinonimo = !string.IsNullOrWhiteSpace(Lbl_Sinonimo.Text)
            ? CalcularAlturaTitulo(Lbl_Sinonimo, larguraColuna)
            : 0;
        int alturaJapones = !string.IsNullOrWhiteSpace(Lbl_TituloJapones.Text)
            ? CalcularAlturaTitulo(Lbl_TituloJapones, larguraColuna)
            : 0;

        if (larguraUtil >= LogicalToDeviceUnits(900))
        {
            int alturaPrimeiraLinha = Math.Max(alturaTitulo, alturaIngles);
            PosicionarTitulo(Lbl_TituloAnime, xEsquerda, yAtual, larguraColuna, alturaPrimeiraLinha);
            PosicionarTitulo(Lbl_TituloIngles, xDireita, yAtual, larguraColuna, alturaPrimeiraLinha);

            yAtual += alturaPrimeiraLinha + espacamentoVertical;
            int alturaSegundaLinha = Math.Max(alturaSinonimo, alturaJapones);
            PosicionarTitulo(Lbl_Sinonimo, xEsquerda, yAtual, larguraColuna, alturaSegundaLinha);
            PosicionarTitulo(Lbl_TituloJapones, xDireita, yAtual, larguraColuna, alturaSegundaLinha);
            yAtual += alturaSegundaLinha;
        }
        else
        {
            yAtual = PosicionarTituloEmLinhaUnica(Lbl_TituloAnime, xEsquerda, yAtual, larguraColuna, alturaTitulo, espacamentoVertical);
            yAtual = PosicionarTituloEmLinhaUnica(Lbl_TituloIngles, xEsquerda, yAtual, larguraColuna, alturaIngles, espacamentoVertical);
            yAtual = PosicionarTituloEmLinhaUnica(Lbl_Sinonimo, xEsquerda, yAtual, larguraColuna, alturaSinonimo, espacamentoVertical);
            yAtual = PosicionarTituloEmLinhaUnica(Lbl_TituloJapones, xEsquerda, yAtual, larguraColuna, alturaJapones, espacamentoVertical);
            yAtual -= espacamentoVertical;
        }

        Pnl_Header.Height = Math.Max(
            Pnl_Header.Padding.Top + Pnl_Header.Padding.Bottom,
            yAtual + Pnl_Header.Padding.Bottom);
    }

    private static int PosicionarTituloEmLinhaUnica(
        SelectableTextLabel label,
        int x,
        int y,
        int largura,
        int altura,
        int espacamentoVertical)
    {
        PosicionarTitulo(label, x, y, largura, altura);
        return !string.IsNullOrWhiteSpace(label.Text) ? y + altura + espacamentoVertical : y;
    }

    private static void PosicionarTitulo(SelectableTextLabel label, int x, int y, int largura, int altura)
    {
        label.Location = new Point(x, y);
        label.Size = new Size(largura, !string.IsNullOrWhiteSpace(label.Text) ? Math.Max(1, altura) : 1);
    }

    private int CalcularAlturaTitulo(SelectableTextLabel label, int largura)
    {
        if (string.IsNullOrWhiteSpace(label.Text))
            return 0;

        return Math.Max(LogicalToDeviceUnits(34), TextRenderer.MeasureText(
            label.Text,
            label.Font,
            new Size(Math.Max(1, largura), int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + LogicalToDeviceUnits(6));
    }

    private void AplicarFundoDaAba()
    {
        var fundo = DarkModeColors.ActiveTabBackgroundColor;
        BackColor = fundo;
        Pnl_Header.BackColor = fundo;
        Pnl_Conteudo.BackColor = fundo;
        Pnl_Esquerda.BackColor = fundo;
        Pnl_Stats.BackColor = fundo;
        Pnl_Acoes.BackColor = fundo;
        Pnl_Info.BackColor = fundo;

        Lbl_TituloAnime.BackColor = fundo;
        Lbl_TituloIngles.BackColor = fundo;
        Lbl_Sinonimo.BackColor = fundo;
        Lbl_TituloJapones.BackColor = fundo;
    }

    private async Task CarregarCapaAsync(AnimeDetails anime)
    {
        var urls = new[]
        {
            anime.Images?.Jpg?.LargeImageUrl,
            anime.Images?.Jpg?.ImageUrl,
            anime.Images?.Jpg?.SmallImageUrl
        }.Where(url => !string.IsNullOrWhiteSpace(url)).Distinct().ToList();
        foreach (var url in urls)
        {
            var image = _consultaLocal
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

    private void AdicionarDetalhe(string campo, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        AdicionarParDeLabels(campo, valor, isLink: false);
    }

    private void AdicionarLink(string campo, string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        AdicionarParDeLabels(campo, url, isLink: true);
    }

    private void AdicionarParDeLabels(string campo, string valor, bool isLink)
    {
        var lblCampo = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = AccentColor,
            Text = campo + ":",
            TextAlign = ContentAlignment.MiddleRight,
            UseMnemonic = false
        };

        var lblValor = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 10F,
                isLink ? FontStyle.Underline : FontStyle.Regular),
            ForeColor = isLink ? DarkModeColors.TextColor : AccentColor,
            Text = valor,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = isLink ? Cursors.Hand : Cursors.Default,
            BackColor = Pnl_Info.BackColor,
            UseMnemonic = false
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
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    DarkMessageBox.Show($"Não foi possível abrir o link.\n\nDetalhes: {ex.Message}",
                        "Erro ao abrir link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
        }
        Pnl_Info.AddDetail(lblCampo, lblValor);
    }

    private void AdicionarTextoLongo(string campo, string? texto, bool exibirTitulo = true)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        if (exibirTitulo)
        {
            var lblCampo = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = AccentColor,
                Text = campo + ":",
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            Pnl_Info.AddSection(lblCampo, spaceBefore: 12, spaceAfter: 12);
        }

        var lblTexto = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = AccentColor,
            BackColor = Pnl_Info.BackColor,
            MinimumSize = new Size(0, LogicalToDeviceUnits(60)),
            Text = texto,
            TextAlign = ContentAlignment.TopCenter,
            UseMnemonic = false
        };
        Pnl_Info.AddSection(lblTexto, spaceBefore: exibirTitulo ? 0 : 12, spaceAfter: 18);
        AdicionarSeparador();
    }

    private void AdicionarSeparador()
    {
        var sep = new Panel
        {
            Height = LogicalToDeviceUnits(2)
        };
        sep.Paint += (_, e) =>
        {
            using var pen = new Pen(AccentColor);
            e.Graphics.DrawLine(pen, 0, 0, sep.ClientSize.Width, 0);
        };
        Pnl_Info.AddSection(sep, spaceAfter: 10);
    }
    // ===================================================================

    private void AdicionarRelacoes(List<AnimeRelationGroup> relacoes)
    {
        var entradasAnime = relacoes
            .SelectMany(g => g.Entry ?? [])
            .Where(e => e.MalId > 0)
            .ToList();

        _animesRelacionados = entradasAnime;

        var cards = new List<UC_MiniAnimeCard>();
        foreach (var entry in entradasAnime)
        {
            var card = new UC_MiniAnimeCard();
            card.CarregarDados(entry);
            card.CardClicado += (s, id) => CardClicado?.Invoke(this, id);
            cards.Add(card);
        }
        AdicionarCardsRelacionados(cards, "Nenhum anime relacionado ao atual foi encontrado.");
    }

    private void AdicionarRelacoesLocais(IReadOnlyList<ObterAnimeDto> animes)
    {
        var animesValidos = animes.Where(anime => anime.MalId > 0).ToList();
        var cards = new List<UC_MiniAnimeCard>();
        foreach (var anime in animesValidos)
        {
            var card = new UC_MiniAnimeCard();
            card.CarregarDadosLocal(anime);
            card.CardClicado += (_, malId) => CardClicado?.Invoke(this, malId);
            cards.Add(card);
        }
        AdicionarCardsRelacionados(cards, "Nenhum anime relacionado foi encontrado no DB_Local.");
    }

    private void AdicionarCardsRelacionados(IReadOnlyList<UC_MiniAnimeCard> cards, string mensagemSemRelacoes)
    {
        var titulo = new Label
        {
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = AccentColor,
            MinimumSize = new Size(0, LogicalToDeviceUnits(52)),
            Text = "🔗 Animes relacionados:",
            TextAlign = ContentAlignment.MiddleCenter,
            UseMnemonic = false
        };
        Pnl_Info.AddSection(titulo, spaceAfter: 8);

        if (cards.Count == 0)
        {
            Pnl_Info.AddSection(new Label
            {
                Font = new Font("Segoe UI", 13F, FontStyle.Italic),
                ForeColor = AccentColor,
                MinimumSize = new Size(0, LogicalToDeviceUnits(52)),
                Text = mensagemSemRelacoes,
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false
            }, spaceAfter: 24);
            return;
        }

        var flp = new FlowLayoutPanel
        {
            Name = "Flp_AnimesRelacionados",
            AutoScroll = false,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(LogicalToDeviceUnits(4)),
            Margin = Padding.Empty,
            BackColor = Pnl_Info.BackColor
        };
        flp.Controls.AddRange(cards.Cast<Control>().ToArray());
        Pnl_Info.AddSection(flp);
    }

    private async void Btn_SalvarComoMyAnime_Click(object? sender, EventArgs e)
    {
        if (_animeAtual is null)
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Os detalhes do anime ainda não foram carregados.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var tituloMyAnime = ObterTituloMyAnime(_animeAtual);
        var malIdsRelacionados = ObterMalIdsRelacionados();

        Btn_SalvarComoMyAnime.Enabled = false;
        StatusAtualizado?.Invoke(this, $"Salvando MyAnime '{tituloMyAnime}': iniciando...");
        try
        {
            var dto = new AdicionaMyAnimeDto
            {
                Titulo = tituloMyAnime,
                AnimesMalId = malIdsRelacionados
            };

            var colecao = await _apiMyAnimesService.GarantirMyAnimeColecaoAsync(dto);
            var myAnimeId = colecao.Id;
            var malIdsParaImportar = malIdsRelacionados
                .Where(malId => colecao.AnimesMalId.Contains(malId)
                    && !colecao.AnimesIgnoradosPorOutraColecao.Contains(malId))
                .Distinct()
                .ToList();

            var importador = new ImportadorAnimesMyAnimeService(
                _apiMyAnimesService,
                myAnimeListApiService: _myAnimeListService);
            var animesRelacionadosPorMalId = new Dictionary<int, IReadOnlyCollection<int>>
            {
                [_animeAtual.MalId] = ObterMalIdsAnimesRelacionados()
            };
            var progresso = new Progress<ProgressoImportacaoAnimes>(p =>
                StatusAtualizado?.Invoke(this, $"[{p.Percentual}%] {p.Mensagem}"));
            var importacao = await importador.ImportarAsync(
                myAnimeId,
                tituloMyAnime,
                malIdsParaImportar,
                progresso,
                animesRelacionadosPorMalId: animesRelacionadosPorMalId);

            var caminhoLog = ImportadorAnimesMyAnimeService.SalvarLogErros(
                $"salvar-myanime-{myAnimeId}",
                importacao.ErrosDetalhados);
            var mensagemSucesso =
                $"Coleção '{tituloMyAnime}' salva com sucesso em MyAnime.\n\n" +
                $"Animes salvos: {importacao.AnimesSalvos}\n" +
                $"Animes já existentes: {importacao.AnimesIgnorados}\n" +
                $"Animes salvos em modo degradação: {importacao.AnimesSalvosModoDegradacao}\n" +
                $"Animes com falha: {importacao.AnimesComFalha}";

            if (!string.IsNullOrWhiteSpace(caminhoLog))
                mensagemSucesso += $"\n\nDetalhes registrados em:\n{caminhoLog}";

            var importacaoCompleta = importacao.AnimesComFalha == 0;

            if (colecao.AnimesIgnoradosPorOutraColecao.Count > 0)
            {
                mensagemSucesso +=
                    "\n\nMalIds preservados nas coleções originais (não foram duplicados): " +
                    string.Join(", ", colecao.AnimesIgnoradosPorOutraColecao.OrderBy(id => id));
            }

            WinAppDtudo.Services.DarkMessageBox.Show(
                mensagemSucesso,
                importacaoCompleta ? "Sucesso" : "Concluído com avisos",
                MessageBoxButtons.OK,
                importacaoCompleta ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            MyAnimeAtualizado?.Invoke(this, myAnimeId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            var myAnimeExistente = await _apiMyAnimesService.ObterMyAnimePorTituloAsync(tituloMyAnime);
            if (myAnimeExistente is not null)
                MostrarMyAnimeExistente(myAnimeExistente);
            else
                WinAppDtudo.Services.DarkMessageBox.Show(
                    "Nenhum anime foi adicionado à nova coleção porque todos os MalIds informados " +
                    "já pertencem às coleções originais.",
                    "Cadastro bloqueado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
        }
        catch (HttpRequestException ex)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Falha ao salvar em MyAnime.\n\nDetalhes: {ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (WinAppAuthenticationException ex)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"A sessão administrativa não está disponível.\n\nDetalhes: {ex.Message}",
                "Autenticação necessária",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException ex)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"A ApiMyAnimes retornou uma resposta inválida.\n\nDetalhes: {ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Btn_SalvarComoMyAnime.Enabled = true;
            StatusAtualizado?.Invoke(this, string.Empty);
        }
    }

    private async void Btn_SalvarComoAnime_Click(object? sender, EventArgs e)
    {
        if (_animeAtual is null)
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Os detalhes do anime ainda não foram carregados.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var entrada = WinAppDtudo.Services.DarkInputDialog.Show(
            "Informe o ID de um MyAnime já existente para relacionar este anime:",
            "Salvar como Anime",
            "");

        if (string.IsNullOrWhiteSpace(entrada)) return;

        if (!int.TryParse(entrada, out var myAnimeId) || myAnimeId <= 0)
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Informe um MyAnimeId válido (número inteiro positivo).", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Btn_SalvarComoAnime.Enabled = false;
        try
        {
            var myAnimeExistente = await _apiMyAnimesService.ObterMyAnimePorIdAsync(myAnimeId);
            if (myAnimeExistente is null)
            {
                WinAppDtudo.Services.DarkMessageBox.Show($"MyAnime com ID {myAnimeId} não encontrado.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var animesRelacionadosIds = ObterMalIdsAnimesRelacionados()
                .Where(malId => malId != _animeAtual.MalId)
                .Distinct()
                .ToList();
            var dtoAnime = ConversorAnimeDtoService.CriarAdicionaAnimeDto(
                _animeAtual,
                myAnimeId,
                animesRelacionadosIds);
            var animeExistente = await _apiMyAnimesService.ObterAnimePorMalIdAsync(_animeAtual.MalId);
            var myAnimeIdOriginal = 0;
            if (animeExistente is not null)
            {
                if (!ConfirmarSubstituicaoAnime())
                    return;

                myAnimeIdOriginal = animeExistente.MyAnimeID;
                var myAnimeIdParaPreservar = myAnimeIdOriginal > 0 && myAnimeIdOriginal != myAnimeId
                    ? myAnimeIdOriginal
                    : myAnimeId;
                var atualizaAnime = ConversorAnimeDtoService.CriarAtualizaAnimeDto(
                    _animeAtual,
                    myAnimeIdParaPreservar,
                    animesRelacionadosIds);
                await _apiMyAnimesService.AtualizarAnimeAsync(_animeAtual.MalId, atualizaAnime);
            }
            else
            {
                await _apiMyAnimesService.AdicionarAnimeAsync(dtoAnime);
            }

            var associacaoAtual = await _apiMyAnimesService.AssociarAnimeAoMyAnimeAsync(
                _animeAtual.MalId,
                myAnimeId);

            StatusAtualizado?.Invoke(this,
                $"Salvando relações diretas de '{_animeAtual.Title ?? _animeAtual.TitleEnglish ?? _animeAtual.MalId.ToString()}'...");
            var importador = new ImportadorAnimesMyAnimeService(
                _apiMyAnimesService,
                myAnimeListApiService: _myAnimeListService);
            var progresso = new Progress<ProgressoImportacaoAnimes>(p =>
                StatusAtualizado?.Invoke(this, $"[{p.Percentual}%] {p.Mensagem}"));
            var importacaoRelacionados = await importador.ImportarAsync(
                myAnimeId,
                myAnimeExistente.Titulo,
                animesRelacionadosIds,
                progresso);

            var mensagemSucesso =
                $"Anime atual salvo com sucesso e relacionado ao MyAnime ID {myAnimeId}.\n\n" +
                $"Relações diretas processadas: {animesRelacionadosIds.Count}\n" +
                $"Relações salvas: {importacaoRelacionados.AnimesSalvos}\n" +
                $"Relações já existentes ou preservadas: {importacaoRelacionados.AnimesIgnorados}\n" +
                $"Relações com falha: {importacaoRelacionados.AnimesComFalha}";

            if (associacaoAtual.IgnoradaPorOutraColecao)
            {
                mensagemSucesso =
                    $"Os dados do anime atual foram atualizados, mas sua associação original foi preservada " +
                    $"no MyAnime ID {associacaoAtual.MyAnimeIdAtual}.\n\n" +
                    mensagemSucesso;
            }

            WinAppDtudo.Services.DarkMessageBox.Show(
                mensagemSucesso,
                importacaoRelacionados.AnimesComFalha == 0 ? "Sucesso" : "Concluído com avisos",
                MessageBoxButtons.OK,
                importacaoRelacionados.AnimesComFalha == 0
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Warning);

            MyAnimeAtualizado?.Invoke(this, myAnimeId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Este anime já existe na base local (MalId {_animeAtual.MalId}).",
                "Conflito",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (HttpRequestException)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                "Falha ao salvar anime na ApiMyAnimes.",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Btn_SalvarComoAnime.Enabled = true;
            StatusAtualizado?.Invoke(this, string.Empty);
        }
    }

    private bool ConfirmarSubstituicaoAnime()
    {
        using var dialogo = new GoldBorderForm
        {
            Text = "Anime já existente",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(860, 290),
            Font = new Font("Segoe UI", 14F)
        };

        var mensagem = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 150,
            Padding = new Padding(24),
            Font = new Font("Segoe UI", 14F),
            Text = $"O anime com MalId {_animeAtual?.MalId} já existe na base local.\nDeseja substituir os dados existentes?",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var painelBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 110,
            Padding = new Padding(16),
            WrapContents = false
        };

        var cancelar = new Button
        {
            Text = "Cancelar",
            AutoSize = false,
            Size = new Size(180, 56),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            DialogResult = DialogResult.Cancel
        };
        var substituir = new Button
        {
            Text = "Substituir",
            AutoSize = false,
            Size = new Size(180, 56),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };

        painelBotoes.Controls.Add(cancelar);
        painelBotoes.Controls.Add(substituir);
        dialogo.Controls.Add(mensagem);
        dialogo.Controls.Add(painelBotoes);
        dialogo.AcceptButton = substituir;
        dialogo.CancelButton = cancelar;
        ThemeManager.ApplyDarkModeToForm(dialogo);
        dialogo.BackColor = DarkModeColors.ActiveTabBackgroundColor;
        mensagem.BackColor = DarkModeColors.ActiveTabBackgroundColor;
        painelBotoes.BackColor = DarkModeColors.ActiveTabBackgroundColor;

        return dialogo.ShowDialog(FindForm()) == DialogResult.OK;
    }

    private void MostrarMyAnimeExistente(ObterMyAnimeDto myAnime)
    {
        using var dialogo = new GoldBorderForm
        {
            Text = "MyAnime já cadastrado",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(1600, 760),
            Font = new Font("Segoe UI", 14F)
        };

        var mensagem = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 480,
            Padding = new Padding(24),
            Font = new Font("Segoe UI", 14F),
            Text = $"MyAnime {myAnime.Id}: {myAnime.Titulo}\nO MyAnime já foi cadastrado!",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var painelBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 220,
            Padding = new Padding(16),
            WrapContents = false
        };

        var ok = new Button
        {
            Text = "OK",
            AutoSize = false,
            Size = new Size(180, 56),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        var acessar = new Button
        {
            Text = "Acessar MyAnime",
            AutoSize = false,
            Size = new Size(240, 56),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };
        acessar.Click += (_, _) =>
        {
            MyAnimeExistenteSelecionado?.Invoke(this, myAnime.Id);
            dialogo.Close();
        };

        painelBotoes.Controls.Add(ok);
        painelBotoes.Controls.Add(acessar);
        dialogo.Controls.Add(mensagem);
        dialogo.Controls.Add(painelBotoes);
        dialogo.AcceptButton = ok;
        ThemeManager.ApplyDarkModeToForm(dialogo);
        dialogo.BackColor = DarkModeColors.ActiveTabBackgroundColor;
        mensagem.BackColor = DarkModeColors.ActiveTabBackgroundColor;
        painelBotoes.BackColor = DarkModeColors.ActiveTabBackgroundColor;

        dialogo.ShowDialog(FindForm());
    }

    private void MostrarConflitoTituloAnime(ConflitoTituloAnimeDto conflito)
    {
        WinAppDtudo.Services.DarkMessageBox.Show(
            $"O anime '{conflito.Titulo}' (MalId {conflito.MalId}) já está cadastrado no banco de dados local " +
            $"com o título '{conflito.TituloEmConflito}'.\n\nO anime atual não foi salvo para evitar duplicidade de títulos.",
            "Anime já cadastrado",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private string ObterTituloMyAnime(AnimeDetails anime)
    {
        var titulo = !string.IsNullOrWhiteSpace(anime.Title)
            ? anime.Title
            : !string.IsNullOrWhiteSpace(anime.TitleEnglish)
                ? anime.TitleEnglish
                : $"Anime_{anime.MalId}";

        return titulo.Trim();
    }

    private List<int> ObterMalIdsRelacionados()
    {
        var ids = ObterMalIdsAnimesRelacionados();

        if (_animeAtual is not null && _animeAtual.MalId > 0)
            ids.Add(_animeAtual.MalId);

        return ids.Distinct().ToList();
    }

    private List<int> ObterMalIdsAnimesRelacionados()
    {
        return _animesRelacionados
            .Select(a => a.MalId)
            .Where(id => id > 0)
            .ToList();
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
        _carregando = carregando;
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
        PerformLayout();
    }
}
