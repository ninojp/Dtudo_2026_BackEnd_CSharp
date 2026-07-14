using WinAppDtudo.Forms;
using WinAppDtudo.FormsUC;
using WinAppDtudo.Services;
using System.Net;

namespace WinAppDtudo;

public partial class Frm_MyAnimes : CustomFormNoBorder
{
    private const int CloseButtonSize = 34;
    private readonly ApiMyAnimesService _apiMyAnimesService = new();
    private readonly AnalizadorDeEstruturas _analizadorDeEstruturas = new();
    private readonly ImportadorAnimesMyAnimeService _importadorAnimesMyAnimeService = new();
    private AnaliseEstruturas? _ultimaAnaliseEstruturas;
    public int _tabIndexMascaras = 0;
    public int _tabIndexMyAnimesPorNome = 0;
    public int _tabIndexApiJikanPorNome = 0;
    public int _tabIndexApiMyAnimeListPorNome = 0;
    public Frm_MyAnimes()
    {
        InitializeComponent();
        MnI_ApiJikanBuscarNome.Click += MnI_ApiJikanBuscarNome_Click;
        MnI_ApiMyAnimeListBuscarNome.Click += MnI_ApiMyAnimeListBuscarNome_Click;
        Tbc_MyAnimes.Selected += Tbc_MyAnimes_Selected;
        // Aplicar o tema Dark Mode ao formulário e seus componentes
        ThemeManager.ApplyDarkModeToForm(this);
        // Inicializa o formulário customizado sem barra de título
        InitializeCustomFormNoBorder(Mnu_MenuMyAnimes);
        AddControlButtonsToMenuStrip(Mnu_MenuMyAnimes);
    }

