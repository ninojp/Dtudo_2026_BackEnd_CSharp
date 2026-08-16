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
public partial class FUC_DetalhesAnime : UserControl
{
    /// <summary>Disparado quando o usuário clica em um mini card de anime relacionado. O argumento é o MalId.</summary>
    public event EventHandler<int>? CardClicado;
    public event EventHandler<int>? MyAnimeAtualizado;
    public event EventHandler<int>? MyAnimeExistenteSelecionado;
    public event EventHandler<int>? MyAnimeSolicitado;
    public event EventHandler<int>? EditarAnimeSolicitado;

    private readonly MyAnimeListApiService _myAnimeListService;
    private readonly ApiMyAnimesService _apiMyAnimesService;
    private readonly WinAppAuthenticationService _authenticationService;
    private readonly int _malId;
    private readonly bool _consultaLocal;
    private int _yOffset;
    private int _colunaDetalhe;
    private int _alturaLinhaDetalhe;
    private Label? _ultimoCampoDetalhe;
    private Label? _ultimoValorDetalhe;
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
        ConfigurarColunaDeDetalhes();
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
        Pnl_Header.Resize += (_, _) => OrganizarTitulosDoCabecalho();
        Load += async (_, _) =>
        {
            OrganizarTitulosDoCabecalho();
            await CarregarAsync();
        };
        // Melhora renderização do UserControl
        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }
    // ===================================================================
    /// <summary>
    /// Carrega os detalhes da fonte selecionada e popula a interface do usuário.
    /// </summary>
    private async Task CarregarAsync()
    {
        MostrarCarregando(true);
        AnimeDetails? anime = null;
        string? erro = null;
        try
        {
            if (_consultaLocal)
            {
                var animeLocal = await _apiMyAnimesService.ObterAnimePorMalIdAsync(_malId);
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
        List<ObterAnimeDto> relacoesMyAnime = [];
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
        else if (anime.MyAnimeID > 0)
        {
            try
            {
                relacoesMyAnime = await _apiMyAnimesService.ObterAnimesPorMyAnimeIdAsync(anime.MyAnimeID);
            }
            catch
            {
            }
        }

        PopularUI(anime, relacoes, !_consultaLocal, relacoesMyAnime);
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
        IReadOnlyList<ObterAnimeDto> relacoesMyAnime)
    {
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
        int larguraDisponivel = Math.Max(200, Pnl_Stats.ClientSize.Width - Pnl_Stats.Padding.Horizontal - 20);
        int esquerda = Math.Max(Pnl_Stats.Padding.Left, (Pnl_Stats.ClientSize.Width - larguraDisponivel) / 2);
        Lbl_EstatisticasRapidas.Location = new Point(esquerda, Pnl_Stats.Padding.Top);
        Lbl_EstatisticasRapidas.Width = larguraDisponivel;
        Lbl_EstatisticasRapidas.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Generos.Width = larguraDisponivel;
        Lbl_Generos.Height = generos.Count > 0
            ? Math.Max(35, TextRenderer.MeasureText(
                Lbl_Generos.Text,
                Lbl_Generos.Font,
                new Size(larguraDisponivel, int.MaxValue),
                TextFormatFlags.WordBreak).Height + 4)
            : 0;
        Lbl_Generos.Location = new Point(esquerda, 0);
        Lbl_Episodios.Width = larguraDisponivel;
        Lbl_Episodios.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_TempoPorEpisodio.Width = larguraDisponivel;
        Lbl_TempoPorEpisodio.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Generos.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Episodios.Text = anime.Episodes is > 0 ? $"📺 {anime.Episodes} ep." : string.Empty;
        Lbl_TempoPorEpisodio.Text = !string.IsNullOrWhiteSpace(anime.Duration) ? $"⏱ {anime.Duration}" : string.Empty;
        const int espacamentoVertical = 5;
        int proximaLinha = Lbl_EstatisticasRapidas.Bottom + espacamentoVertical;
        Lbl_Episodios.Location = new Point(esquerda, proximaLinha);
        proximaLinha = Lbl_Episodios.Bottom + espacamentoVertical;
        Lbl_TempoPorEpisodio.Location = new Point(esquerda, proximaLinha);
        proximaLinha = Lbl_TempoPorEpisodio.Bottom + espacamentoVertical;
        Lbl_Generos.Location = new Point(esquerda, proximaLinha);
        Btn_ExibirMyAnime.Visible = _consultaLocal && anime.MyAnimeID > 0;
        const int larguraBotaoMyAnime = 300;
        int larguraBotao = Math.Min(larguraBotaoMyAnime, larguraDisponivel);
        int esquerdaBotao = (Pnl_Stats.ClientSize.Width - larguraBotao) / 2;
        Btn_ExibirMyAnime.Location = new Point(esquerdaBotao, Lbl_Generos.Visible ? Lbl_Generos.Bottom + 30 : Lbl_TempoPorEpisodio.Bottom + 30);
        Btn_ExibirMyAnime.Width = larguraBotao;
        Btn_EditarAnime.Location = new Point(esquerdaBotao, Btn_ExibirMyAnime.Bottom + 12);
        Btn_EditarAnime.Width = larguraBotao;
        Btn_EditarAnime.Visible = _consultaLocal;

        // Painel direito: detalhes dinâmicos
        Pnl_Info.SuspendLayout();
        Pnl_Info.Controls.Clear();
        _yOffset = 10;
        _colunaDetalhe = 0;
        _alturaLinhaDetalhe = 0;
        _ultimoCampoDetalhe = null;
        _ultimoValorDetalhe = null;

        int larguraColuna = Math.Max((Pnl_Info.ClientSize.Width - 20) / 2, 350);
        int larguraValor = Math.Max(larguraColuna - 160, 180);
        
        if (_consultaLocal)
            AdicionarRelacoesMyAnime(relacoesMyAnime);
        else if (exibirRelacoes)
            AdicionarRelacoes(relacoes);

        AdicionarDetalhe("Mal ID", anime.MalId.ToString(), larguraValor);
        if (_consultaLocal && anime.MyAnimeID > 0)
            AdicionarDetalhe("MyAnime ID", anime.MyAnimeID.ToString(), larguraValor);
        AdicionarDetalhe("Fonte", anime.Source, larguraValor);
        AdicionarDetalhe("Classificação", anime.Rating, larguraValor);
        AdicionarDetalhe("Exibição", anime.Aired, larguraValor);
        if (!string.IsNullOrWhiteSpace(anime.Season) && anoLancamento.HasValue)
            AdicionarDetalhe("Temporada", anime.Season, larguraValor);
        if (anime.Score.HasValue)
            AdicionarDetalhe("Votos da pontuação", anime.ScoredBy?.ToString("N0"), larguraValor);
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
        AdicionarTextoLongo("Sinopse", anime.Synopsis, larguraValor, exibirTitulo: false);
        AdicionarTextoLongo("Contexto / Fundo", anime.Background, larguraValor);

        Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
        Pnl_Info.ResumeLayout(true);
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
        OrganizarTitulosDoCabecalho();
    }

    private void OrganizarTitulosDoCabecalho()
    {
        if (Pnl_Header.ClientSize.Width <= Pnl_Header.Padding.Horizontal)
            return;

        int larguraUtil = Pnl_Header.ClientSize.Width -
            Pnl_Header.Padding.Left - Pnl_Header.Padding.Right;
        int espacamentoHorizontal = 20;
        int espacamentoVertical = 6;
        int larguraColuna = larguraUtil >= 900
            ? Math.Max(1, (larguraUtil - espacamentoHorizontal) / 2)
            : larguraUtil;
        int xEsquerda = Pnl_Header.Padding.Left;
        int xDireita = xEsquerda + larguraColuna + espacamentoHorizontal;
        int yAtual = Pnl_Header.Padding.Top;

        int alturaTitulo = CalcularAlturaTitulo(Lbl_TituloAnime, larguraColuna);
        int alturaIngles = Lbl_TituloIngles.Visible
            ? CalcularAlturaTitulo(Lbl_TituloIngles, larguraColuna)
            : 0;
        int alturaSinonimo = Lbl_Sinonimo.Visible
            ? CalcularAlturaTitulo(Lbl_Sinonimo, larguraColuna)
            : 0;
        int alturaJapones = Lbl_TituloJapones.Visible
            ? CalcularAlturaTitulo(Lbl_TituloJapones, larguraColuna)
            : 0;

        if (larguraUtil >= 900)
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
        return label.Visible ? y + altura + espacamentoVertical : y;
    }

    private static void PosicionarTitulo(SelectableTextLabel label, int x, int y, int largura, int altura)
    {
        label.Location = new Point(x, y);
        label.Size = new Size(largura, label.Visible ? Math.Max(1, altura) : 1);
    }

    private static int CalcularAlturaTitulo(SelectableTextLabel label, int largura)
    {
        if (!label.Visible)
            return 0;

        return Math.Max(34, TextRenderer.MeasureText(
            label.Text,
            label.Font,
            new Size(Math.Max(1, largura), int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.WordBreak).Height + 6);
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

    private void ConfigurarColunaDeDetalhes()
    {
        Pnl_Esquerda.SuspendLayout();
        Pnl_Esquerda.Controls.Remove(Pbx_Capa);
        Pnl_Esquerda.Controls.Remove(Pnl_Stats);
        Pnl_Esquerda.Controls.Remove(Pnl_Acoes);

        var layoutColuna = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            Padding = new Padding(12)
        };
        layoutColuna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutColuna.RowStyles.Add(new RowStyle(SizeType.Percent, 56F));
        layoutColuna.RowStyles.Add(new RowStyle(SizeType.Percent, 44F));
        layoutColuna.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Pbx_Capa.Dock = DockStyle.Fill;
        Pbx_Capa.Margin = new Padding(0, 0, 0, 12);
        Pbx_Capa.MinimumSize = new Size(0, 160);

        Pnl_Stats.Dock = DockStyle.Fill;
        Pnl_Stats.AutoScroll = true;
        Pnl_Stats.Margin = new Padding(0, 0, 0, 12);

        Pnl_Acoes.AutoSize = true;
        Pnl_Acoes.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Pnl_Acoes.Dock = DockStyle.Fill;
        Pnl_Acoes.Padding = Padding.Empty;
        ConfigurarBotaoAcao(Btn_SalvarComoMyAnime);
        ConfigurarBotaoAcao(Btn_SalvarComoAnime);
        Btn_SalvarComoMyAnime.Dock = DockStyle.Top;
        Btn_SalvarComoAnime.Dock = DockStyle.Top;

        layoutColuna.Controls.Add(Pbx_Capa, 0, 0);
        layoutColuna.Controls.Add(Pnl_Stats, 0, 1);
        layoutColuna.Controls.Add(Pnl_Acoes, 0, 2);
        Pnl_Esquerda.Controls.Add(layoutColuna);
        Pnl_Esquerda.ResumeLayout(true);
    }

    private static void ConfigurarBotaoAcao(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(
            TextRenderer.MeasureText(button.Text, button.Font).Width + 28,
            button.Font.Height + 18);
        button.Padding = new Padding(12, 8, 12, 8);
        button.Margin = new Padding(0, 0, 0, 8);
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
                new Font("Segoe UI", 10F, isLink ? FontStyle.Underline : FontStyle.Regular),
                new Size(larguraValor, int.MaxValue),
                TextFormatFlags.WordBreak).Height + 4);

        int larguraColuna = larguraValor + 160;
        int xColuna = 4 + (_colunaDetalhe * larguraColuna);
        var lblCampo = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(xColuna, _yOffset + 2),
            Size = new Size(150, alturaValor),
            Text = campo + ":",
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblValor = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 10F,
                isLink ? FontStyle.Underline : FontStyle.Regular),
            ForeColor = corValor,
            Location = new Point(xColuna + 154, _yOffset + 2),
            Size = new Size(larguraValor, alturaValor),
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
                catch { }
            };
        }
        Pnl_Info.Controls.Add(lblCampo);
        Pnl_Info.Controls.Add(lblValor);
        lblCampo.BackColor = Pnl_Info.BackColor;
        lblCampo.ForeColor = Color.Gold;
        lblValor.BackColor = Pnl_Info.BackColor;
        lblValor.ForeColor = corValor;

        if (_colunaDetalhe == 0)
        {
            _ultimoCampoDetalhe = lblCampo;
            _ultimoValorDetalhe = lblValor;
            _alturaLinhaDetalhe = alturaValor;
            _colunaDetalhe = 1;
        }
        else
        {
            _alturaLinhaDetalhe = Math.Max(_alturaLinhaDetalhe, alturaValor);
            _ultimoCampoDetalhe!.Height = _alturaLinhaDetalhe;
            _ultimoValorDetalhe!.Height = _alturaLinhaDetalhe;
            lblCampo.Height = _alturaLinhaDetalhe;
            lblValor.Height = _alturaLinhaDetalhe;
            _yOffset += _alturaLinhaDetalhe + 12;
            _colunaDetalhe = 0;
            _alturaLinhaDetalhe = 0;
            _ultimoCampoDetalhe = null;
            _ultimoValorDetalhe = null;
        }
    }

    private void FinalizarLinhaDetalhes()
    {
        if (_colunaDetalhe == 0) return;

        _yOffset += _alturaLinhaDetalhe + 12;
        _colunaDetalhe = 0;
        _alturaLinhaDetalhe = 0;
        _ultimoCampoDetalhe = null;
        _ultimoValorDetalhe = null;
    }

    private void AdicionarTextoLongo(string campo, string? texto, int larguraValor, bool exibirTitulo = true)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        FinalizarLinhaDetalhes();
        _yOffset += 12;
        int topoSecao = _yOffset;
        int topoTexto = topoSecao;
        if (exibirTitulo)
        {
            var lblCampo = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.Gold,
                Location = new Point(4, topoSecao),
                Size = new Size(2 * larguraValor + 148, 32),
                Text = campo + ":",
                TextAlign = ContentAlignment.MiddleLeft
            };
            Pnl_Info.Controls.Add(lblCampo);
            topoTexto = lblCampo.Bottom + 28;
        }

        int larguraTexto = Math.Max(2 * larguraValor + 148, 450);
        int alturaTexto = Math.Max(60, TextRenderer.MeasureText(
            texto, new Font("Segoe UI", 9.5F), new Size(larguraTexto - 24, int.MaxValue),
            TextFormatFlags.WordBreak).Height + 28);
        var lblTexto = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(255, 115, 0),
            BackColor = Pnl_Info.BackColor,
            Location = new Point(8, topoTexto),
            Size = new Size(larguraTexto, alturaTexto),
            Text = texto,
            TextAlign = ContentAlignment.TopCenter
        };
        Pnl_Info.Controls.Add(lblTexto);
        _yOffset = lblTexto.Bottom + 18;
        AdicionarSeparador(larguraValor);
    }

    private void AdicionarSeparador(int larguraValor)
    {
        FinalizarLinhaDetalhes();
        var sep = new Panel
        {
            BackColor = Color.LightSteelBlue,
            Location = new Point(20, _yOffset),
            Size = new Size(2 * larguraValor + 148, 1)
        };
        Pnl_Info.Controls.Add(sep);
        _yOffset += 10;
    }
    // ===================================================================

    private void AdicionarRelacoes(List<AnimeRelationGroup> relacoes)
    {
        int larguraSecao = Math.Max(Pnl_Info.ClientSize.Width - 12, 370);

        var lblTituloSecao = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(4, _yOffset),
            Size = new Size(larguraSecao, 100),
            Text = "🔗 Animes Relacionados:",
            TextAlign = ContentAlignment.MiddleCenter
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
                Font = new Font("Segoe UI", 13F, FontStyle.Italic),
                ForeColor = Color.Gold,
                Location = new Point(4, _yOffset),
                Size = new Size(larguraSecao, 100),
                Text = "Nenhum anime relacionado ao atual foi encontrado.",
                TextAlign = ContentAlignment.MiddleCenter
            };
            Pnl_Info.SuspendLayout();
            Pnl_Info.Controls.Add(lblSemRel);
            _yOffset += lblSemRel.Height + 24;
            Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
            Pnl_Info.ResumeLayout(true);
            return;
        }

        int larguraContainer = Math.Max(Pnl_Info.ClientSize.Width - 16, 370);
        int larguraFlp = CalcularLarguraRelacoes(larguraContainer);

        var pnlRelacoes = new Panel
        {
            Location = new Point(4, _yOffset),
            Size = new Size(larguraContainer, 390),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Pnl_Info.BackColor,
            AutoScroll = true,
            Padding = new Padding(8),
            BorderStyle = BorderStyle.None
        };

        var flp = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Width = larguraFlp,
            MinimumSize = new Size(larguraFlp, 0),
            MaximumSize = new Size(larguraFlp, 0),
            Padding = new Padding(4),
            Margin = new Padding(0),
            BackColor = Pnl_Info.BackColor
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
        AplicarFundoRelacoes(pnlRelacoes, flp);
        flp.CreateControl();
        int alturaEstimada = flp.GetPreferredSize(new Size(larguraFlp, 0)).Height;
        int alturaContainer = Math.Clamp(alturaEstimada + pnlRelacoes.Padding.Vertical + 2, 390, 1200);
        pnlRelacoes.Height = alturaContainer;
        ConfigurarScrollVerticalRelacoes(pnlRelacoes, alturaEstimada);
        _yOffset += alturaContainer + 12;
        Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
        Pnl_Info.ResumeLayout(true);
    }

    private void AdicionarRelacoesMyAnime(IReadOnlyList<ObterAnimeDto> animes)
    {
        int larguraSecao = Math.Max(Pnl_Info.ClientSize.Width - 12, 370);
        var lblTituloSecao = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(4, _yOffset),
            Size = new Size(larguraSecao, 100),
            Text = "🔗 Animes relacionados no MyAnime:",
            TextAlign = ContentAlignment.MiddleCenter
        };
        Pnl_Info.Controls.Add(lblTituloSecao);
        _yOffset += lblTituloSecao.Height + 8;

        var animesValidos = animes.Where(anime => anime.MalId > 0).ToList();
        if (animesValidos.Count == 0)
        {
            var lblSemRelacoes = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 13F, FontStyle.Italic),
                ForeColor = Color.Gold,
                Location = new Point(4, _yOffset),
                Size = new Size(larguraSecao, 100),
                Text = "Nenhum anime do MyAnime foi encontrado no DB_Local.",
                TextAlign = ContentAlignment.MiddleCenter
            };
            Pnl_Info.Controls.Add(lblSemRelacoes);
            _yOffset += lblSemRelacoes.Height + 24;
            return;
        }

        int larguraContainer = Math.Max(Pnl_Info.ClientSize.Width - 16, 370);
        int larguraFlp = CalcularLarguraRelacoes(larguraContainer);
        var pnlRelacoes = new Panel
        {
            Location = new Point(4, _yOffset),
            Size = new Size(larguraContainer, 390),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Pnl_Info.BackColor,
            AutoScroll = true,
            Padding = new Padding(8),
            BorderStyle = BorderStyle.None
        };
        var flp = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Width = larguraFlp,
            MinimumSize = new Size(larguraFlp, 0),
            MaximumSize = new Size(larguraFlp, 0),
            Padding = new Padding(4),
            Margin = new Padding(0),
            BackColor = Pnl_Info.BackColor
        };

        foreach (var anime in animesValidos)
        {
            var card = new UC_MiniAnimeCard();
            card.CarregarDadosLocal(anime);
            card.CardClicado += (_, malId) => CardClicado?.Invoke(this, malId);
            flp.Controls.Add(card);
        }

        pnlRelacoes.Controls.Add(flp);
        Pnl_Info.Controls.Add(pnlRelacoes);
        AplicarFundoRelacoes(pnlRelacoes, flp);
        flp.CreateControl();
        int alturaEstimada = flp.GetPreferredSize(new Size(larguraFlp, 0)).Height;
        pnlRelacoes.Height = Math.Clamp(alturaEstimada + pnlRelacoes.Padding.Vertical + 2, 390, 1200);
        ConfigurarScrollVerticalRelacoes(pnlRelacoes, alturaEstimada);
        _yOffset += pnlRelacoes.Height + 12;
    }

    private void AplicarFundoRelacoes(Panel pnlRelacoes, FlowLayoutPanel flp)
    {
        pnlRelacoes.BackColor = Pnl_Info.BackColor;
        flp.BackColor = Pnl_Info.BackColor;
        WindowsDarkMode.ApplyTo(pnlRelacoes);
        WindowsDarkMode.ApplyTo(flp);
    }

    private static int CalcularLarguraRelacoes(int larguraContainer)
    {
        return Math.Max(
            larguraContainer - 16 - SystemInformation.VerticalScrollBarWidth - 12,
            220);
    }

    private static void ConfigurarScrollVerticalRelacoes(Panel pnlRelacoes, int alturaConteudo)
    {
        pnlRelacoes.AutoScrollMinSize = new Size(
            0,
            Math.Max(0, alturaConteudo + pnlRelacoes.Padding.Vertical + 2));
        pnlRelacoes.HorizontalScroll.Enabled = false;
        pnlRelacoes.HorizontalScroll.Visible = false;
        pnlRelacoes.HorizontalScroll.Maximum = 0;
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
        try
        {
            var myAnimeExistente = await _apiMyAnimesService.ObterMyAnimePorTituloAsync(tituloMyAnime);
            if (myAnimeExistente is not null)
            {
                var malIdsAtualizados = myAnimeExistente.AnimesMalId
                    .Concat(malIdsRelacionados)
                    .Where(malId => malId > 0)
                    .Distinct()
                    .ToList();

                if (!malIdsAtualizados.SequenceEqual(myAnimeExistente.AnimesMalId))
                {
                    await _apiMyAnimesService.AtualizarMyAnimeAsync(
                        myAnimeExistente.Id,
                        new AtualizaMyAnimeDto
                        {
                            Titulo = myAnimeExistente.Titulo,
                            AnimesMalId = malIdsAtualizados
                        });

                    myAnimeExistente.AnimesMalId = malIdsAtualizados;
                }

                MostrarMyAnimeExistente(myAnimeExistente);
                return;
            }

            var dto = new AdicionaMyAnimeDto
            {
                Titulo = tituloMyAnime,
                AnimesMalId = malIdsRelacionados
            };

            var colecao = await _apiMyAnimesService.GarantirMyAnimeColecaoAsync(dto);
            if (!colecao.Created)
            {
                var colecaoExistente = await _apiMyAnimesService.ObterMyAnimePorIdAsync(colecao.Id);
                if (colecaoExistente is not null)
                    MostrarMyAnimeExistente(colecaoExistente);
                return;
            }

            var myAnimeId = colecao.Id;

            var importador = new ImportadorAnimesMyAnimeService(
                _apiMyAnimesService,
                myAnimeListApiService: _myAnimeListService);
            var importacao = await importador.ImportarAsync(
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

            WinAppDtudo.Services.DarkMessageBox.Show(
                mensagemSucesso,
                "Sucesso",
                MessageBoxButtons.OK,
                iconeSucesso);

            MyAnimeAtualizado?.Invoke(this, myAnimeId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            var myAnimeExistente = await _apiMyAnimesService.ObterMyAnimePorTituloAsync(tituloMyAnime);
            if (myAnimeExistente is not null)
                MostrarMyAnimeExistente(myAnimeExistente);
            else
                WinAppDtudo.Services.DarkMessageBox.Show(
                    $"Já existe uma coleção MyAnime com o título '{tituloMyAnime}'.",
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

            var dtoAnime = ConversorAnimeDtoService.CriarAdicionaAnimeDto(_animeAtual, myAnimeId);
            var animeExistente = await _apiMyAnimesService.ObterAnimePorMalIdAsync(_animeAtual.MalId);
            if (animeExistente is not null)
            {
                if (!ConfirmarSubstituicaoAnime())
                    return;

                var atualizaAnime = ConversorAnimeDtoService.CriarAtualizaAnimeDto(_animeAtual, myAnimeId);
                await _apiMyAnimesService.AtualizarAnimeAsync(_animeAtual.MalId, atualizaAnime);
            }
            else
            {
                await _apiMyAnimesService.AdicionarAnimeAsync(dtoAnime);
            }

            await _apiMyAnimesService.AssociarAnimeAoMyAnimeAsync(_animeAtual.MalId, myAnimeId);

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

            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Anime salvo com sucesso e relacionado ao MyAnime ID {myAnimeId}.",
                "Sucesso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

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
