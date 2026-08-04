using System.Net;
using LibDtudo.Shared.Dtos;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public sealed class FUC_EditarMyAnime : UserControl
{
    public event EventHandler<MyAnimeEditSavedEventArgs>? MyAnimeSalvo;
    public event EventHandler<MyAnimeRemovedEventArgs>? MyAnimeRemovido;

    private readonly ApiMyAnimesService _apiMyAnimesService = new();
    private readonly MyAnimeEditFieldSet _fields = new();
    private readonly int _myAnimeId;

    private Label _lblTitulo = null!;
    private Label _lblStatus = null!;
    private Label _lblResumo = null!;
    private Panel _pnlEditor = null!;
    private Button _btnSalvar = null!;
    private Button _btnRecarregar = null!;
    private Button _btnRemover = null!;

    private ObterMyAnimeDto? _myAnimeAtual;
    private bool _hasChanges;

    public FUC_EditarMyAnime(int myAnimeId)
    {
        _myAnimeId = myAnimeId;
        InitializeLayout();
        _fields.Changed += (_, _) => SetDirty(true);
        Load += async (_, _) => await CarregarAsync(confirmarPerdaAlteracoes: false);
        ThemeManager.ApplyDarkModeToUserControl(this);
        AplicarEstiloBotaoRemover(_btnRemover);
    }

    private void InitializeLayout()
    {
        BackColor = DarkModeColors.BackgroundColor;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 128,
            Padding = new Padding(36, 10, 24, 8),
            BackColor = Color.FromArgb(25, 30, 80)
        };

        _lblTitulo = new Label
        {
            Dock = DockStyle.Top,
            Height = 54,
            AutoEllipsis = true,
            Font = new Font("Segoe UI Black", 17F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = header.BackColor,
            Text = $"Editar MyAnime #{_myAnimeId}",
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.Gold,
            BackColor = header.BackColor,
            Text = "Carregando colecao local...",
            TextAlign = ContentAlignment.MiddleLeft
        };

        header.Controls.Add(_lblStatus);
        header.Controls.Add(_lblTitulo);

        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 360,
            Padding = new Padding(18),
            BackColor = DarkModeColors.BackgroundSecondaryColor
        };

        _lblResumo = new Label
        {
            Dock = DockStyle.Top,
            Height = 180,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.Gold,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            TextAlign = ContentAlignment.TopLeft
        };

        _btnSalvar = CreateButton("Salvar MyAnime", 52);
        _btnSalvar.Click += async (_, _) => await SalvarAsync();

        _btnRecarregar = CreateButton("Recarregar", 46);
        _btnRecarregar.Click += async (_, _) => await CarregarAsync(confirmarPerdaAlteracoes: true);

        _btnRemover = CreateButton("Remover MyAnime", 46);
        _btnRemover.Click += async (_, _) => await RemoverAsync();

        var actionsPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 168,
            BackColor = DarkModeColors.BackgroundSecondaryColor
        };

        actionsPanel.Controls.Add(_btnRemover);
        actionsPanel.Controls.Add(_btnRecarregar);
        actionsPanel.Controls.Add(_btnSalvar);
        actionsPanel.Resize += (_, _) => OrganizarBotoes(actionsPanel);
        leftPanel.Controls.Add(actionsPanel);
        leftPanel.Controls.Add(_lblResumo);

        _pnlEditor = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8),
            BackColor = DarkModeColors.BackgroundSecondaryColor
        };

        Controls.Add(_pnlEditor);
        Controls.Add(leftPanel);
        Controls.Add(header);
    }

    private async Task CarregarAsync(bool confirmarPerdaAlteracoes)
    {
        if (confirmarPerdaAlteracoes && _hasChanges && !ConfirmarPerdaAlteracoes())
            return;

        SetBusy(true, "Carregando colecao local...");
        try
        {
            var myAnime = await _apiMyAnimesService.ObterMyAnimePorIdAsync(_myAnimeId);
            if (myAnime is null)
            {
                _lblStatus.Text = $"MyAnime ID {_myAnimeId} nao encontrado no DB_Local.";
                _lblResumo.Text = "Registro indisponivel.";
                _pnlEditor.Controls.Clear();
                WinAppDtudo.Services.DarkMessageBox.Show(_lblStatus.Text, "MyAnime nao encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _myAnimeAtual = myAnime;
            _lblTitulo.Text = $"Editar MyAnime #{myAnime.Id} - {myAnime.Titulo}";
            PopularResumo(myAnime);
            _fields.Build(_pnlEditor, myAnime);
            SetDirty(false);
            _lblStatus.Text = $"Colecao carregada em {DateTime.Now:HH:mm:ss}.";
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
            _lblStatus.Text = "Erro ao carregar MyAnime.";
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao carregar MyAnime local:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task SalvarAsync()
    {
        if (_myAnimeAtual is null)
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Carregue o MyAnime antes de salvar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var errors = new List<string>();
        if (!_fields.TryCreateDto(errors, out var dto, out var parseResult))
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                string.Join(Environment.NewLine, errors.Distinct()),
                "Revise os campos",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (parseResult.DuplicateMalIds.Count > 0)
        {
            var confirmacao = WinAppDtudo.Services.DarkMessageBox.Show(
                $"Foram encontrados {parseResult.DuplicateMalIds.Count} MalIds duplicados. Eles serao removidos ao salvar.\n\nDeseja continuar?",
                "MalIds duplicados",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirmacao != DialogResult.Yes)
                return;
        }

        SetBusy(true, "Salvando MyAnime...");
        try
        {
            await _apiMyAnimesService.AtualizarMyAnimeAsync(_myAnimeAtual.Id, dto);
            var salvo = await _apiMyAnimesService.ObterMyAnimePorIdAsync(_myAnimeAtual.Id);
            _myAnimeAtual = salvo ?? new ObterMyAnimeDto
            {
                Id = _myAnimeAtual.Id,
                Titulo = dto.Titulo,
                AnimesMalId = dto.AnimesMalId
            };

            _fields.Build(_pnlEditor, _myAnimeAtual);
            _lblTitulo.Text = $"Editar MyAnime #{_myAnimeAtual.Id} - {_myAnimeAtual.Titulo}";
            PopularResumo(_myAnimeAtual);
            SetDirty(false);
            _lblStatus.Text = $"Alteracoes salvas em {DateTime.Now:HH:mm:ss}.";

            WinAppDtudo.Services.DarkMessageBox.Show("MyAnime atualizado com sucesso no DB_Local.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            MyAnimeSalvo?.Invoke(this, new MyAnimeEditSavedEventArgs(_myAnimeAtual.Id, _myAnimeAtual.Titulo));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            WinAppDtudo.Services.DarkMessageBox.Show($"MyAnime ID {_myAnimeAtual.Id} nao encontrado para atualizacao.", "Nao encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Ja existe outro MyAnime com o titulo '{dto.Titulo}'. Escolha um titulo diferente.",
                "Titulo ja cadastrado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao salvar MyAnime:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RemoverAsync()
    {
        if (_myAnimeAtual is null)
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Carregue o MyAnime antes de removê-lo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirmacao = WinAppDtudo.Services.DarkMessageBox.Show(
            $"O MyAnime '{_myAnimeAtual.Titulo}' será removido permanentemente do DB_Local.\n\nDeseja continuar?",
            "Remover MyAnime",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirmacao != DialogResult.Yes)
            return;

        var myAnimeId = _myAnimeAtual.Id;
        var titulo = _myAnimeAtual.Titulo;
        SetBusy(true, "Removendo MyAnime...");
        try
        {
            await _apiMyAnimesService.RemoverMyAnimeAsync(myAnimeId);
            _myAnimeAtual = null;
            SetDirty(false);
            _lblStatus.Text = "MyAnime removido do DB_Local.";
            WinAppDtudo.Services.DarkMessageBox.Show("MyAnime removido com sucesso do DB_Local.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            MyAnimeRemovido?.Invoke(this, new MyAnimeRemovedEventArgs(myAnimeId, titulo));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            WinAppDtudo.Services.DarkMessageBox.Show($"MyAnime ID {myAnimeId} não foi encontrado no DB_Local.", "Não encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (HttpRequestException ex)
        {
            WinAppDtudo.Services.DarkMessageBox.Show(
                $"Falha ao remover na ApiMyAnimes em:\n{ApiMyAnimesService.ApiBase}\n\nDetalhes: {ex.Message}",
                "Erro de Conexão",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            WinAppDtudo.Services.DarkMessageBox.Show($"Erro ao remover MyAnime:\n\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void PopularResumo(ObterMyAnimeDto myAnime)
    {
        var idsUnicos = myAnime.AnimesMalId.Distinct().Count();
        _lblResumo.Text =
            $"ID: {myAnime.Id}{Environment.NewLine}" +
            $"Titulo: {myAnime.Titulo}{Environment.NewLine}" +
            $"MalIds: {myAnime.AnimesMalId.Count}{Environment.NewLine}" +
            $"MalIds unicos: {idsUnicos}{Environment.NewLine}" +
            $"Ultima leitura: {myAnime.HoraDaConsulta:dd/MM/yyyy HH:mm:ss}";
    }

    private void SetDirty(bool dirty)
    {
        _hasChanges = dirty;
        if (_myAnimeAtual is null)
            return;

        _lblStatus.Text = dirty
            ? "Alteracoes pendentes."
            : _lblStatus.Text;
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _btnSalvar.Enabled = !busy && _myAnimeAtual is not null;
        _btnRecarregar.Enabled = !busy;
        _btnRemover.Enabled = !busy && _myAnimeAtual is not null;
        UseWaitCursor = busy;
        if (!string.IsNullOrWhiteSpace(status))
            _lblStatus.Text = status;
    }

    private bool ConfirmarPerdaAlteracoes()
    {
        var resposta = WinAppDtudo.Services.DarkMessageBox.Show(
            "Existem alteracoes nao salvas. Recarregar descartara essas alteracoes.\n\nDeseja recarregar mesmo assim?",
            "Descartar alteracoes",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        return resposta == DialogResult.Yes;
    }

    private static Button CreateButton(string text, int height)
    {
        return new Button
        {
            Text = text,
            Height = height,
            FlatStyle = FlatStyle.Flat,
            BackColor = DarkModeColors.AccentColor,
            ForeColor = DarkModeColors.TextColor,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
    }

    private void OrganizarBotoes(Panel panel)
    {
        const int buttonWidth = 300;
        var left = Math.Max(0, (panel.ClientSize.Width - buttonWidth) / 2);

        var larguraBotao = Math.Min(buttonWidth, panel.ClientSize.Width);
        _btnSalvar.Width = larguraBotao;
        _btnRecarregar.Width = larguraBotao;
        _btnRemover.Width = larguraBotao;
        _btnSalvar.Location = new Point(left, 0);
        _btnRecarregar.Location = new Point(left, _btnSalvar.Bottom + 12);
        _btnRemover.Location = new Point(left, _btnRecarregar.Bottom + 12);
    }

    private static void AplicarEstiloBotaoRemover(Button button)
    {
        button.BackColor = Color.Red;
        button.ForeColor = Color.White;
        button.FlatAppearance.MouseOverBackColor = Color.DarkRed;
        button.FlatAppearance.MouseDownBackColor = Color.Maroon;
    }
}

public sealed class MyAnimeEditSavedEventArgs(int myAnimeId, string titulo) : EventArgs
{
    public int MyAnimeId { get; } = myAnimeId;
    public string Titulo { get; } = titulo;
}

public sealed class MyAnimeRemovedEventArgs(int myAnimeId, string titulo) : EventArgs
{
    public int MyAnimeId { get; } = myAnimeId;
    public string Titulo { get; } = titulo;
}
