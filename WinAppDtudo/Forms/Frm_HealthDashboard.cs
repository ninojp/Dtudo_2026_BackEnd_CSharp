using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public sealed class Frm_HealthDashboard : Form
{
    private readonly WinAppHealthMonitoringService _healthService;
    private readonly DataGridView _healthGrid = new();
    private readonly Label _summaryLabel = new();
    private readonly Label _lastCheckLabel = new();
    private readonly Button _refreshButton = new();
    private readonly CancellationTokenSource _closingCancellation = new();
    private bool _isRefreshing;

    public Frm_HealthDashboard(WinAppHealthMonitoringService healthService)
    {
        _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
        Text = "Painel de saude e alertas";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1280, 760);
        MinimumSize = new Size(960, 560);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;

        BuildLayout();
        ThemeManager.ApplyDarkModeToForm(this);
        FormClosed += (_, _) => _closingCancellation.Cancel();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_isRefreshing || IsDisposed)
        {
            return;
        }

        _isRefreshing = true;
        _refreshButton.Enabled = false;
        _summaryLabel.Text = "Consultando fontes protegidas...";
        try
        {
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _closingCancellation.Token);
            var snapshot = await _healthService.CheckAsync(linkedCancellation.Token);
            ApplySnapshot(snapshot);
        }
        catch (OperationCanceledException) when (_closingCancellation.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            _summaryLabel.Text = "Atualizacao cancelada.";
        }
        catch (WinAppAuthenticationException)
        {
            _summaryLabel.Text = "Sessao administrativa indisponivel para esta consulta.";
            _lastCheckLabel.Text = "Estado indisponivel";
        }
        catch (HttpRequestException)
        {
            _summaryLabel.Text = "Uma ou mais fontes nao responderam.";
            _lastCheckLabel.Text = "Estado indisponivel";
        }
        finally
        {
            _isRefreshing = false;
            if (!IsDisposed)
            {
                _refreshButton.Enabled = true;
            }
        }
    }

    public void ApplySnapshot(WinAppHealthSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (IsDisposed)
        {
            return;
        }

        _healthGrid.Rows.Clear();
        foreach (var item in snapshot.Items.OrderBy(item => item.Category).ThenBy(item => item.Name))
        {
            var rowIndex = _healthGrid.Rows.Add(
                item.Category,
                item.Name,
                GetStateText(item.State),
                item.Summary,
                item.CheckedAtUtc.ToLocalTime().ToString("g"));
            var row = _healthGrid.Rows[rowIndex];
            row.Cells[2].Style.ForeColor = GetStateColor(item.State);
            row.Cells[2].Style.Font = new Font(_healthGrid.Font, FontStyle.Bold);
        }

        var healthy = snapshot.Items.Count(item => item.State == WinAppHealthState.Healthy);
        var warnings = snapshot.Items.Count(item => item.State == WinAppHealthState.Warning);
        var critical = snapshot.Items.Count(item => item.State == WinAppHealthState.Critical);
        var unavailable = snapshot.Items.Count(item => item.State == WinAppHealthState.Unavailable);
        _summaryLabel.Text = $"{healthy} operacional(is) | {warnings} aviso(s) | {critical} critico(s) | {unavailable} indisponivel(is)";
        _lastCheckLabel.Text = $"Ultima consulta: {snapshot.CheckedAtUtc.ToLocalTime():g}";
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

    private void BuildLayout()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 86,
            Padding = new Padding(18, 12, 18, 10)
        };
        var title = new Label
        {
            AutoSize = true,
            Text = "Saude operacional e alertas",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Location = new Point(18, 10)
        };
        _lastCheckLabel.AutoSize = true;
        _lastCheckLabel.Text = "Nenhuma consulta executada";
        _lastCheckLabel.Location = new Point(20, 48);
        _lastCheckLabel.Font = new Font("Segoe UI", 10F);
        _refreshButton.Text = "Atualizar";
        _refreshButton.AutoSize = true;
        _refreshButton.Height = 42;
        _refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _refreshButton.Location = new Point(1070, 16);
        _refreshButton.Click += async (_, _) => await RefreshAsync();
        header.Resize += (_, _) => _refreshButton.Left = header.ClientSize.Width - _refreshButton.Width;
        header.Controls.Add(title);
        header.Controls.Add(_lastCheckLabel);
        header.Controls.Add(_refreshButton);

        _summaryLabel.Dock = DockStyle.Bottom;
        _summaryLabel.Height = 38;
        _summaryLabel.Padding = new Padding(18, 8, 18, 6);
        _summaryLabel.Text = "Nenhuma consulta executada";

        ConfigureGrid();
        Controls.Add(_healthGrid);
        Controls.Add(_summaryLabel);
        Controls.Add(header);
    }

    private void ConfigureGrid()
    {
        _healthGrid.Dock = DockStyle.Fill;
        _healthGrid.ReadOnly = true;
        _healthGrid.AllowUserToAddRows = false;
        _healthGrid.AllowUserToDeleteRows = false;
        _healthGrid.AllowUserToResizeRows = false;
        _healthGrid.AutoGenerateColumns = false;
        _healthGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _healthGrid.ColumnHeadersHeight = 42;
        _healthGrid.RowTemplate.Height = 42;
        _healthGrid.RowHeadersVisible = false;
        _healthGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _healthGrid.MultiSelect = false;
        _healthGrid.Columns.Add(CreateColumn("Categoria", 16));
        _healthGrid.Columns.Add(CreateColumn("Fonte", 22));
        _healthGrid.Columns.Add(CreateColumn("Estado", 14));
        var summaryColumn = CreateColumn("Resumo", 34);
        summaryColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _healthGrid.Columns.Add(summaryColumn);
        _healthGrid.Columns.Add(CreateColumn("Consultado", 18));
    }

    private static DataGridViewTextBoxColumn CreateColumn(string headerText, float fillWeight) =>
        new()
        {
            HeaderText = headerText,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            MinimumWidth = 100
        };

    private static string GetStateText(WinAppHealthState state) => state switch
    {
        WinAppHealthState.Healthy => "Operacional",
        WinAppHealthState.Warning => "Aviso",
        WinAppHealthState.Critical => "Critico",
        _ => "Indisponivel"
    };

    private static Color GetStateColor(WinAppHealthState state) => state switch
    {
        WinAppHealthState.Healthy => DarkModeColors.SuccessColor,
        WinAppHealthState.Warning => DarkModeColors.WarningColor,
        WinAppHealthState.Critical => DarkModeColors.ErrorColor,
        _ => DarkModeColors.InactiveTabTextColor
    };
}
