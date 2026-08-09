using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public sealed class Frm_IdentityAdministration : Form
{
    private readonly IdentityAdministrationApiClient _client;
    private readonly WinAppAuthenticationService _authenticationService;
    private readonly TextBox _userNameInput = new();
    private readonly TextBox _emailInput = new();
    private readonly TextBox _passwordInput = new();
    private readonly ComboBox _roleInput = new();
    private readonly DataGridView _accountsGrid = CreateGrid();
    private readonly DataGridView _rolesGrid = CreateGrid();
    private readonly DataGridView _permissionsGrid = CreateGrid();
    private readonly DataGridView _devicesGrid = CreateGrid();
    private readonly DataGridView _sessionsGrid = CreateGrid();
    private readonly Label _statusLabel = new();
    private readonly CancellationTokenSource _closingCancellation = new();
    private bool _isLoading;

    public Frm_IdentityAdministration(WinAppAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _client = new IdentityAdministrationApiClient(authenticationService);

        Text = "Administracao do Identity";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1280, 780);
        MinimumSize = new Size(960, 620);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildLayout();
        ThemeManager.ApplyDarkModeToForm(this);
        Load += async (_, _) => await LoadDataAsync();
        FormClosed += (_, _) => _closingCancellation.Cancel();
    }

    private void BuildLayout()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            Padding = new Padding(18, 12, 18, 10)
        };
        var title = new Label
        {
            AutoSize = true,
            Text = "Administracao segura do Identity",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Location = new Point(18, 12)
        };
        var refresh = new Button
        {
            Text = "Atualizar",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(1080, 12),
            Height = 42
        };
        refresh.Click += async (_, _) => await LoadDataAsync();
        header.Resize += (_, _) => refresh.Left = header.ClientSize.Width - refresh.Width;
        header.Controls.Add(title);
        header.Controls.Add(refresh);

        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 32;
        _statusLabel.Padding = new Padding(18, 5, 18, 4);
        _statusLabel.Text = "Pronto.";

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 8) };
        tabs.TabPages.Add(BuildAccountsTab());
        tabs.TabPages.Add(BuildRolesTab());
        tabs.TabPages.Add(BuildDevicesTab());
        tabs.TabPages.Add(BuildSessionsTab());

        Controls.Add(tabs);
        Controls.Add(_statusLabel);
        Controls.Add(header);
    }

    private TabPage BuildAccountsTab()
    {
        var tab = new TabPage("Contas");
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var provision = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 4, 0, 4)
        };
        ConfigureInput(_userNameInput, "Usuario", 190);
        ConfigureInput(_emailInput, "Email", 250);
        ConfigureInput(_passwordInput, "Senha", 250);
        _passwordInput.UseSystemPasswordChar = true;
        _roleInput.DropDownStyle = ComboBoxStyle.DropDownList;
        _roleInput.Width = 220;
        _roleInput.Height = 34;
        var provisionButton = new Button { Text = "Provisionar conta", AutoSize = true, Height = 38 };
        provisionButton.Click += async (_, _) => await ProvisionAccountAsync();
        provision.Controls.Add(CreateInputGroup("Usuario", _userNameInput));
        provision.Controls.Add(CreateInputGroup("Email", _emailInput));
        provision.Controls.Add(CreateInputGroup("Senha", _passwordInput));
        provision.Controls.Add(CreateInputGroup("Role", _roleInput));
        provision.Controls.Add(provisionButton);

        var accountActions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        var lockButton = new Button { Text = "Bloquear/desbloquear selecionada", AutoSize = true, Height = 36 };
        lockButton.Click += async (_, _) => await ToggleSelectedAccountAsync();
        var assignButton = new Button { Text = "Atribuir role selecionada", AutoSize = true, Height = 36 };
        assignButton.Click += async (_, _) => await AssignSelectedRoleAsync(true);
        var removeButton = new Button { Text = "Remover role selecionada", AutoSize = true, Height = 36 };
        removeButton.Click += async (_, _) => await AssignSelectedRoleAsync(false);
        accountActions.Controls.Add(lockButton);
        accountActions.Controls.Add(assignButton);
        accountActions.Controls.Add(removeButton);

        ConfigureGrid(
            _accountsGrid,
            ("Usuario", 180),
            ("Email", 240),
            ("Ativado", 90),
            ("Bloqueado", 100),
            ("Roles", 360));

        content.Controls.Add(provision, 0, 0);
        content.Controls.Add(accountActions, 0, 1);
        content.Controls.Add(_accountsGrid, 0, 2);
        tab.Controls.Add(content);
        return tab;
    }

    private TabPage BuildRolesTab()
    {
        var tab = new TabPage("Roles e permissoes");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 };
        ConfigureGrid(_rolesGrid, ("Role", 240), ("Permissoes", 700));
        ConfigureGrid(_permissionsGrid, ("Permissao", 260), ("Descricao", 800));
        split.Panel1.Controls.Add(_rolesGrid);
        split.Panel2.Controls.Add(_permissionsGrid);
        tab.Controls.Add(split);
        return tab;
    }

    private TabPage BuildDevicesTab()
    {
        var tab = new TabPage("Dispositivos");
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        var revoke = new Button { Text = "Revogar dispositivo selecionado", Dock = DockStyle.Top, Height = 42 };
        revoke.Click += async (_, _) => await RevokeSelectedDeviceAsync();
        ConfigureGrid(
            _devicesGrid,
            ("Conta", 220),
            ("Nome", 260),
            ("Ultima atividade", 170),
            ("Confiavel ate", 170),
            ("Revogado", 90));
        panel.Controls.Add(_devicesGrid);
        panel.Controls.Add(revoke);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage BuildSessionsTab()
    {
        var tab = new TabPage("Sessoes");
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        var revoke = new Button { Text = "Revogar sessao selecionada", Dock = DockStyle.Top, Height = 42 };
        revoke.Click += async (_, _) => await RevokeSelectedSessionAsync();
        ConfigureGrid(
            _sessionsGrid,
            ("Conta", 220),
            ("Sessao", 280),
            ("Dispositivo", 280),
            ("Ultima atividade", 170),
            ("Expira em", 170),
            ("Revogada", 90));
        panel.Controls.Add(_sessionsGrid);
        panel.Controls.Add(revoke);
        tab.Controls.Add(panel);
        return tab;
    }

    private async Task LoadDataAsync()
    {
        if (_isLoading || IsDisposed)
        {
            return;
        }

        var context = GetContext();
        if (context is null)
        {
            ShowStatus("A sessao administrativa nao esta disponivel.", true);
            return;
        }

        _isLoading = true;
        SetBusy(true);
        try
        {
            var accounts = await _client.GetAccountsAsync(context, _closingCancellation.Token);
            var roles = await _client.GetRolesAsync(context, _closingCancellation.Token);
            var permissions = await _client.GetPermissionsAsync(context, _closingCancellation.Token);
            var devices = await _client.GetDevicesAsync(context, true, _closingCancellation.Token);
            var sessions = await _client.GetSessionsAsync(context, true, _closingCancellation.Token);

            FillAccounts(accounts);
            FillRoles(roles);
            FillPermissions(permissions);
            FillDevices(devices);
            FillSessions(sessions);
            _roleInput.Items.Clear();
            foreach (var role in roles)
            {
                _roleInput.Items.Add(role.Name);
            }

            if (_roleInput.Items.Count > 0 && _roleInput.SelectedIndex < 0)
            {
                _roleInput.SelectedIndex = 0;
            }

            ShowStatus("Dados administrativos atualizados.", false);
        }
        catch (OperationCanceledException) when (_closingCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, true);
        }
        finally
        {
            _isLoading = false;
            SetBusy(false);
        }
    }

    private async Task ProvisionAccountAsync()
    {
        var context = GetContext();
        var role = _roleInput.SelectedItem?.ToString();
        if (context is null
            || string.IsNullOrWhiteSpace(_userNameInput.Text)
            || string.IsNullOrWhiteSpace(_emailInput.Text)
            || string.IsNullOrWhiteSpace(_passwordInput.Text)
            || string.IsNullOrWhiteSpace(role))
        {
            ShowStatus("Preencha usuario, email, senha e role.", true);
            return;
        }

        if (!await RequestStepUpAsync(context))
        {
            return;
        }

        try
        {
            SetBusy(true);
            var result = await _client.ProvisionAsync(
                _userNameInput.Text.Trim(),
                _emailInput.Text.Trim(),
                _passwordInput.Text,
                role,
                context,
                _closingCancellation.Token);
            if (result is null || !result.Succeeded)
            {
                throw new WinAppAuthenticationException("O Identity nao confirmou o provisionamento da conta.");
            }

            DarkMessageBox.Show(
                "Conta criada com sucesso. A conta ja pode iniciar sessao com a senha informada.",
                "Provisionamento concluido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            _userNameInput.Clear();
            _emailInput.Clear();
            _passwordInput.Clear();
            await LoadDataAsync();
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ToggleSelectedAccountAsync()
    {
        if (GetSelected<WinAppAdminAccount>(_accountsGrid) is not WinAppAdminAccount account)
        {
            ShowStatus("Selecione uma conta.", true);
            return;
        }

        var context = GetContext();
        if (context is null || !await RequestStepUpAsync(context))
        {
            return;
        }

        try
        {
            await _client.SetLockAsync(account.Id, !account.IsLocked, context, _closingCancellation.Token);
            await LoadDataAsync();
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, true);
        }
    }

    private async Task AssignSelectedRoleAsync(bool assign)
    {
        if (GetSelected<WinAppAdminAccount>(_accountsGrid) is not WinAppAdminAccount account
            || _roleInput.SelectedItem?.ToString() is not { Length: > 0 } role)
        {
            ShowStatus("Selecione uma conta e uma role.", true);
            return;
        }

        var context = GetContext();
        if (context is null || !await RequestStepUpAsync(context))
        {
            return;
        }

        try
        {
            await _client.AssignRoleAsync(account.Id, role, assign, context, _closingCancellation.Token);
            await LoadDataAsync();
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, true);
        }
    }

    private async Task RevokeSelectedDeviceAsync()
    {
        if (GetSelected<WinAppAdminDevice>(_devicesGrid) is not WinAppAdminDevice device)
        {
            ShowStatus("Selecione um dispositivo.", true);
            return;
        }

        var context = GetContext();
        if (context is null || !await RequestStepUpAsync(context))
        {
            return;
        }

        try
        {
            await _client.RevokeDeviceAsync(device.DeviceId, context, _closingCancellation.Token);
            await LoadDataAsync();
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, true);
        }
    }

    private async Task RevokeSelectedSessionAsync()
    {
        if (GetSelected<WinAppAdminSession>(_sessionsGrid) is not WinAppAdminSession session)
        {
            ShowStatus("Selecione uma sessao.", true);
            return;
        }

        var context = GetContext();
        if (context is null || !await RequestStepUpAsync(context))
        {
            return;
        }

        try
        {
            await _client.RevokeSessionAsync(session.SessionId, context, _closingCancellation.Token);
            await LoadDataAsync();
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, true);
        }
    }

    private async Task<bool> RequestStepUpAsync(IdentityAdminContext context)
    {
        var token = DarkInputDialog.Show(
            "Informe o codigo TOTP para autorizar esta operacao administrativa.",
            "Step-up MFA");
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            await _client.GrantProvisionStepUpAsync(token.Trim(), context, _closingCancellation.Token);
            return true;
        }
        catch (Exception exception)
        {
            ShowStatus(exception.Message, true);
            return false;
        }
    }

    private IdentityAdminContext? GetContext()
    {
        var session = _authenticationService.CurrentSession;
        return session is null
            ? null
            : new IdentityAdminContext(session.SessionId, session.DeviceId);
    }

    private void FillAccounts(IReadOnlyList<WinAppAdminAccount> accounts)
    {
        _accountsGrid.Rows.Clear();
        foreach (var account in accounts)
        {
            var row = _accountsGrid.Rows[_accountsGrid.Rows.Add(
                account.UserName ?? string.Empty,
                account.Email ?? string.Empty,
                account.IsActivationCompleted ? "Sim" : "Nao",
                account.IsLocked ? "Sim" : "Nao",
                string.Join(", ", account.Roles))];
            row.Tag = account;
        }
    }

    private void FillRoles(IReadOnlyList<WinAppAdminRole> roles)
    {
        _rolesGrid.Rows.Clear();
        foreach (var role in roles)
        {
            _rolesGrid.Rows.Add(role.Name, string.Join(", ", role.PermissionKeys));
        }
    }

    private void FillPermissions(IReadOnlyList<WinAppAdminPermission> permissions)
    {
        _permissionsGrid.Rows.Clear();
        foreach (var permission in permissions)
        {
            _permissionsGrid.Rows.Add(permission.Key, permission.Description);
        }
    }

    private void FillDevices(IReadOnlyList<WinAppAdminDevice> devices)
    {
        _devicesGrid.Rows.Clear();
        foreach (var device in devices)
        {
            var row = _devicesGrid.Rows[_devicesGrid.Rows.Add(
                device.AccountId,
                device.Name,
                device.LastSeenAtUtc.ToLocalTime().ToString("g"),
                device.TrustedUntilUtc.ToLocalTime().ToString("g"),
                device.IsRevoked ? "Sim" : "Nao")];
            row.Tag = device;
        }
    }

    private void FillSessions(IReadOnlyList<WinAppAdminSession> sessions)
    {
        _sessionsGrid.Rows.Clear();
        foreach (var session in sessions)
        {
            var row = _sessionsGrid.Rows[_sessionsGrid.Rows.Add(
                session.AccountId,
                session.SessionId.ToString("D"),
                session.DeviceId.ToString("D"),
                session.LastSeenAtUtc.ToLocalTime().ToString("g"),
                session.ExpiresAtUtc.ToLocalTime().ToString("g"),
                session.IsRevoked ? "Sim" : "Nao")];
            row.Tag = session;
        }
    }

    private void ConfigureInput(TextBox input, string placeholder, int width)
    {
        input.Width = width;
        input.Height = 34;
        input.PlaceholderText = placeholder;
    }

    private static Control CreateInputGroup(string labelText, Control input)
    {
        var panel = new Panel { Width = input.Width + 8, Height = 78, Margin = new Padding(0, 0, 12, 0) };
        var label = new Label { Text = labelText, AutoSize = true, Dock = DockStyle.Top, Height = 26 };
        input.Dock = DockStyle.Bottom;
        panel.Controls.Add(input);
        panel.Controls.Add(label);
        return panel;
    }

    private static DataGridView CreateGrid() => new()
    {
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        AutoGenerateColumns = false,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
        Dock = DockStyle.Fill,
        ReadOnly = true,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false
    };

    private static void ConfigureGrid(DataGridView grid, params (string Header, int Width)[] columns)
    {
        grid.Columns.Clear();
        foreach (var column in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = column.Header,
                Width = column.Width,
                MinimumWidth = 80,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = Math.Max(1, column.Width)
            });
        }
    }

    private static T? GetSelected<T>(DataGridView grid) where T : class =>
        grid.CurrentRow?.Tag as T;

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        foreach (Control control in Controls)
        {
            control.Enabled = !busy;
        }
    }

    private void ShowStatus(string message, bool error)
    {
        _statusLabel.Text = message.ReplaceLineEndings(" | ");
        _statusLabel.ForeColor = error
            ? DarkModeColors.ErrorColor
            : DarkModeColors.TextSecondaryColor;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closingCancellation.Cancel();
            _closingCancellation.Dispose();
        }

        base.Dispose(disposing);
    }
}
