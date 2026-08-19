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
    private readonly ApiMyAnimesService _apiMyAnimesService;
    private readonly IFileStorageApiClient _fileStorageApiClient;
    private readonly CriadorDeEstruturas _criadorDeEstruturas;

    private readonly Label _lblTitulo;
    private readonly Label _lblResumo;
    private readonly Label _lblMyAnimeId;
    private readonly TextBox _txtMyAnimeId;
    private readonly Label _lblStatus;
    private readonly Button _btnSalvarEstrutura;
    private readonly Button _btnExcluirEstrutura;
    private readonly Button _btnEditarMyAnime;
    private readonly FlowLayoutPanel _flpCards;

    private ObterMyAnimeDto? _myAnimeAtual;
    private List<ObterAnimeDto> _animesAtuais = [];

    public FUC_MyAnimeDetalhes(int myAnimeId, ApiMyAnimesService? apiMyAnimesService = null)
    {
        _myAnimeId = myAnimeId;
        _apiMyAnimesService = apiMyAnimesService ?? new ApiMyAnimesService();
        _fileStorageApiClient = new FileStorageApiClient();
        _criadorDeEstruturas = new CriadorDeEstruturas(_fileStorageApiClient);

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
            Text = "Exportar para ApiFileStorage"
        };

        _btnExcluirEstrutura = new Button
        {
            Text = "Excluir capas da ApiFileStorage"
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
        ConfigurarBotaoAcao(_btnExcluirEstrutura);
        ConfigurarBotaoAcao(_btnEditarMyAnime);
        flpAcoes.Controls.AddRange([
            _lblMyAnimeId,
            _txtMyAnimeId,
            _btnSalvarEstrutura,
            _btnExcluirEstrutura,
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
        _btnExcluirEstrutura.Click += BtnExcluirEstrutura_Click;
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
            _btnExcluirEstrutura.Enabled = false;
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
            _btnExcluirEstrutura.Enabled = _animesAtuais.Count > 0;
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

        _btnSalvarEstrutura.Enabled = false;
        _lblStatus.Text = "Consultando pastas de exportação autorizadas...";

        try
        {
            var destino = await SelecionarDestinoAsync("Selecione a pasta para exportar o MyAnime.");
            if (destino is null)
            {
                _lblStatus.Text = "Exportação cancelada antes do envio.";
                return;
            }

            _lblStatus.Text = "Preparando exportação segura na ApiFileStorage...";
            var progresso = new Progress<ProgressoExportacao>(atualizacao =>
            {
                _lblStatus.Text = $"{atualizacao.PercentualConcluido}% - {atualizacao.Mensagem}";
            });
            var resultado = await _criadorDeEstruturas.CriarEstruturaAsync(
                _myAnimeAtual,
                _animesAtuais,
                progresso,
                destino.Id);

            _lblStatus.Text = "Exportação concluída na ApiFileStorage.";

            var mensagem =
                $"Exportação concluída com segurança.\n\n" +
                $"Pasta selecionada: {destino.DisplayName}\n" +
                $"Destinos lógicos preparados: {resultado.TotalPastasCriadas}\n" +
                $"Imagens salvas: {resultado.TotalImagensSalvas}";

            if (resultado.TotalImagensRepetidas > 0)
                mensagem += $"\nImagens reconciliadas: {resultado.TotalImagensRepetidas}";

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
            _lblStatus.Text = "Falha na exportação para a ApiFileStorage.";
            WinAppDtudo.Services.DarkMessageBox.Show($"Falha ao exportar estrutura:\n\n{ex.Message}",
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSalvarEstrutura.Enabled = _animesAtuais.Count > 0;
            _btnExcluirEstrutura.Enabled = _animesAtuais.Count > 0;
        }
    }

    private async void BtnExcluirEstrutura_Click(object? sender, EventArgs e)
    {
        if (_myAnimeAtual is null || _animesAtuais.Count == 0)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                "Não há capas associadas para excluir.",
                "Aviso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _btnSalvarEstrutura.Enabled = false;
        _btnExcluirEstrutura.Enabled = false;
        _lblStatus.Text = "Consultando pastas de exportação autorizadas...";

        try
        {
            var destino = await SelecionarDestinoAsync("Selecione a pasta cujas capas serão excluídas.");
            if (destino is null)
            {
                _lblStatus.Text = "Exclusão cancelada antes da prévia.";
                return;
            }

            _lblStatus.Text = "Preparando prévia de exclusão segura...";
            var plano = await _fileStorageApiClient.PrepareExportAsync(
                _myAnimeAtual.Id,
                _myAnimeAtual.Titulo,
                _animesAtuais
                    .Select(anime => new WinAppStorageExportAnime(
                        anime.MalId,
                        anime.Year,
                        anime.Titulo,
                        anime.Type))
                    .ToArray(),
                destino.Id);
            var previa = await _fileStorageApiClient.PreviewDeleteAsync(
                plano.Items.Select(item => item.ObjectId).ToArray());
            var tamanhoTotal = previa.Items.Sum(item => item.Length);

            var confirmacao = WinAppDtudo.Services.DarkMessageBox.Show(
                $"Prévia de exclusão:\n\n" +
                $"Arquivos: {previa.Items.Count}\n" +
                $"Tamanho: {tamanhoTotal:N0} bytes\n\n" +
                "Os arquivos serão movidos para a lixeira da ApiFileStorage e poderão ser purgados após sete dias.\n\n" +
                "Deseja continuar?",
                "Confirmar exclusão em massa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirmacao != DialogResult.Yes)
            {
                _lblStatus.Text = "Exclusão cancelada após a prévia.";
                return;
            }

            var totp = DarkInputDialog.Show(
                "Informe o código TOTP para autorizar esta exclusão em massa.",
                "Step-up MFA");
            if (string.IsNullOrWhiteSpace(totp))
            {
                _lblStatus.Text = "Exclusão cancelada: step-up não informado.";
                return;
            }

            _lblStatus.Text = "Validando step-up e movendo arquivos para a lixeira...";
            await _fileStorageApiClient.GrantDeleteStepUpAsync(totp.Trim());
            var resultado = await _fileStorageApiClient.DeleteBatchAsync(previa.PreviewId);
            var excluidos = resultado.Items.Count(item => item.Status is "deleted" or "replayed");
            var falhas = resultado.Items.Count - excluidos;

            _lblStatus.Text = falhas == 0
                ? $"Exclusão concluída: {excluidos} arquivo(s) na lixeira."
                : $"Exclusão concluída com ocorrências: {excluidos} concluído(s), {falhas} falha(s).";
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Exclusão em massa finalizada.\n\n" +
                $"Movidos para a lixeira: {excluidos}\n" +
                $"Com falha ou ausentes: {falhas}\n" +
                "A purga automática respeitará a janela de sete dias.",
                "Exclusão de capas",
                MessageBoxButtons.OK,
                falhas == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Falha na exclusão em massa.";
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Falha ao excluir capas pela ApiFileStorage:\n\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _btnSalvarEstrutura.Enabled = _animesAtuais.Count > 0;
            _btnExcluirEstrutura.Enabled = _animesAtuais.Count > 0;
        }
    }

    private async Task<WinAppStorageExportDestination?> SelecionarDestinoAsync(string mensagem)
    {
        var destinos = await _fileStorageApiClient.GetExportDestinationsAsync();
        if (destinos.Count == 0)
        {
            throw new InvalidOperationException(
                "Nenhuma pasta de exportação foi configurada na ApiFileStorage.");
        }

        var destinoId = DarkSelectionDialog.Show(
            mensagem,
            "Pasta de exportação",
            destinos
                .Select(destino => new DarkSelectionOption(destino.Id, destino.DisplayName))
                .ToArray());
        return destinoId is null
            ? null
            : destinos.First(destino =>
                string.Equals(destino.Id, destinoId, StringComparison.Ordinal));
    }

}
