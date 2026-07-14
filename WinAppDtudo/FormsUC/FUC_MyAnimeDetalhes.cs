using LibDtudo.Shared.Dtos;
using WinAppDtudo.Controls;
using WinAppDtudo.Forms;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public class FUC_MyAnimeDetalhes : UserControl
{
    public event EventHandler<int>? CardClicado;

    private readonly int _myAnimeId;
    private readonly ApiMyAnimesService _apiMyAnimesService = new();
    private readonly CriadorDeEstruturas _criadorDeEstruturas = new();
    private readonly ImportadorAnimesMyAnimeService _importadorAnimesMyAnimeService = new();

    private readonly Label _lblTitulo;
    private readonly Label _lblResumo;
    private readonly Label _lblMyAnimeId;
    private readonly TextBox _txtMyAnimeId;
    private readonly Label _lblStatus;
    private readonly Button _btnSalvarEstrutura;
    private readonly Button _btnSalvarAnimesNoBanco;
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
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 138F));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

        var pnlTopo = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 400,
            BackColor = Color.Black
        };

        _lblTitulo = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Black", 18F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Text = "MyAnime",
            Location = new Point(100, 10)
        };

        _lblResumo = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Regular),
            ForeColor = Color.Goldenrod,
            Text = "",
            Location = new Point(140, 90)
        };

        _lblMyAnimeId = new Label
        {
            AutoSize = true,
            ForeColor = Color.Gold,
            Text = "ID do MyAnime:",
            Location = new Point(1200, 90)
        };

        _txtMyAnimeId = new TextBox
        {
            ReadOnly = true,
            Width = 120,
            Location = new Point(1415, 90),
            Text = _myAnimeId.ToString(),
            BorderStyle = BorderStyle.FixedSingle
        };

        _btnSalvarEstrutura = new Button
        {
            Text = "💾 Salvar Estrutura em Disco",
            Width = 450,
            Height = 50,
            Location = new Point(700, 80)
        };

        _btnSalvarAnimesNoBanco = new Button
        {
            Text = "🗃️ Salvar todos os Animes no DB",
            Width = 450,
            Height = 50,
            Location = new Point(1600, 80)
        };

        pnlTopo.Controls.Add(_lblTitulo);
        pnlTopo.Controls.Add(_lblResumo);
        pnlTopo.Controls.Add(_lblMyAnimeId);
        pnlTopo.Controls.Add(_txtMyAnimeId);
        pnlTopo.Controls.Add(_btnSalvarEstrutura);
        pnlTopo.Controls.Add(_btnSalvarAnimesNoBanco);

        _flpCards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Black,
            Padding = new Padding(58, 36, 58, 36)
        };

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.DarkGray,
            Padding = new Padding(12, 0, 0, 0),
            Text = "—"
        };

        tlpMain.Controls.Add(pnlTopo, 0, 0);
        tlpMain.Controls.Add(_flpCards, 0, 1);
        tlpMain.Controls.Add(_lblStatus, 0, 2);

        Controls.Add(tlpMain);

        Load += async (_, _) => await CarregarDadosAsync();
        _btnSalvarEstrutura.Click += BtnSalvarEstrutura_Click;
        _btnSalvarAnimesNoBanco.Click += BtnSalvarAnimesNoBanco_Click;

        DoubleBuffered = true;
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    private async Task CarregarDadosAsync()
    {
        try
        {
            _lblStatus.Text = "⏳ Carregando detalhes do MyAnime...";
            _btnSalvarEstrutura.Enabled = false;
            _btnSalvarAnimesNoBanco.Enabled = false;

            _myAnimeAtual = await _apiMyAnimesService.ObterMyAnimePorIdAsync(_myAnimeId);
            if (_myAnimeAtual is null)
            {
                _lblStatus.Text = "❌ MyAnime não encontrado.";
                MessageBox.Show($"MyAnime ID {_myAnimeId} não encontrado na ApiMyAnimes.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _animesAtuais = await _apiMyAnimesService.ObterAnimesPorMyAnimeIdAsync(_myAnimeId);

            if (_animesAtuais.Count == 0 && _myAnimeAtual.AnimesMalId.Count > 0)
            {
                foreach (var malId in _myAnimeAtual.AnimesMalId.Distinct())
                {
                    var anime = await _apiMyAnimesService.ObterAnimePorMalIdAsync(malId);
                    if (anime is not null)
                        _animesAtuais.Add(anime);
                }
            }

            _animesAtuais = _animesAtuais
                .GroupBy(a => a.MalId)
                .Select(g => g.First())
                .OrderBy(a => a.Year ?? int.MaxValue)
                .ThenBy(a => a.Titulo)
                .ToList();

            _lblTitulo.Text = _myAnimeAtual.Titulo;
            _lblResumo.Text = $"Animes relacionados: {_animesAtuais.Count}";
            _txtMyAnimeId.Text = _myAnimeId.ToString();
            _txtMyAnimeId.SelectAll();
            try
            {
                Clipboard.SetText(_myAnimeId.ToString());
            }
            catch
            {
            }

            PopularCards();

            _lblStatus.Text = _animesAtuais.Count == 0
                ? "⚠️ Nenhum anime relacionado encontrado para esta coleção."
                : "✅ Coleção carregada.";

            _btnSalvarEstrutura.Enabled = _animesAtuais.Count > 0;
            _btnSalvarAnimesNoBanco.Enabled = _myAnimeAtual.AnimesMalId.Count > 0;
        }
        catch (HttpRequestException ex)
        {
            _lblStatus.Text = "❌ Erro de conexão com ApiMyAnimes.";
            MessageBox.Show(
                $"Falha ao consultar ApiMyAnimes em:\n{ApiMyAnimesService.ApiBase}\n\nDetalhes: {ex.Message}",
                "Erro de Conexão",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao carregar detalhes.";
            MessageBox.Show($"Erro ao carregar detalhes do MyAnime:\n\n{ex.Message}",
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public Task AtualizarAsync() => CarregarDadosAsync();

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
            card.CarregarDados(new JikanAnimeCard
            {
                MalId = anime.MalId,
                Title = anime.Titulo,
                TitleEnglish = anime.TitleEnglish,
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
            MessageBox.Show("Não há dados carregados para exportar.", "Aviso",
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
            MessageBox.Show(
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

            MessageBox.Show(mensagem, "Exportação concluída",
                MessageBoxButtons.OK,
                resultado.Erros.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao criar estrutura em disco.";
            MessageBox.Show($"Falha ao criar estrutura:\n\n{ex.Message}",
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSalvarEstrutura.Enabled = _animesAtuais.Count > 0;
        }
    }

    private async void BtnSalvarAnimesNoBanco_Click(object? sender, EventArgs e)
    {
        if (_myAnimeAtual is null)
        {
            MessageBox.Show("Carregue a coleção antes de salvar os animes.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var malIds = _myAnimeAtual.AnimesMalId.Distinct().ToList();
        if (malIds.Count == 0)
        {
            MessageBox.Show("Esta coleção não possui MalIds para importar.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnSalvarAnimesNoBanco.Enabled = false;
        _lblStatus.Text = "⏳ Salvando animes da coleção no banco local...";

        using var frmProgresso = new Frm_ProgressoOperacao("Salvando animes da coleção");
        frmProgresso.Atualizar(0, "Iniciando importação...");
        frmProgresso.Show(this);
        frmProgresso.BringToFront();

        try
        {
            var progresso = new Progress<ProgressoImportacaoAnimes>(p => frmProgresso.Atualizar(p.Percentual, p.Mensagem));

            var resultado = await _importadorAnimesMyAnimeService.ImportarAsync(
                _myAnimeId,
                _myAnimeAtual.Titulo,
                malIds,
                progresso);

            frmProgresso.Atualizar(100, "Importação concluída.");
            frmProgresso.Close();

            string? caminhoLog = null;
            if (resultado.ErrosDetalhados.Count > 0)
                caminhoLog = ImportadorAnimesMyAnimeService.SalvarLogErros("importacao-manual-myanime", resultado.ErrosDetalhados);

            var mensagem =
                $"Importação concluída para '{_myAnimeAtual.Titulo}'.\n\n" +
                $"Animes salvos: {resultado.AnimesSalvos}\n" +
                $"Animes salvos em modo degradação: {resultado.AnimesSalvosModoDegradacao}\n" +
                $"Animes ignorados: {resultado.AnimesIgnorados}\n" +
                $"Animes com falha: {resultado.AnimesComFalha}";

            if (!string.IsNullOrWhiteSpace(caminhoLog))
                mensagem += $"\n\nLog de erros salvo em:\n{caminhoLog}";

            MessageBox.Show(
                mensagem,
                "Salvar animes da coleção",
                MessageBoxButtons.OK,
                resultado.ErrosDetalhados.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            await CarregarDadosAsync();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "❌ Erro ao salvar animes da coleção.";
            MessageBox.Show($"Falha ao salvar animes no banco local:\n\n{ex.Message}",
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnSalvarAnimesNoBanco.Enabled = _myAnimeAtual?.AnimesMalId.Count > 0;
        }
    }
}
