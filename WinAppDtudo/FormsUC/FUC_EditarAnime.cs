using System.Net;
using LibDtudo.Shared.Dtos;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public sealed class FUC_EditarAnime : UserControl
{
    public event EventHandler<AnimeEditSavedEventArgs>? AnimeSalvo;

    private static readonly string[] TiposAnime = ["TV", "Movie", "OVA", "ONA", "Special", "Music", "CM", "PV", "TV Special"];
    private static readonly string[] StatusAnime = ["Finished Airing", "Currently Airing", "Not yet aired"];
    private static readonly string[] Temporadas = ["winter", "spring", "summer", "fall"];

    private readonly ApiMyAnimesService _apiMyAnimesService = new();
    private readonly AnimeEditFieldSet _fields = new();
    private readonly int _malId;

    private Label _lblTitulo = null!;
    private Label _lblTituloIngles = null!;
    private Label _lblSinonimos = null!;
    private Label _lblTituloJapones = null!;
    private Label _lblStatus = null!;
    private Label _lblEstatisticasRapidas = null!;
    private Label _lblGeneros = null!;
    private Label _lblEpisodios = null!;
    private Label _lblTempoPorEpisodio = null!;
    private PictureBox _pbxCapa = null!;
    private Button _btnSalvar = null!;
    private Button _btnRecarregar = null!;
    private Panel _pnlEditor = null!;
    private ObterAnimeDto? _animeAtual;

    public FUC_EditarAnime(int malId)
    {
        _malId = malId;
        InitializeLayout();
        Load += async (_, _) => await CarregarAsync();
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    private void InitializeLayout()
    {
        BackColor = DarkModeColors.BackgroundColor;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 200,
            BackColor = Color.FromArgb(25, 30, 80),
            Padding = new Padding(50, 6, 13, 4)
        };

        _lblTitulo = new Label
        {
            AutoEllipsis = true,
            Font = new Font("Segoe UI Black", 15F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(25, 30, 80),
            Text = $"Editar Anime #{_malId}",
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblTituloIngles = CriarTituloHeader(Color.Gold, 15F, FontStyle.Bold);
        _lblSinonimos = CriarTituloHeader(Color.LightGray, 12F, FontStyle.Regular);
        _lblTituloJapones = CriarTituloHeader(Color.LightSteelBlue, 11F, FontStyle.Regular);

        _lblStatus = new Label
        {
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.Gold,
            BackColor = Color.FromArgb(25, 30, 80),
            Text = "Carregando registro local...",
            TextAlign = ContentAlignment.MiddleLeft
        };

        header.Controls.Add(_lblTituloJapones);
        header.Controls.Add(_lblSinonimos);
        header.Controls.Add(_lblTituloIngles);
        header.Controls.Add(_lblStatus);
        header.Controls.Add(_lblTitulo);
        header.Resize += (_, _) => OrganizarTitulosDoCabecalho(header);

        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 550,
            BackColor = DarkModeColors.BackgroundSecondaryColor
        };

        _pbxCapa = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 576,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(50, 50, 60)
        };

        _btnSalvar = CriarBotao("Salvar Alteracoes");
        _btnSalvar.Height = 52;
        _btnSalvar.Width = 360;
        _btnSalvar.Margin = new Padding(0, 0, 0, 10);
        _btnSalvar.Click += async (_, _) => await SalvarAsync();

        _btnRecarregar = CriarBotao("Recarregar");
        _btnRecarregar.Height = 44;
        _btnRecarregar.Width = 360;
        _btnRecarregar.Margin = new Padding(0);
        _btnRecarregar.Click += async (_, _) => await CarregarAsync();

        var statsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(13, 10, 8, 4),
            BackColor = DarkModeColors.BackgroundSecondaryColor
        };

        _lblEstatisticasRapidas = CriarStatsLabel(Color.Gold, 12F, FontStyle.Bold);
        _lblGeneros = CriarStatsLabel(Color.DarkOrange, 9.5F, FontStyle.Regular);
        _lblEpisodios = CriarStatsLabel(Color.Gold, 12F, FontStyle.Bold);
        _lblTempoPorEpisodio = CriarStatsLabel(Color.Gold, 10F, FontStyle.Bold);

        statsPanel.Controls.Add(_btnRecarregar);
        statsPanel.Controls.Add(_btnSalvar);
        statsPanel.Controls.Add(_lblTempoPorEpisodio);
        statsPanel.Controls.Add(_lblEpisodios);
        statsPanel.Controls.Add(_lblGeneros);
        statsPanel.Controls.Add(_lblEstatisticasRapidas);
        statsPanel.Resize += (_, _) => OrganizarPainelEsquerdo(statsPanel);
        leftPanel.Controls.Add(statsPanel);
        leftPanel.Controls.Add(_pbxCapa);

        _pnlEditor = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(5, 4, 5, 4),
            BackColor = DarkModeColors.BackgroundSecondaryColor
        };

        Controls.Add(_pnlEditor);
        Controls.Add(leftPanel);
        Controls.Add(header);
        OrganizarTitulosDoCabecalho(header);
    }

    private static Label CriarTituloHeader(Color color, float size, FontStyle style)
    {
        return new Label
        {
            AutoEllipsis = true,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            BackColor = Color.FromArgb(25, 30, 80),
            TextAlign = ContentAlignment.MiddleLeft,
            Visible = false
        };
    }

    private static Label CriarStatsLabel(Color color, float size, FontStyle style)
    {
        return new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", size, style),
            ForeColor = color,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private void PopularCabecalho(ObterAnimeDto anime)
    {
        _lblTitulo.Text = ObterTitulo(anime);
        _lblTituloIngles.Text = anime.TitleEnglish ?? string.Empty;
        _lblSinonimos.Text = string.Join("  -  ", (anime.TitleSynonyms ?? [])
            .Where(titulo => !string.IsNullOrWhiteSpace(titulo))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        _lblTituloJapones.Text = anime.TitleJapanese ?? string.Empty;

        _lblTituloIngles.Visible = !string.IsNullOrWhiteSpace(_lblTituloIngles.Text);
        _lblSinonimos.Visible = !string.IsNullOrWhiteSpace(_lblSinonimos.Text);
        _lblTituloJapones.Visible = !string.IsNullOrWhiteSpace(_lblTituloJapones.Text);

        if (Controls.OfType<Panel>().LastOrDefault() is { } header)
            OrganizarTitulosDoCabecalho(header);
    }

    private void PopularResumoEsquerdo(ObterAnimeDto anime)
    {
        var estatisticas = new List<string>();
        if (anime.Year.HasValue)
            estatisticas.Add(anime.Year.Value.ToString());
        if (!string.IsNullOrWhiteSpace(anime.Type))
            estatisticas.Add(anime.Type);
        if (anime.Score.HasValue)
            estatisticas.Add(anime.Score.Value.ToString("0.00"));

        _lblEstatisticasRapidas.Text = string.Join("  ", estatisticas);
        _lblGeneros.Text = anime.Genres.Count > 0 ? string.Join(" - ", anime.Genres) : string.Empty;
        _lblGeneros.Visible = anime.Genres.Count > 0;
        _lblEpisodios.Text = anime.Episodes is > 0
            ? $"{anime.Episodes} ep."
            : anime.Episodios > 0 ? $"{anime.Episodios} ep." : string.Empty;
        _lblTempoPorEpisodio.Text = anime.Duration ?? string.Empty;

        if (_lblEstatisticasRapidas.Parent is Panel panel)
            OrganizarPainelEsquerdo(panel);
    }

    private void OrganizarTitulosDoCabecalho(Panel header)
    {
        if (header.ClientSize.Width <= header.Padding.Horizontal)
            return;

        var width = header.ClientSize.Width - header.Padding.Left - header.Padding.Right;
        var columnWidth = Math.Max(1, (width - 20) / 2);
        var left = header.Padding.Left;
        var right = left + columnWidth + 20;
        const int rowHeight = 50;
        var firstRow = header.Padding.Top;
        var secondRow = firstRow + rowHeight + 4;

        _lblTitulo.Location = new Point(left, firstRow);
        _lblTitulo.Size = new Size(columnWidth, rowHeight);
        _lblTituloIngles.Location = new Point(right, firstRow);
        _lblTituloIngles.Size = new Size(columnWidth, rowHeight);
        _lblSinonimos.Location = new Point(left, secondRow);
        _lblSinonimos.Size = new Size(columnWidth, rowHeight);
        _lblTituloJapones.Location = new Point(right, secondRow);
        _lblTituloJapones.Size = new Size(columnWidth, rowHeight);
        _lblStatus.Location = new Point(left, secondRow + rowHeight + 6);
        _lblStatus.Size = new Size(width, 34);
    }

    private void OrganizarPainelEsquerdo(Panel panel)
    {
        var width = Math.Max(200, panel.ClientSize.Width - panel.Padding.Horizontal - 20);
        var left = Math.Max(panel.Padding.Left, (panel.ClientSize.Width - width) / 2);
        var y = panel.Padding.Top;

        _lblEstatisticasRapidas.Location = new Point(left, y);
        _lblEstatisticasRapidas.Size = new Size(width, 40);
        y = _lblEstatisticasRapidas.Bottom + 5;

        _lblEpisodios.Location = new Point(left, y);
        _lblEpisodios.Size = new Size(width, 50);
        y = _lblEpisodios.Bottom + 5;

        _lblTempoPorEpisodio.Location = new Point(left, y);
        _lblTempoPorEpisodio.Size = new Size(width, 50);
        y = _lblTempoPorEpisodio.Bottom + 5;

        _lblGeneros.Location = new Point(left, y);
        _lblGeneros.Width = width;
        _lblGeneros.Height = _lblGeneros.Visible
            ? Math.Max(35, TextRenderer.MeasureText(
                _lblGeneros.Text,
                _lblGeneros.Font,
                new Size(width, int.MaxValue),
                TextFormatFlags.WordBreak).Height + 4)
            : 0;

        _btnSalvar.Width = Math.Min(300, width);
        _btnRecarregar.Width = _btnSalvar.Width;
        var buttonLeft = (panel.ClientSize.Width - _btnSalvar.Width) / 2;
        var buttonTop = (_lblGeneros.Visible ? _lblGeneros.Bottom : _lblTempoPorEpisodio.Bottom) + 30;
        _btnSalvar.Location = new Point(buttonLeft, buttonTop);
        _btnRecarregar.Location = new Point(buttonLeft, _btnSalvar.Bottom + 12);
    }

    private static Button CriarBotao(string text)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = DarkModeColors.AccentColor,
            ForeColor = DarkModeColors.TextColor,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
    }

    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando registro local...");
        try
        {
            var anime = await _apiMyAnimesService.ObterAnimePorMalIdAsync(_malId);
            if (anime is null)
            {
                _lblStatus.Text = $"Anime com MalId {_malId} nao encontrado no DB_Local.";
                WinAppDtudo.Services.DarkMessageBox.Show(_lblStatus.Text, "Anime nao encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _animeAtual = anime;
            PopularCabecalho(anime);
            PopularResumoEsquerdo(anime);
            _lblStatus.Text = $"Registro carregado em {DateTime.Now:HH:mm:ss}.";
            PopularCampos(anime);
            await CarregarCapaAsync(anime);
        }
        catch (HttpRequestException ex)
        {
            _lblStatus.Text = "Erro de conexao com ApiMyAnimes.";
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Nao foi possivel conectar a ApiMyAnimes em:\n{ApiMyAnimesService.ApiBase}\n\nDetalhes: {ex.Message}",
                "Erro de Conexao",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Erro ao carregar anime.";
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao carregar anime local:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void PopularCampos(ObterAnimeDto anime)
    {
        _pnlEditor.SuspendLayout();
        _pnlEditor.Controls.Clear();
        _fields.Begin(_pnlEditor);

        _fields.AddText("MalId", "Mal ID", anime.MalId.ToString(), readOnly: true);
        _fields.AddText("Titulo", "Titulo", anime.Titulo, required: true);
        _fields.AddText("Episodios", "Episodios", anime.Episodios.ToString(), required: true);
        _fields.AddText("MyAnimeID", "MyAnime ID", anime.MyAnimeID.ToString());
        _fields.AddText("Source", "Fonte", anime.Source);
        _fields.AddText("Rating", "Classificacao", anime.Rating);
        _fields.AddText("Aired", "Exibicao", anime.Aired);
        _fields.AddCombo("Season", "Temporada", anime.Season, Temporadas);
        _fields.AddText("Score", "Score", anime.Score?.ToString());
        _fields.AddText("ScoredBy", "Votos da pontuacao", anime.ScoredBy?.ToString());
        _fields.AddText("Rank", "Rank", anime.Rank?.ToString());
        _fields.AddText("Popularity", "Popularidade", anime.Popularity?.ToString());
        _fields.AddText("Members", "Membros", anime.Members?.ToString());
        _fields.AddText("Favorites", "Favoritos", anime.Favorites?.ToString());
        _fields.AddText("Year", "Ano", anime.Year?.ToString());
        _fields.AddCombo("Type", "Tipo", anime.Type, TiposAnime);
        _fields.AddText("Episodes", "Episodes", anime.Episodes?.ToString());
        _fields.AddCombo("Status", "Status", anime.Status, StatusAnime);
        _fields.AddBool("Airing", "Airing", anime.Airing);
        _fields.AddBool("Approved", "Approved", anime.Approved);
        _fields.AddText("Duration", "Duracao", anime.Duration);
        _fields.AddText("Trailer", "Trailer", anime.Trailer);
        _fields.AddText("MalUrl", "MAL URL", anime.MalUrl);
        _fields.AddText("Title", "Title", anime.Title);
        _fields.AddText("TitleEnglish", "Title English", anime.TitleEnglish);
        _fields.AddText("TitleJapanese", "Title Japanese", anime.TitleJapanese);
        _fields.AddList("TitleSynonyms", "Title Synonyms", anime.TitleSynonyms);
        _fields.AddList("ImagensUrlMal", "Imagens URL MAL", anime.ImagensUrlMal);
        _fields.AddList("SubTitulos", "Subtitulos", anime.SubTitulos);
        _fields.AddList("Studios", "Estudios", anime.Studios);
        _fields.AddList("Producers", "Produtoras", anime.Producers);
        _fields.AddList("Licensors", "Licenciadores", anime.Licensors);
        _fields.AddList("Genres", "Generos", anime.Genres);
        _fields.AddList("ExplicitGenres", "Generos +18", anime.ExplicitGenres);
        _fields.AddList("Themes", "Temas", anime.Themes);
        _fields.AddList("Demographics", "Publico-alvo", anime.Demographics);
        _fields.AddLongText("Synopsis", "Sinopse", anime.Synopsis);
        _fields.AddLongText("Background", "Contexto / Fundo", anime.Background);
        _fields.Finish();
        _pnlEditor.ResumeLayout(true);
    }

    private async Task SalvarAsync()
    {
        if (_animeAtual is null)
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Carregue o anime antes de salvar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var errors = new List<string>();
        if (!_fields.TryRequiredString("Titulo", "Titulo", errors, out var titulo))
            titulo = string.Empty;
        if (!_fields.TryRequiredInt("Episodios", "Episodios", 1, 3000, errors, out var episodios))
            episodios = 1;

        var dto = new AtualizaAnimeDto
        {
            Titulo = titulo,
            Episodios = episodios,
            MyAnimeID = _fields.OptionalInt("MyAnimeID", "MyAnime ID", 0, null, errors) ?? 0,
            MalUrl = _fields.Text("MalUrl"),
            ImagensUrlMal = _fields.List("ImagensUrlMal"),
            SubTitulos = _fields.List("SubTitulos"),
            Trailer = _fields.OptionalText("Trailer"),
            Approved = _fields.Bool("Approved"),
            Title = _fields.OptionalText("Title"),
            TitleEnglish = _fields.OptionalText("TitleEnglish"),
            TitleJapanese = _fields.OptionalText("TitleJapanese"),
            TitleSynonyms = _fields.List("TitleSynonyms"),
            Type = _fields.OptionalText("Type"),
            Source = _fields.OptionalText("Source"),
            Episodes = _fields.OptionalInt("Episodes", "Episodes", 1, 3000, errors),
            Status = _fields.OptionalText("Status"),
            Airing = _fields.Bool("Airing"),
            Aired = _fields.OptionalText("Aired"),
            Duration = _fields.OptionalText("Duration"),
            Rating = _fields.OptionalText("Rating"),
            Score = _fields.OptionalDouble("Score", "Score", 0, 10, errors),
            ScoredBy = _fields.OptionalInt("ScoredBy", "Scored By", 0, null, errors),
            Rank = _fields.OptionalInt("Rank", "Rank", 0, null, errors),
            Popularity = _fields.OptionalInt("Popularity", "Popularity", 0, null, errors),
            Members = _fields.OptionalInt("Members", "Members", 0, null, errors),
            Favorites = _fields.OptionalInt("Favorites", "Favorites", 0, null, errors),
            Synopsis = _fields.OptionalText("Synopsis"),
            Background = _fields.OptionalText("Background"),
            Season = _fields.OptionalText("Season"),
            Year = _fields.OptionalInt("Year", "Year", 1900, 2200, errors),
            Producers = _fields.List("Producers"),
            Licensors = _fields.List("Licensors"),
            Studios = _fields.List("Studios"),
            Genres = _fields.List("Genres"),
            ExplicitGenres = _fields.List("ExplicitGenres"),
            Themes = _fields.List("Themes"),
            Demographics = _fields.List("Demographics")
        };

        if (errors.Count > 0)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                string.Join(Environment.NewLine, errors.Distinct()),
                "Revise os campos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true, "Salvando alteracoes...");
        try
        {
            await _apiMyAnimesService.AtualizarAnimeAsync(_animeAtual.MalId, dto);
            _lblStatus.Text = $"Alteracoes salvas em {DateTime.Now:HH:mm:ss}.";

            WinAppDtudo.Services.DarkMessageBox.Show("Anime atualizado com sucesso no DB_Local.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var salvo = await _apiMyAnimesService.ObterAnimePorMalIdAsync(_animeAtual.MalId);
            _animeAtual = salvo ?? _animeAtual;
            if (salvo is not null)
            {
                _lblTitulo.Text = $"Editar Anime #{salvo.MalId} - {ObterTitulo(salvo)}";
                await CarregarCapaAsync(salvo);
                AnimeSalvo?.Invoke(this, new AnimeEditSavedEventArgs(salvo.MalId, salvo.MyAnimeID));
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            WinAppDtudo.Services.DarkMessageBox.Show($"Anime com MalId {_animeAtual.MalId} nao encontrado para atualizacao.", "Nao encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (HttpRequestException ex)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Falha ao salvar na ApiMyAnimes em:\n{ApiMyAnimesService.ApiBase}\n\nDetalhes: {ex.Message}",
                "Erro de Conexao",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao salvar anime:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CarregarCapaAsync(ObterAnimeDto anime)
    {
        try
        {
            var url = anime.ImagensUrlMal.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
            if (string.IsNullOrWhiteSpace(url))
            {
                TrocarImagem(null);
                return;
            }

            var image = await ImageLoaderService.DownloadAsync(url);
            TrocarImagem(image);
        }
        catch
        {
            TrocarImagem(null);
        }
    }

    private void TrocarImagem(Image? image)
    {
        if (_pbxCapa.IsDisposed)
        {
            image?.Dispose();
            return;
        }

        var anterior = _pbxCapa.Image;
        _pbxCapa.Image = image;
        anterior?.Dispose();
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _btnSalvar.Enabled = !busy && _animeAtual is not null;
        _btnRecarregar.Enabled = !busy;
        UseWaitCursor = busy;
        if (!string.IsNullOrWhiteSpace(status))
            _lblStatus.Text = status;
    }

    private static string ObterTitulo(ObterAnimeDto anime)
    {
        if (!string.IsNullOrWhiteSpace(anime.Titulo)) return anime.Titulo;
        if (!string.IsNullOrWhiteSpace(anime.Title)) return anime.Title;
        return $"Anime #{anime.MalId}";
    }
}

public sealed class AnimeEditSavedEventArgs(int malId, int myAnimeId) : EventArgs
{
    public int MalId { get; } = malId;
    public int MyAnimeId { get; } = myAnimeId;
}