    private void MnI_ApiMyAnimeListBuscarNome_Click(object sender, EventArgs e)
    {
        _tabIndexApiMyAnimeListPorNome++;
        try
        {
            var ucBuscaApiMyAnimeList = new FUC_ApiMyAnimeListBuscarNome
            {
                Dock = DockStyle.Fill
            };
            ucBuscaApiMyAnimeList.AnimeMyAnimeListSelecionado += AbrirDetalhesAnimeMyAnimeList;
            TabPage tabPage = new()
            {
                Text = $"{_tabIndexApiMyAnimeListPorNome} MAL",
                Name = $"ID_{_tabIndexApiMyAnimeListPorNome}",
                ImageIndex = 1,
            };
            tabPage.Controls.Add(ucBuscaApiMyAnimeList);
            Tbc_MyAnimes.TabPages.Add(tabPage);
            Tbc_MyAnimes.SelectedTab = tabPage;
        }
        catch (Exception ex)
        {
            _tabIndexApiMyAnimeListPorNome = Math.Max(0, _tabIndexApiMyAnimeListPorNome - 1);
            MessageBox.Show($"Erro ao abrir a aba ApiMyAnimeList:\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
    //=============================================================================
    private void MnI_ProcurarAnimePorNome_Click(object sender, EventArgs e)
    {
        _tabIndexMyAnimesPorNome++;
        try
        {
            var ucBuscaLocal = new FUC_DBLocalBuscarNome
            {
                Dock = DockStyle.Fill
            };

            ucBuscaLocal.MyAnimeSelecionado += AbrirDetalhesMyAnime;

            TabPage tabPage = new()
            {
                Text = $"{_tabIndexMyAnimesPorNome} DBLocal",
                Name = $"DBLocal_{_tabIndexMyAnimesPorNome}",
                ImageIndex = 2,
            };

            tabPage.Controls.Add(ucBuscaLocal);
            Tbc_MyAnimes.TabPages.Add(tabPage);
            Tbc_MyAnimes.SelectedTab = tabPage;
        }
        catch (Exception ex)
        {
            _tabIndexMyAnimesPorNome = Math.Max(0, _tabIndexMyAnimesPorNome - 1);
            MessageBox.Show($"Erro ao abrir a aba DBLocalBuscar:\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void MnI_ApiJikanBuscarNome_Click(object sender, EventArgs e)
    {
        _tabIndexApiJikanPorNome++;
        try
        {
            var ucBuscaApiJikan = new FUC_ApiJikanBuscarNome
            {
                Dock = DockStyle.Fill
            };

            ucBuscaApiJikan.AnimeJikanSelecionado += AbrirDetalhesAnimeJikan;

            TabPage tabPage = new()
            {
                Text = $"{_tabIndexApiJikanPorNome} ApiJikan",
                Name = $"ApiJikan_{_tabIndexApiJikanPorNome}",
                ImageIndex = 1,
            };

            tabPage.Controls.Add(ucBuscaApiJikan);
            Tbc_MyAnimes.TabPages.Add(tabPage);
            Tbc_MyAnimes.SelectedTab = tabPage;
        }
        catch (Exception ex)
        {
            _tabIndexApiJikanPorNome = Math.Max(0, _tabIndexApiJikanPorNome - 1);
            MessageBox.Show($"Erro ao abrir a aba ApiJikan:\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
    //=============================================================================
    private void MnI_AbaMascaras_Click(object sender, EventArgs e)
    {
        _tabIndexMascaras++;
        FUC_Mascaras ucMascaras = new()
        {
            Dock = DockStyle.Fill
        };
        TabPage tabPage = new()
        {
            Text = $"{_tabIndexMascaras} Máscaras",
            Name = $"{_tabIndexMascaras} Mascaras",
            ImageIndex = 3,
        };
        tabPage.Controls.Add(ucMascaras);
        Tbc_MyAnimes.TabPages.Add(tabPage);
    }
    //=============================================================================
    private void MnI_FecharAbaAtual_Click(object sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagaAbaAtual(Tbc_MyAnimes.SelectedTab);
        }
    }
    private void MnI_FecharTodasAbas_Click(object sender, EventArgs e)
    {
        //Na REMOÇÃO de itens de uma COLLECTION, devemos percorrer a COLLECTION de trás para frente, para evitar problemas de índice.
        for (int i = Tbc_MyAnimes.TabPages.Count - 1; i >= 0; i--)
        {
            ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
        }
    }
    private void MnI_FecharAbasADireita_Click(object sender, EventArgs e)
    {
        //remover abas a direita da aba selecionada.
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            int selectedIndex = Tbc_MyAnimes.SelectedIndex;
            for (int i = Tbc_MyAnimes.TabCount - 1; i > selectedIndex; i--)
            {
                ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
            }
        }
    }
    private void MnI_FecharAbasAEsquerda_Click(object sender, EventArgs e)
    {
        //remover abas a esquerda da aba selecionada
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            int selectedIndex = Tbc_MyAnimes.SelectedIndex;
            for (int i = selectedIndex - 1; i >= 0; i--)
            {
                ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
            }
        }
    }
    //=============================================================================
    //Item de menu para abrir o formulário Frm_Questao
    private void MnI_FormMsgBox_Click(object sender, EventArgs e)
    {
        Frm_Questao frmQuestao = new("pngwing.com", "Deseja continuar?");
        frmQuestao.ShowDialog();
        if (frmQuestao.DialogResult == DialogResult.Yes)
        {
            MessageBox.Show("Você clicou em Continuar!");
        }
        if (frmQuestao.DialogResult == DialogResult.Cancel)
        {
            MessageBox.Show("Você clicou em Parar!");
        }
    }
    //=============================================================================
    //Menu Flutuante - Captura o evento MouseDown do controle Tbc_MyAnimes e exibe um menu de contexto ao clicar com o botão direito do mouse.
    private void Tbc_MyAnimes_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            int tabIndex = GetTabIndexAt(e.Location);
            if (tabIndex >= 0)
            {
                Rectangle closeBounds = GetCloseButtonBounds(tabIndex);
                if (closeBounds.Contains(e.Location))
                {
                    ApagaAbaAtual(Tbc_MyAnimes.TabPages[tabIndex]);
                    return;
                }
            }
        }
        if (e.Button == MouseButtons.Right)
        {
            ContextMenuStrip contextMenu = new();
            ToolStripMenuItem menuFlutuanteItem1 = CriaMenuFlutuanteItem("ApagarAbaAtual", "BaixoGatilho");
            ToolStripMenuItem menuFlutuanteItem2 = CriaMenuFlutuanteItem("ApagarTodasAbas", "CimaGatilho");
            ToolStripMenuItem menuFlutuanteItem3 = CriaMenuFlutuanteItem("ApagarAbasDireita", "DireitaGatilho");
            ToolStripMenuItem menuFlutuanteItem4 = CriaMenuFlutuanteItem("ApagarAbasEsquerda", "EsquerdaGatilho");
            contextMenu.Items.Add(menuFlutuanteItem1);
            contextMenu.Items.Add(menuFlutuanteItem2);
            contextMenu.Items.Add(menuFlutuanteItem3);
            contextMenu.Items.Add(menuFlutuanteItem4);
            contextMenu.Show(this, new Point(e.X, e.Y));
            menuFlutuanteItem1.Click += new EventHandler(MenuFlutuanteItem1_Click);
            menuFlutuanteItem2.Click += new EventHandler(MenuFlutuanteItem2_Click);
            menuFlutuanteItem3.Click += new EventHandler(MenuFlutuanteItem3_Click);
            menuFlutuanteItem4.Click += new EventHandler(MenuFlutuanteItem4_Click);
        }
    }

    private void Tbc_MyAnimes_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Tbc_MyAnimes.TabPages.Count) return;

        TabPage tabPage = Tbc_MyAnimes.TabPages[e.Index];
        Rectangle tabRect = Tbc_MyAnimes.GetTabRect(e.Index);
        bool selecionada = e.Index == Tbc_MyAnimes.SelectedIndex;

        Color fundo = selecionada ? DarkModeColors.SelectionColor : DarkModeColors.BackgroundSecondaryColor;
        Color texto = selecionada ? Color.Black : DarkModeColors.TextColor;

        using (SolidBrush brush = new(fundo))
        { e.Graphics.FillRectangle(brush, tabRect); }

        Rectangle closeRect = GetCloseButtonBounds(e.Index);
        int espacamentoDireita = closeRect.Width + 6;
        Rectangle textRect = new(
            tabRect.X + 10,
            tabRect.Y + 4,
            Math.Max(10, tabRect.Width - espacamentoDireita - 6),
            tabRect.Height - 8);

        TextRenderer.DrawText(
            e.Graphics,
            tabPage.Text,
            Tbc_MyAnimes.Font,
            textRect,
            texto,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        using (Font closeFont = new("Segoe UI", 10F, FontStyle.Bold))
        {
            TextRenderer.DrawText(
                e.Graphics,
                "x",
                closeFont,
                closeRect,
                texto,
                TextFormatFlags.Top | TextFormatFlags.Right);
        }
        using Pen borderPen = new(DarkModeColors.BorderColor);
        e.Graphics.DrawRectangle(borderPen, tabRect);
    }

    private int GetTabIndexAt(Point location)
    {
        for (int i = 0; i < Tbc_MyAnimes.TabCount; i++)
        {
            if (Tbc_MyAnimes.GetTabRect(i).Contains(location))
                return i;
        }
        return -1;
    }

    private Rectangle GetCloseButtonBounds(int tabIndex)
    {
        Rectangle tabRect = Tbc_MyAnimes.GetTabRect(tabIndex);
        return new Rectangle(
            tabRect.Right - CloseButtonSize - 16,
            tabRect.Top + (tabRect.Height - CloseButtonSize) / 2,
            CloseButtonSize,
            CloseButtonSize);
    }

    private static ToolStripMenuItem CriaMenuFlutuanteItem(string textMenuItem, string imageName)
    {
        ToolStripMenuItem menuFlutuanteItem = new(textMenuItem);
        if (Properties.Resources.ResourceManager.GetObject(imageName) is Image imgMenuItem)
        { menuFlutuanteItem.Image = imgMenuItem; }
        return menuFlutuanteItem;
    }
    void MenuFlutuanteItem1_Click(object? sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagaAbaAtual(Tbc_MyAnimes.SelectedTab);
        }
    }
    void MenuFlutuanteItem2_Click(object? sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagarDireita(Tbc_MyAnimes.SelectedIndex);
            ApagarEsquerda(Tbc_MyAnimes.SelectedIndex);
        }
    }
    void MenuFlutuanteItem3_Click(object? sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagarDireita(Tbc_MyAnimes.SelectedIndex);
        }
    }
    void MenuFlutuanteItem4_Click(object? sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagarEsquerda(Tbc_MyAnimes.SelectedIndex);
        }
    }
    void ApagarDireita(int itemSelecionado)
    {
        for (int i = Tbc_MyAnimes.TabCount - 1; i > itemSelecionado; i--)
        {
            ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
        }
    }
    void ApagarEsquerda(int itemSelecionado)
    {
        for (int i = itemSelecionado - 1; i >= 0; i--)
        {
            //RemoveAt remove a aba no índice especificado.
            ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
        }
    }
    //=============================================================================
    void ApagaAbaAtual(TabPage tabPage)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            Tbc_MyAnimes.TabPages.Remove(tabPage);
        }
    }

    //=============================================================================
    private void AbrirDetalhesAnimeMyAnimeList(object? sender, int malId)
        => AbrirDetalhesAnime(malId, usarJikan: false);

    private void AbrirDetalhesAnimeJikan(object? sender, int malId)
        => AbrirDetalhesAnime(malId, usarJikan: true);

    private void AbrirDetalhesAnime(int malId, bool usarJikan)
    {
        var origem = usarJikan ? "Jikan" : "MyAnimeList";
        var tabName = $"{origem} {malId}";
        var tabExistente = Tbc_MyAnimes.TabPages
            .Cast<TabPage>().FirstOrDefault(tp => tp.Name == tabName);
        if (tabExistente != null)
        {
            Tbc_MyAnimes.SelectedTab = tabExistente;
            return;
        }

        var ucDetalhes = new FUC_DetalhesAnime(malId, usarJikan)
        {
            Dock = DockStyle.Fill
        };
        ucDetalhes.CardClicado += usarJikan ? AbrirDetalhesAnimeJikan : AbrirDetalhesAnimeMyAnimeList;
        ucDetalhes.MyAnimeAtualizado += (_, myAnimeId) =>
        {
            AbrirDetalhesMyAnime(this, myAnimeId);
            _ = AtualizarAbaMyAnimeAsync(myAnimeId);
        };
        var tabPage = new TabPage
        {
            Text = $" #{malId}",
            Name = tabName,
            ImageIndex = 1,
        };
        tabPage.Controls.Add(ucDetalhes);
        Tbc_MyAnimes.TabPages.Add(tabPage);
        Tbc_MyAnimes.SelectedTab = tabPage;
    }

    private async Task AtualizarAbaMyAnimeAsync(int myAnimeId)
    {
        var tabName = $"MyAnime_{myAnimeId}";
        var tab = Tbc_MyAnimes.TabPages.Cast<TabPage>().FirstOrDefault(tp => tp.Name == tabName);
        if (tab?.Controls.OfType<FUC_MyAnimeDetalhes>().FirstOrDefault() is FUC_MyAnimeDetalhes detalhes)
            await detalhes.AtualizarAsync();
    }

    private void AbrirDetalhesMyAnime(object? sender, int myAnimeId)
    {
        var tabName = $"MyAnime_{myAnimeId}";
        var tabExistente = Tbc_MyAnimes.TabPages
            .Cast<TabPage>().FirstOrDefault(tp => tp.Name == tabName);
        if (tabExistente != null)
        {
            Tbc_MyAnimes.SelectedTab = tabExistente;
            return;
        }

        var ucDetalhes = new FUC_MyAnimeDetalhes(myAnimeId)
        {
            Dock = DockStyle.Fill
        };
        ucDetalhes.CardClicado += AbrirDetalhesAnimeMyAnimeList;

        var tabPage = new TabPage
        {
            Text = $"My #{myAnimeId}",
            Name = tabName,
            ImageIndex = 1
        };

        tabPage.Controls.Add(ucDetalhes);
        Tbc_MyAnimes.TabPages.Add(tabPage);
        Tbc_MyAnimes.SelectedTab = tabPage;
    }

    private void Frm_MyAnimes_Load(object sender, EventArgs e)
    {

    }

    private async void Tbc_MyAnimes_Selected(object? sender, TabControlEventArgs e)
    {
        if (e.TabPage?.Controls.OfType<FUC_MyAnimeDetalhes>().FirstOrDefault() is FUC_MyAnimeDetalhes detalhes)
            await detalhes.AtualizarAsync();
    }

    private async void Mnu_AnalizarEstruturas_Click(object sender, EventArgs e)
    {
        using var folderDialog = new FolderBrowserDialog
        {
            Description = "Selecione a pasta raiz que contém os MyAnimes.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (folderDialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            return;

        Frm_ProgressoOperacao? frmProgresso = null;
        List<MyAnimeCriadoInfo> myAnimesCriados = [];
        var errosDetalhados = new List<string>();

        try
        {
            UseWaitCursor = true;
            Mnu_AnalizarEstruturas.Enabled = false;

            frmProgresso = new Frm_ProgressoOperacao("Analisando estruturas e salvando MyAnime");
            frmProgresso.Atualizar(0, "Iniciando análise...");
            frmProgresso.Show(this);
            frmProgresso.BringToFront();

            var progressoAnalise = new Progress<ProgressoAnalise>(p =>
            {
                var percentualEtapa = p.PercentualConcluido / 2;
                frmProgresso.Atualizar(percentualEtapa, p.Mensagem);
            });

            var analise = await Task.Run(() => _analizadorDeEstruturas.AnalisarDiretorio(folderDialog.SelectedPath, progressoAnalise));
            _ultimaAnaliseEstruturas = analise;

            if (analise.MyAnimesParaPersistir.Count == 0)
            {
                frmProgresso.Close();
                frmProgresso.Dispose();
                frmProgresso = null;

                MessageBox.Show(
                    analise.CriarResumo(),
                    "Análise concluída",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var resultadoCadastroMyAnime = await SalvarMyAnimesDaAnaliseComProgressoAsync(analise, frmProgresso, errosDetalhados);
            myAnimesCriados = resultadoCadastroMyAnime.Criados;

            frmProgresso.Close();
            frmProgresso.Dispose();
            frmProgresso = null;

            var resumoMyAnime =
                $"{analise.CriarResumo()}\n\n" +
                $"MyAnime criados: {myAnimesCriados.Count}\n" +
                $"MyAnime já existentes (ignorados): {resultadoCadastroMyAnime.Ignorados}\n" +
                $"Falhas no cadastro de MyAnime: {resultadoCadastroMyAnime.Falhas}\n\n" +
                "Deseja continuar e salvar agora os animes da coleção no banco local?";

            var desejaImportarAnimes = MessageBox.Show(
                resumoMyAnime,
                "Cadastro de MyAnime concluído",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (desejaImportarAnimes == DialogResult.Yes && myAnimesCriados.Count > 0)
                await ImportarAnimesDosMyAnimesCriadosAsync(analise.DiretorioRaiz, myAnimesCriados, errosDetalhados);

            if (myAnimesCriados.Count > 0)
            {
                var ultimoMyAnimeCriado = myAnimesCriados.Last();
                var abrirDetalhes = MessageBox.Show(
                    $"Processamento finalizado.\n\nDeseja abrir MyAnimeDetalhes da coleção recém criada: '{ultimoMyAnimeCriado.Titulo}' (ID {ultimoMyAnimeCriado.Id})?",
                    "Abrir detalhes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (abrirDetalhes == DialogResult.Yes)
                    AbrirDetalhesMyAnime(this, ultimoMyAnimeCriado.Id);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao analisar as estruturas:\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            if (frmProgresso is not null && !frmProgresso.IsDisposed)
            {
                frmProgresso.Close();
                frmProgresso.Dispose();
            }

            UseWaitCursor = false;
            Mnu_AnalizarEstruturas.Enabled = true;
        }
    }

    private async Task<ResultadoCadastroMyAnime> SalvarMyAnimesDaAnaliseComProgressoAsync(
        AnaliseEstruturas analise,
        Frm_ProgressoOperacao frmProgresso,
        List<string> errosDetalhados)
    {
        var resultado = new ResultadoCadastroMyAnime();
        var total = analise.MyAnimesParaPersistir.Count;
        if (total == 0)
            return resultado;

        for (var indice = 0; indice < total; indice++)
        {
            var myAnime = analise.MyAnimesParaPersistir[indice];
            try
            {
                var myAnimeId = await _apiMyAnimesService.AdicionarMyAnimeAsync(myAnime);
                if (!myAnimeId.HasValue || myAnimeId.Value <= 0)
                {
                    errosDetalhados.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MyAnime '{myAnime.Titulo}' não retornou ID após criação.");
                    resultado.Falhas++;
                }
                else
                {
                    resultado.Criados.Add(new MyAnimeCriadoInfo(myAnimeId.Value, myAnime.Titulo, myAnime.AnimesMalId));
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                resultado.Ignorados++;

                var myAnimeExistente = await _apiMyAnimesService.ObterMyAnimePorTituloAsync(myAnime.Titulo);
                if (myAnimeExistente is not null
                    && myAnimeExistente.Id > 0
                    && !resultado.Criados.Any(c => c.Id == myAnimeExistente.Id))
                {
                    resultado.Criados.Add(new MyAnimeCriadoInfo(myAnimeExistente.Id, myAnimeExistente.Titulo, myAnime.AnimesMalId));
                }
            }
            catch (Exception ex)
            {
                resultado.Falhas++;
                errosDetalhados.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Falha ao criar MyAnime '{myAnime.Titulo}': {ex.Message}");
            }

            var percentual = 50 + (int)Math.Round(((indice + 1) / (double)total) * 50, MidpointRounding.AwayFromZero);
            frmProgresso.Atualizar(Math.Clamp(percentual, 50, 100), $"Salvando MyAnime {indice + 1}/{total}: {myAnime.Titulo}");
        }

        return resultado;
    }

    private async Task ImportarAnimesDosMyAnimesCriadosAsync(
        string diretorioRaizAnalise,
        List<MyAnimeCriadoInfo> myAnimesCriados,
        List<string> errosDetalhados)
    {
        using var frmProgresso = new Frm_ProgressoOperacao("Salvando animes das coleções");
        frmProgresso.Atualizar(0, "Iniciando importação de animes...");
        frmProgresso.Show(this);
        frmProgresso.BringToFront();

        var totalAnimesSalvos = 0;
        var totalAnimesSalvosDegradacao = 0;
        var totalAnimesIgnorados = 0;
        var totalAnimesFalha = 0;

        for (var indiceMyAnime = 0; indiceMyAnime < myAnimesCriados.Count; indiceMyAnime++)
        {
            var item = myAnimesCriados[indiceMyAnime];
            var progressoLocal = new Progress<ProgressoImportacaoAnimes>(p =>
            {
                var basePercent = (indiceMyAnime / (double)myAnimesCriados.Count) * 100d;
                var faixa = 100d / myAnimesCriados.Count;
                var percentualGlobal = (int)Math.Round(basePercent + (p.Percentual / 100d) * faixa, MidpointRounding.AwayFromZero);

                frmProgresso.Atualizar(Math.Clamp(percentualGlobal, 0, 100), p.Mensagem);
            });

            var resultado = await _importadorAnimesMyAnimeService.ImportarAsync(
                item.Id,
                item.Titulo,
                item.AnimesMalId,
                progressoLocal);

            totalAnimesSalvos += resultado.AnimesSalvos;
            totalAnimesSalvosDegradacao += resultado.AnimesSalvosModoDegradacao;
            totalAnimesIgnorados += resultado.AnimesIgnorados;
            totalAnimesFalha += resultado.AnimesComFalha;
            errosDetalhados.AddRange(resultado.ErrosDetalhados);
        }

        frmProgresso.Atualizar(100, "Importação finalizada.");
        frmProgresso.Close();

        var mensagem =
            $"Salvamento dos animes finalizado.\n" +
            $"Animes salvos: {totalAnimesSalvos}\n" +
            $"Animes salvos em modo degradação: {totalAnimesSalvosDegradacao}\n" +
            $"Animes ignorados (já existentes): {totalAnimesIgnorados}\n" +
            $"Animes com falha: {totalAnimesFalha}\n" +
            $"Erros detalhados: {errosDetalhados.Count}";

        string? caminhoLog = null;
        if (errosDetalhados.Count > 0)
        {
            caminhoLog = ImportadorAnimesMyAnimeService.SalvarLogErros("importacao-myanime", errosDetalhados);
            var detalhesFalha = string.Join(Environment.NewLine, errosDetalhados.Take(8));
            mensagem += $"\n\nPrimeiros erros:\n{detalhesFalha}";

            if (!string.IsNullOrWhiteSpace(caminhoLog))
                mensagem += $"\n\nLog salvo em:\n{caminhoLog}";
        }

        MessageBox.Show(
            mensagem,
            "Importação MyAnime",
            MessageBoxButtons.OK,
            errosDetalhados.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private sealed record MyAnimeCriadoInfo(int Id, string Titulo, List<int> AnimesMalId);

    private sealed class ResultadoCadastroMyAnime
    {
        public List<MyAnimeCriadoInfo> Criados { get; } = [];
        public int Ignorados { get; set; }
        public int Falhas { get; set; }
    }
}
