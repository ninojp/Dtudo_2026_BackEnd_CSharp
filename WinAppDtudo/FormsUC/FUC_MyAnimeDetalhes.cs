using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.MyAnimeList;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public class FUC_MyAnimeDetalhes : UserControl
{
    public event EventHandler<int>? CardClicado;
    public event EventHandler<int>? EditarMyAnimeSolicitado;

    private readonly int _myAnimeId;
    private readonly ApiMyAnimesService _apiMyAnimesService = new();
    private readonly CriadorDeEstruturas _criadorDeEstruturas = new();

    private readonly Label _lblTitulo;
    private readonly Label _lblResumo;
    private readonly Label _lblMyAnimeId;
    private readonly TextBox _txtMyAnimeId;
    private readonly Label _lblStatus;
    private readonly Button _btnSalvarEstrutura;
    private readonly Button _btnEditarMyAnime;
    private readonly FlowLayoutPanel _flpCards;

    private ObterMyAnimeDto? _myAnimeAtual;
    private List<ObterAnimeDto> _animesAtuais = [];

    public FUC_MyAnimeDetalhes(int myAnimeId)
    {
        _myAnimeId = myAnimeId;

        var tlpMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Black
        };
        tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var tlpTopo = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Black,
            Padding = new Padding(24, 20, 24, 12)
        };
        tlpTopo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (var row = 0; row < tlpTopo.RowCount; row++)
            tlpTopo.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _lblTitulo = new Label
        {
            AutoSize = false,
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Black", 18F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Text = "MyAnime",
            MinimumSize = new Size(0, 48),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblResumo = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 13F, FontStyle.Regular),
            ForeColor = Color.Goldenrod,
            Text = "",
            Margin = new Padding(0, 0, 0, 12)
        };

        _lblMyAnimeId = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Regular),
            ForeColor = Color.Gold,
            BackColor = Color.Black,
            Text = "MyAnime ID:",
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 8, 8, 8)
        };

        _txtMyAnimeId = new TextBox
        {
            ReadOnly = true,
            Width = 110,
            Font = new Font("Segoe UI Black", 13F, FontStyle.Bold),
            Text = _myAnimeId.ToString(),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            ForeColor = DarkModeColors.TextColor,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 16, 4)
        };

        _btnSalvarEstrutura = new Button
        {
            Text = "💾 Salvar em Disco"
        };

        _btnEditarMyAnime = new Button
        {
            Text = "Editar MyAnime"
        };

        var flpAcoes = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Black,
            Margin = Padding.Empty
        };

        ConfigurarBotaoAcao(_btnSalvarEstrutura);
        ConfigurarBotaoAcao(_btnEditarMyAnime);
        flpAcoes.Controls.AddRange([
            _lblMyAnimeId,
            _txtMyAnimeId,
            _btnSalvarEstrutura,
            _btnEditarMyAnime
        ]);

        tlpTopo.Controls.Add(_lblTitulo, 0, 0);
        tlpTopo.Controls.Add(_lblResumo, 0, 1);
        tlpTopo.Controls.Add(flpAcoes, 0, 2);

        _flpCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Black,
            Padding = new Padding(24, 16, 24, 16),
            AutoScrollMargin = new Size(12, 12)
        };

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.DarkGray,
            Padding = new Padding(24, 8, 24, 8),
            Text = "—"
        };

        tlpMain.Controls.Add(tlpTopo, 0, 0);
        tlpMain.Controls.Add(_flpCards, 0, 1);
        tlpMain.Controls.Add(_lblStatus, 0, 2);

        Controls.Add(tlpMain);

        Load += async (_, _) => await CarregarDadosAsync();
        _btnSalvarEstrutura.Click += BtnSalvarEstrutura_Click;
        _btnEditarMyAnime.Click += (_, _) => EditarMyAnimeSolicitado?.Invoke(this, _myAnimeId);
        _lblMyAnimeId.Click += (_, _) => CopiarMyAnimeId();
        _txtMyAnimeId.Click += (_, _) => CopiarMyAnimeId();

        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    private static void ConfigurarBotaoAcao(Button button)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(
            TextRenderer.MeasureText(button.Text, button.Font).Width + 32,
            button.Font.Height + 20);
        button.Padding = new Padding(14, 8, 14, 8);
        button.Margin = new Padding(0, 4, 12, 4);
    }

    private async Task CarregarDadosAsync()
    {
        try
        {
            _lblStatus.Text = "⏳ Carregando detalhes do MyAnime...";
            _btnSalvarEstrutura.Enabled = false;
            _btnEditarMyAnime.Enabled = false;
            _txtMyAnimeId.Text = _myAnimeId.ToString();

            _myAnimeAtual = await _apiMyAnimesService.ObterMyAnimePorIdAsync(_myAnimeId);
            if (_myAnimeAtual is null)
            {
                _lblStatus.Text = "❌ MyAnime não encontrado.";
                WinAppDtudo.Services.DarkMessageBox.Show($"MyAnime ID {_myAnimeId} não encontrado na ApiMyAnimes.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _animesAtuais = [];
            if (_myAnimeAtual.AnimesMalId.Count > 0)
            {
                foreach (var malId in _myAnimeAtual.AnimesMalId.Distinct())
                {
                    var anime = await _apiMyAnimesService.ObterAnimePorMalIdAsync(malId);
                    if (anime is not null)
                        _animesAtuais.Add(anime);
                }
            }
            else
            {
                _animesAtuais = await _apiMyAnimesService.ObterAnimesPorMyAnimeIdAsync(_myAnimeId);
            }

            _animesAtuais = _animesAtuais
                .GroupBy(a => a.MalId)
                .Select(g => g.First())
                .OrderBy(a => a.Year ?? int.MaxValue)
                .ThenBy(a => a.Titulo)
                .ToList();

            _lblTitulo.Text = _myAnimeAtual.Titulo;
            _lblResumo.Text = $"Animes relacionados: {_animesAtuais.Count}";
            _txtMyAnimeId.Text = _myAnimeAtual.Id.ToString();

            PopularCards();

            _lblStatus.Text = _animesAtuais.Count == 0
                ? "⚠️ Nenhum anime relacionado encontrado para esta coleção."
                : "✅ Coleção carregada.";

            _btnSalvarEstrutura.Enabled = _animesAtuais.Count > 0;
            _btnEditarMyAnime.Enabled = true;
        }
        catch (HttpRequestException ex)
        {
            _lblStatus.Text = "❌ Erro de conexão com ApiMyAnimes.";
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Falha ao consultar ApiMyAnimes em:\n{ApiMyAnimesService.ApiBase}\n\nDetalhes: {ex.Message}",
                "Erro de Conexão",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao carregar detalhes.";
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao carregar detalhes do MyAnime:\n\n{ex.Message}",
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public Task AtualizarAsync() => CarregarDadosAsync();

    private void CopiarMyAnimeId()
    {
        var texto = _txtMyAnimeId.Text.Trim();
        if (string.IsNullOrWhiteSpace(texto))
        {
            _lblStatus.Text = "Nenhum MyAnime ID disponivel para copiar.";
            return;
        }

        try
        {
            Clipboard.SetText(texto);
            _txtMyAnimeId.SelectAll();
            _lblStatus.Text = $"MyAnime ID {texto} copiado para a area de transferencia.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Nao foi possivel copiar o MyAnime ID.";
            WinAppDtudo.Services.DarkMessageBox.Show($"Falha ao copiar para a area de transferencia:\n\n{ex.Message}",
                "Clipboard indisponivel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PopularCards()
    {
        var cards = _flpCards.Controls.OfType<UC_AnimeCard>().ToList();
        _flpCards.Controls.Clear();
        foreach (var card in cards)
            card.Dispose();

        _flpCards.SuspendLayout();
        foreach (var anime in _animesAtuais)
        {
            var card = new UC_AnimeCard();
            card.CarregarDados(new AnimeSearchCard
            {
                MalId = anime.MalId,
                Title = anime.Titulo,
                TitleEnglish = anime.TitleEnglish,
                TitleJapanese = anime.TitleJapanese,
                TitleSynonyms = anime.TitleSynonyms,
                Type = anime.Type,
                Year = anime.Year,
                Score = anime.Score,
                ImageUrl = anime.ImagensUrlMal.FirstOrDefault()
            });
            var malId = anime.MalId;
            card.CardClicado += (_, _) => CardClicado?.Invoke(this, malId);
            _flpCards.Controls.Add(card);
        }
        _flpCards.ResumeLayout();
    }

    private async void BtnSalvarEstrutura_Click(object? sender, EventArgs e)
    {
        if (_myAnimeAtual is null || _animesAtuais.Count == 0)
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Não há dados carregados para exportar.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecione o diretório onde a estrutura do MyAnime será salva.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
            return;

        var pastaRaiz = CriadorDeEstruturas.ObterCaminhoPastaRaiz(_myAnimeAtual, dialog.SelectedPath);
        if (Directory.Exists(pastaRaiz))
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"A pasta já existe e não será sobrescrita:\n\n{pastaRaiz}",
                "Exportação interrompida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _btnSalvarEstrutura.Enabled = false;
        _lblStatus.Text = "⏳ Criando estrutura de pastas e baixando imagens...";

        try
        {
            var resultado = await _criadorDeEstruturas.CriarEstruturaAsync(_myAnimeAtual, _animesAtuais, dialog.SelectedPath);

            _lblStatus.Text = $"✅ Estrutura criada em: {resultado.PastaRaiz}";

            var mensagem =
                $"Estrutura criada com sucesso.\n\n" +
                $"Pasta raiz: {resultado.PastaRaiz}\n" +
                $"Pastas criadas: {resultado.TotalPastasCriadas}\n" +
                $"Imagens salvas: {resultado.TotalImagensSalvas}";

            if (resultado.Erros.Count > 0)
            {
                mensagem += $"\n\nOcorrências ({resultado.Erros.Count}):\n- " + string.Join("\n- ", resultado.Erros.Take(5));
            }

            WinAppDtudo.Services.DarkMessageBox.Show(mensagem, "Exportação concluída",
                MessageBoxButtons.OK,
                resultado.Erros.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao criar estrutura em disco.";
            WinAppDtudo.Services.DarkMessageBox.Show($"Falha ao criar estrutura:\n\n{ex.Message}",
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSalvarEstrutura.Enabled = _animesAtuais.Count > 0;
        }
    }

}
