using System.Net;
using WinAppDtudo.Controls;
using WinAppDtudo.Forms;
using WinAppDtudo.FormsUC;

namespace WinAppDtudo.Services;

/// <summary>
/// Orquestra as abas de MyAnimes dentro da janela principal: busca, detalhes (DB Local e ApiMyAnimeList),
/// edicao de Anime e MyAnime, detalhes de MyAnime, estrutura monitorada e a analise de pastas.
/// Todas as abas sao planas (um unico <see cref="TabWorkspace"/>) e reaproveitadas por chave.
/// </summary>
public sealed class MyAnimesTabController : IDisposable
{
    public const string BuscarAnimeKey = "BuscarAnime";

    private readonly Control _owner;
    private readonly TabWorkspace _workspace;
    private readonly WinAppAuthenticationService _authenticationService;
    private readonly ApiMyAnimesService _apiMyAnimesService;
    private readonly AnalizadorDeEstruturas _analizadorDeEstruturas = new();
    private readonly ImportadorAnimesMyAnimeService _importadorAnimesMyAnimeService;
    private readonly Action<string>? _statusReporter;
    private bool _analiseEmAndamento;
    private bool _disposed;

    public MyAnimesTabController(
        Control owner,
        TabWorkspace workspace,
        WinAppAuthenticationService authenticationService,
        Action<string>? statusReporter = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _statusReporter = statusReporter;
        _apiMyAnimesService = new ApiMyAnimesService(_authenticationService);
        _importadorAnimesMyAnimeService = new ImportadorAnimesMyAnimeService(
            _apiMyAnimesService,
            authenticationService: _authenticationService);
        _workspace.TabSelected += Workspace_TabSelected;
    }

    /// <summary>Indica se a analise de pastas esta em execucao (usado para desabilitar o menu).</summary>
    public bool AnaliseEmAndamento => _analiseEmAndamento;

    public event EventHandler? AnaliseEmAndamentoAlterada;

    public static string ChaveDetalhesAnime(int malId, bool consultaLocal)
        => $"{(consultaLocal ? "DB" : "MyAnimeList")} {malId}";

    public static string ChaveEditarAnime(int malId) => $"EditarAnime_{malId}";

    public static string ChaveMyAnime(int myAnimeId) => $"MyAnime_{myAnimeId}";

    public static string ChaveEditarMyAnime(int myAnimeId) => $"EditarMyAnime_{myAnimeId}";

    public static string ChaveEstrutura(int collectionId) => $"Monitoramento_{collectionId}";

