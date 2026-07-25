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
    public event EventHandler<int>? MyAnimeExistenteSelecionado;

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
        Lbl_Ano.Text = string.Join("  ", estatisticas);
        var generos = anime.Genres?
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .ToList() ?? [];
        Lbl_ScoreStat.Text = generos.Count > 0
            ? $"🎭 {string.Join(" • ", generos)}"
            : string.Empty;
        Lbl_ScoreStat.Visible = generos.Count > 0;
        int larguraGenero = Math.Max(200, Pnl_Stats.ClientSize.Width - Lbl_ScoreStat.Left - 20);
        Lbl_ScoreStat.Width = larguraGenero;
        Lbl_ScoreStat.Height = generos.Count > 0
            ? Math.Max(35, TextRenderer.MeasureText(
                Lbl_ScoreStat.Text,
                Lbl_ScoreStat.Font,
                new Size(larguraGenero, int.MaxValue),
                TextFormatFlags.WordBreak).Height + 4)
            : 0;
        int proximaLinha = Lbl_ScoreStat.Bottom + 5;
        Lbl_Episodios.Location = new Point(Lbl_Episodios.Left, proximaLinha);
        Lbl_Duracao.Location = new Point(Lbl_Duracao.Left, Lbl_Episodios.Bottom);
        Lbl_Episodios.Text = anime.Episodes is > 0 ? $"📺 {anime.Episodes} ep." : string.Empty;
        Lbl_Duracao.Text = !string.IsNullOrWhiteSpace(anime.Duration) ? $"⏱ {anime.Duration}" : string.Empty;

        // Painel direito: detalhes dinâmicos
        Pnl_Info.SuspendLayout();
        Pnl_Info.Controls.Clear();
        _yOffset = 10;

        int larguraValor = Math.Max(Pnl_Info.ClientSize.Width - 265, 600);
        
        AdicionarRelacoes(relacoes);

        AdicionarDetalhe("Mal ID", anime.MalId.ToString(), larguraValor);
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
        AdicionarTextoLongo("Sinopse", anime.Synopsis, larguraValor);
        AdicionarTextoLongo("Contexto / Fundo", anime.Background, larguraValor);

        Pnl_Info.AutoScrollMinSize = new Size(0, _yOffset + 20);
        Pnl_Info.ResumeLayout(true);
    }

    private void ConfigurarTitulosSecundarios(JikanAnimeDetalhes anime)
    {
        Lbl_TituloIngles.Text = anime.TitleEnglish ?? string.Empty;
        Lbl_TituloIngles.Visible = !string.IsNullOrWhiteSpace(anime.TitleEnglish);

        Lbl_Sinonimo.Text = anime.TitleSynonyms?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? string.Empty;
        Lbl_Sinonimo.Visible = !string.IsNullOrWhiteSpace(Lbl_Sinonimo.Text);

        Lbl_TituloJapones.Text = anime.TitleJapanese ?? string.Empty;
        Lbl_TituloJapones.Visible = !string.IsNullOrWhiteSpace(anime.TitleJapanese);
        Pnl_Header.PerformLayout();
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
        int topoSecao = _yOffset;
        var lblCampo = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Location = new Point(4, topoSecao),
            Size = new Size(148 + larguraValor, 32),
            Text = campo + ":",
            TextAlign = ContentAlignment.MiddleLeft
        };
        int topoTexto = topoSecao + lblCampo.Height + 10;

        int larguraTexto = Math.Max(148 + larguraValor - 12, 450);
        var lblTexto = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(255, 115, 0),
            BackColor = Pnl_Info.BackColor,
            Location = new Point(8, topoTexto),
            Size = new Size(larguraTexto, Math.Max(60, TextRenderer.MeasureText(
                texto, new Font("Segoe UI", 9.5F), new Size(larguraTexto - 8, int.MaxValue),
                TextFormatFlags.WordBreak).Height + 12)),
            Text = texto,
            //ScrollBars = ScrollBars.Vertical,
            TabStop = true
        };
        Pnl_Info.Controls.Add(lblCampo);
        Pnl_Info.Controls.Add(lblTexto);
        _yOffset = lblTexto.Bottom + 18;
        AdicionarSeparador(larguraValor);
    }

    private void AdicionarSeparador(int larguraValor)
    {
        var sep = new Panel
        {
            BackColor = Color.LightSteelBlue,
            Location = new Point(20, _yOffset),
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
            var myAnimeExistente = await _apiMyAnimesService.ObterMyAnimePorTituloAsync(tituloMyAnime);
            if (myAnimeExistente is not null)
            {
                MostrarMyAnimeExistente(myAnimeExistente);
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
            var myAnimeExistente = await _apiMyAnimesService.ObterMyAnimePorTituloAsync(tituloMyAnime);
            if (myAnimeExistente is not null)
                MostrarMyAnimeExistente(myAnimeExistente);
            else
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
            var animeExistente = await _apiMyAnimesService.ObterAnimePorMalIdAsync(_animeAtual.MalId);
            if (animeExistente is not null)
            {
                if (!ConfirmarSubstituicaoAnime())
                    return;

                await _apiMyAnimesService.AtualizarAnimeAsync(_animeAtual.MalId, dtoAnime);
            }
            else
            {
                await _apiMyAnimesService.AdicionarAnimeAsync(dtoAnime);
            }

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

    private bool ConfirmarSubstituicaoAnime()
    {
        using var dialogo = new Form
        {
            Text = "Anime já existente",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(430, 145)
        };

        var mensagem = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 75,
            Padding = new Padding(12),
            Text = $"O anime com MalId {_animeAtual?.MalId} já existe na base local.\nDeseja substituir os dados existentes?",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var painelBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 55,
            Padding = new Padding(8),
            WrapContents = false
        };

        var cancelar = new Button
        {
            Text = "Cancelar",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        var substituir = new Button
        {
            Text = "Substituir",
            AutoSize = true,
            DialogResult = DialogResult.OK
        };

        painelBotoes.Controls.Add(cancelar);
        painelBotoes.Controls.Add(substituir);
        dialogo.Controls.Add(mensagem);
        dialogo.Controls.Add(painelBotoes);
        dialogo.AcceptButton = substituir;
        dialogo.CancelButton = cancelar;

        return dialogo.ShowDialog(FindForm()) == DialogResult.OK;
    }

    private void MostrarMyAnimeExistente(ObterMyAnimeDto myAnime)
    {
        using var dialogo = new Form
        {
            Text = "MyAnime já cadastrado",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(1040, 380)
        };

        var mensagem = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 240,
            Padding = new Padding(12),
            Text = $"MyAnime {myAnime.Id}: {myAnime.Titulo}\nO MyAnime já foi cadastrado!",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var painelBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 140,
            Padding = new Padding(8),
            WrapContents = false
        };

        var ok = new Button
        {
            Text = "OK",
            AutoSize = true,
            DialogResult = DialogResult.OK
        };
        var acessar = new Button
        {
            Text = "Acessar MyAnime",
            AutoSize = true
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

        dialogo.ShowDialog(FindForm());
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