    public void AbrirBuscarAnime()
    {
        try
        {
            _workspace.OpenOrActivate(BuscarAnimeKey, () =>
            {
                var tabPage = new TabPage
                {
                    Text = "Buscar Anime",
                    Name = BuscarAnimeKey,
                    ImageIndex = ObterIndiceIconeAnime(consultaLocal: true)
                };

                var ucBusca = new FUC_BuscarAnime(_apiMyAnimesService, _authenticationService)
                {
                    Dock = DockStyle.Fill
                };
                ucBusca.AnimeLocalSelecionado += AbrirDetalhesAnimeLocal;
                ucBusca.AnimeMyAnimeListSelecionado += AbrirDetalhesAnimeMyAnimeList;
                tabPage.Controls.Add(ucBusca);
                return tabPage;
            });
        }
        catch (Exception ex)
        {
            DarkMessageBox.Show(
                $"Erro ao abrir a aba Buscar Anime:\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    public void AbrirEstruturaMyAnime(int collectionId)
    {
        if (collectionId <= 0)
            return;

        var key = ChaveEstrutura(collectionId);
        _workspace.OpenOrActivate(key, () =>
        {
            var tabPage = new TabPage($"Estrutura My #{collectionId}") { Name = key };
            tabPage.Controls.Add(new FUC_Monitoramento(_authenticationService, collectionId, AbrirEstruturaMyAnime));
            return tabPage;
        });
    }

    /// <summary>
    /// Escolhe uma pasta, analisa as estruturas, cadastra os MyAnimes e, opcionalmente, importa os animes
    /// da colecao, reportando cada etapa em um dialogo de progresso. Ignora chamadas enquanto uma analise esta ativa.
    /// </summary>
    public async Task AnalizarPastasAsync()
    {
        if (_analiseEmAndamento || _disposed)
            return;

        using var folderDialog = new FolderBrowserDialog
        {
            Description = "Selecione a pasta raiz que contém os MyAnimes.",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (folderDialog.ShowDialog(_owner) != DialogResult.OK || string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            return;

        Frm_ProgressoOperacao? frmProgresso = null;
        List<MyAnimeCriadoInfo> myAnimesCriados = [];
        var errosDetalhados = new List<string>();

        try
        {
            DefinirAnaliseEmAndamento(true);
            _owner.UseWaitCursor = true;

            frmProgresso = new Frm_ProgressoOperacao("Analisando estruturas e salvando MyAnime");
            frmProgresso.Atualizar(0, "Iniciando análise...");
            frmProgresso.Show(_owner);
            frmProgresso.BringToFront();

            var progressoForm = frmProgresso;
            var progressoAnalise = new Progress<ProgressoAnalise>(p =>
            {
                var percentualEtapa = p.PercentualConcluido / 2;
                if (!progressoForm.IsDisposed)
                    progressoForm.Atualizar(percentualEtapa, p.Mensagem);
            });

            var analise = await Task.Run(() => _analizadorDeEstruturas.AnalisarDiretorio(folderDialog.SelectedPath, progressoAnalise));

            if (analise.MyAnimesParaPersistir.Count == 0)
            {
                FecharProgresso(ref frmProgresso);

                DarkMessageBox.Show(
                    analise.CriarResumo(),
                    "Análise concluída",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var resultadoCadastroMyAnime = await SalvarMyAnimesDaAnaliseComProgressoAsync(analise, frmProgresso, errosDetalhados);
            myAnimesCriados = resultadoCadastroMyAnime.Criados;

            FecharProgresso(ref frmProgresso);

            var resumoMyAnime =
                $"{analise.CriarResumo()}\n\n" +
                $"MyAnime criados: {myAnimesCriados.Count}\n" +
                $"MyAnime já existentes (ignorados): {resultadoCadastroMyAnime.Ignorados}\n" +
                $"Falhas no cadastro de MyAnime: {resultadoCadastroMyAnime.Falhas}\n\n" +
                "Deseja continuar e salvar agora os animes da coleção no banco local?";

            var desejaImportarAnimes = DarkMessageBox.Show(
                resumoMyAnime,
                "Cadastro de MyAnime concluído",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (desejaImportarAnimes == DialogResult.Yes && myAnimesCriados.Count > 0)
                await ImportarAnimesDosMyAnimesCriadosAsync(myAnimesCriados, errosDetalhados);

            if (myAnimesCriados.Count > 0 && !_disposed)
            {
                var ultimoMyAnimeCriado = myAnimesCriados[^1];
                var abrirDetalhes = DarkMessageBox.Show(
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
            DarkMessageBox.Show(
                $"Erro ao analisar as estruturas:\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            if (frmProgresso is not null && !frmProgresso.IsDisposed)
                FecharProgresso(ref frmProgresso);

            if (!_owner.IsDisposed)
                _owner.UseWaitCursor = false;

            DefinirAnaliseEmAndamento(false);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _workspace.TabSelected -= Workspace_TabSelected;
    }

    private void DefinirAnaliseEmAndamento(bool valor)
    {
        _analiseEmAndamento = valor;
        AnaliseEmAndamentoAlterada?.Invoke(this, EventArgs.Empty);
    }

    private static void FecharProgresso(ref Frm_ProgressoOperacao? frmProgresso)
    {
        if (frmProgresso is null)
            return;

        frmProgresso.Close();
        frmProgresso.Dispose();
        frmProgresso = null;
    }

    private void ReportarStatus(string mensagem) => _statusReporter?.Invoke(mensagem);

    private async void Workspace_TabSelected(object? sender, TabControlEventArgs e)
    {
        if (e.TabPage?.Controls.OfType<FUC_MyAnimeDetalhes>().FirstOrDefault() is not { } detalhes)
            return;

        try
        {
            await detalhes.AtualizarAsync();
        }
        catch (Exception ex)
        {
            ReportarStatus($"Não foi possível atualizar o MyAnime: {ex.Message}");
        }
    }

    private void AbrirDetalhesAnimeMyAnimeList(object? sender, int malId)
        => AbrirDetalhesAnime(malId, consultaLocal: false);

    private void AbrirDetalhesAnimeLocal(object? sender, int malId)
        => AbrirDetalhesAnime(malId, consultaLocal: true);

    private void AbrirDetalhesAnime(int malId, bool consultaLocal)
    {
        var tabName = ChaveDetalhesAnime(malId, consultaLocal);
        _workspace.OpenOrActivate(tabName, () =>
        {
            var ucDetalhes = new FUC_DetalhesAnime(
                malId,
                consultaLocal,
                _apiMyAnimesService,
                _authenticationService)
            {
                Dock = DockStyle.Fill
            };
            ucDetalhes.CardClicado += consultaLocal
                ? AbrirDetalhesAnimeLocal
                : AbrirDetalhesAnimeMyAnimeList;
            if (consultaLocal)
            {
                ucDetalhes.MyAnimeSolicitado += AbrirDetalhesMyAnime;
                ucDetalhes.EditarAnimeSolicitado += AbrirEditarAnimeLocal;
            }

            ucDetalhes.MyAnimeExistenteSelecionado += AbrirDetalhesMyAnime;
            ucDetalhes.MyAnimeAtualizado += (_, myAnimeId) =>
            {
                AbrirDetalhesMyAnime(this, myAnimeId);
                _ = AtualizarAbaMyAnimeAsync(myAnimeId);
            };
            ucDetalhes.StatusAtualizado += (_, mensagem) => ReportarStatus(mensagem);

            var tabPage = new AccentTabPage(DarkModeColors.GetAnimeDetailAccentColor(consultaLocal))
            {
                Text = $"#{malId}",
                HeaderText = $"#{malId}",
                Name = tabName,
                AccessibleName = $"Anime #{malId} - {(consultaLocal ? "DB Local" : "ApiMyAnimeList")}",
                ImageIndex = -1
            };
            tabPage.Controls.Add(ucDetalhes);
            return tabPage;
        });
    }

    private void AbrirEditarAnimeLocal(object? sender, int malId)
    {
        var tabName = ChaveEditarAnime(malId);
        _workspace.OpenOrActivate(tabName, () =>
        {
            var ucEditar = new FUC_EditarAnime(malId, _apiMyAnimesService)
            {
                Dock = DockStyle.Fill
            };
            ucEditar.AnimeSalvo += (_, args) =>
            {
                if (args.MyAnimeId > 0)
                    _ = AtualizarAbaMyAnimeAsync(args.MyAnimeId);
            };
            ucEditar.AnimeRemovido += (_, args) =>
            {
                _workspace.Close(tabName);
                _workspace.Close(ChaveDetalhesAnime(args.MalId, consultaLocal: true));
                if (args.MyAnimeId > 0)
                    _ = AtualizarAbaMyAnimeAsync(args.MyAnimeId);
            };

            var tabPage = new TabPage
            {
                Text = $"Editar #{malId}",
                Name = tabName,
                ImageIndex = ObterIndiceIconeAnime(consultaLocal: true)
            };
            tabPage.Controls.Add(ucEditar);
            return tabPage;
        });
    }

    private void AbrirEditarMyAnime(object? sender, int myAnimeId)
    {
        var tabName = ChaveEditarMyAnime(myAnimeId);
        _workspace.OpenOrActivate(tabName, () =>
        {
            var ucEditar = new FUC_EditarMyAnime(myAnimeId, _apiMyAnimesService)
            {
                Dock = DockStyle.Fill
            };
            ucEditar.MyAnimeSalvo += (_, args) => _ = AtualizarAbaMyAnimeAsync(args.MyAnimeId);
            ucEditar.MyAnimeRemovido += (_, args) =>
            {
                _workspace.Close(tabName);
                _workspace.Close(ChaveMyAnime(args.MyAnimeId));
            };

            var tabPage = new TabPage
            {
                Text = $"Editar My #{myAnimeId}",
                Name = tabName,
                ImageIndex = ObterIndiceIconeAnime(consultaLocal: true)
            };
            tabPage.Controls.Add(ucEditar);
            return tabPage;
        });
    }

    private void AbrirDetalhesMyAnime(object? sender, int myAnimeId)
    {
        var tabName = ChaveMyAnime(myAnimeId);
        _workspace.OpenOrActivate(tabName, () =>
        {
            var ucDetalhes = new FUC_MyAnimeDetalhes(myAnimeId, _apiMyAnimesService)
            {
                Dock = DockStyle.Fill
            };
            ucDetalhes.CardClicado += AbrirDetalhesAnimeLocal;
            ucDetalhes.EditarMyAnimeSolicitado += AbrirEditarMyAnime;
            ucDetalhes.MonitorarEstruturaSolicitado += (_, collectionId) => AbrirEstruturaMyAnime(collectionId);

            var tabPage = new TabPage
            {
                Text = $"My #{myAnimeId}",
                Name = tabName,
                ImageIndex = ObterIndiceIconeAnime(consultaLocal: true)
            };
            tabPage.Controls.Add(ucDetalhes);
            return tabPage;
        });
    }

    private async Task AtualizarAbaMyAnimeAsync(int myAnimeId)
    {
        if (_workspace.Find(ChaveMyAnime(myAnimeId))?.Controls.OfType<FUC_MyAnimeDetalhes>().FirstOrDefault() is { } detalhes)
            await detalhes.AtualizarAsync();
    }

    private int ObterIndiceIconeAnime(bool consultaLocal) => _workspace.GetImageIndex(
        consultaLocal ? "DBLocal" : "ApiMyAnimeList",
        consultaLocal ? Properties.Resources.Vvendetta : Properties.Resources.MyAnimeList_Logo);

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
                var colecao = await _apiMyAnimesService.GarantirMyAnimeColecaoAsync(myAnime);
                if (colecao.Id <= 0)
                {
                    errosDetalhados.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] MyAnime '{myAnime.Titulo}' não retornou ID após criação.");
                    resultado.Falhas++;
                }
                else
                {
                    if (!colecao.Created)
                        resultado.Ignorados++;

                    if (!resultado.Criados.Any(c => c.Id == colecao.Id))
                        resultado.Criados.Add(new MyAnimeCriadoInfo(colecao.Id, colecao.Titulo, myAnime.AnimesMalId));
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
        List<MyAnimeCriadoInfo> myAnimesCriados,
        List<string> errosDetalhados)
    {
        using var frmProgresso = new Frm_ProgressoOperacao("Salvando animes das coleções");
        frmProgresso.Atualizar(0, "Iniciando importação de animes...");
        frmProgresso.Show(_owner);
        frmProgresso.BringToFront();

        var totalAnimesSalvos = 0;
        var totalAnimesSalvosDegradacao = 0;
        var totalAnimesIgnorados = 0;
        var totalAnimesFalha = 0;

        for (var indiceMyAnime = 0; indiceMyAnime < myAnimesCriados.Count; indiceMyAnime++)
        {
            var item = myAnimesCriados[indiceMyAnime];
            var indiceAtual = indiceMyAnime;
            var progressoLocal = new Progress<ProgressoImportacaoAnimes>(p =>
            {
                var basePercent = (indiceAtual / (double)myAnimesCriados.Count) * 100d;
                var faixa = 100d / myAnimesCriados.Count;
                var percentualGlobal = (int)Math.Round(basePercent + (p.Percentual / 100d) * faixa, MidpointRounding.AwayFromZero);

                if (!frmProgresso.IsDisposed)
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

        if (errosDetalhados.Count > 0)
        {
            var caminhoLog = ImportadorAnimesMyAnimeService.SalvarLogErros("importacao-myanime", errosDetalhados);
            var detalhesFalha = string.Join(Environment.NewLine, errosDetalhados.Take(8));
            mensagem += $"\n\nPrimeiros erros:\n{detalhesFalha}";

            if (!string.IsNullOrWhiteSpace(caminhoLog))
                mensagem += $"\n\nLog salvo em:\n{caminhoLog}";
        }

        DarkMessageBox.Show(
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
