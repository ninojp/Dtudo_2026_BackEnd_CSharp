using System.ComponentModel;
using LibDtudo.Shared.Dtos.FileMonitoring;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public sealed class FUC_Monitoramento : UserControl
{
    private readonly MonitoringApiClient client;
    private readonly ApiMyAnimesService catalog;
    private readonly int? myAnimeId;
    private readonly CancellationTokenSource lifetime = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 3000 };
    private readonly Label status = new() { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8), Text = "Iniciando..." };
    private readonly ComboBox locations = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 460 };
    private readonly TreeView tree = new() { Dock = DockStyle.Fill, HideSelection = false, ShowNodeToolTips = true };
    private readonly BindingList<MonitoringEventDto> events = [];
    private readonly BindingList<MonitoringEventDto> history = [];
    private readonly Button toggle = new() { Text = "Pausar coleta", AutoSize = true };
    private readonly Button refresh = new() { Text = "Atualizar inventario", AutoSize = true };
    private MonitoringSessionDto? session;
    private MyAnimeMonitoringLocationDto? target;
    private long eventCursor;
    private long historyCursor;
    private bool busy;
    private bool initializing;
    private Guid? displayedSnapshot;
    private Task startup = Task.CompletedTask;
    private TabControl? owningTabs;
    private TabPage? owningPage;

    public FUC_Monitoramento(WinAppAuthenticationService authentication, int? collectionId = null)
    {
        myAnimeId = collectionId;
        client = new MonitoringApiClient(authentication);
        catalog = new ApiMyAnimesService(authentication);
        Dock = DockStyle.Fill;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(8) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        actions.Controls.AddRange([toggle, refresh, locations]);
        if (myAnimeId.HasValue)
        {
            var change = new Button { Text = "Associar pasta", AutoSize = true };
            change.Click += async (_, _) => await GuardAsync(async () =>
            {
                await PauseAsync();
                if (await ChooseFolderAsync()) await BeginAsync();
            });
            actions.Controls.Add(change);
        }
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var structure = new TabPage("Estrutura");
        structure.Controls.Add(tree);
        var alerts = new TabPage("Ocorrencias em tempo real");
        alerts.Controls.Add(CreateGrid(events));
        var historic = new TabPage("Historico");
        var historicLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        historicLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        historicLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var next = new Button { Text = "Carregar proximos registros", AutoSize = true };
        next.Click += async (_, _) => await GuardAsync(async () =>
        {
            if (session is null) return;
            var page = await client.EventsAsync(session.Id, historyCursor, false, lifetime.Token);
            foreach (var item in page) { history.Add(item); historyCursor = item.Id; }
            Trim(history);
        });
        historicLayout.Controls.Add(next, 0, 0);
        historicLayout.Controls.Add(CreateGrid(history), 0, 1);
        historic.Controls.Add(historicLayout);
        tabs.TabPages.AddRange([structure, alerts, historic]);
        layout.Controls.Add(actions, 0, 0);
        layout.Controls.Add(status, 0, 1);
        layout.Controls.Add(tabs, 0, 2);
        Controls.Add(layout);
        ThemeManager.ApplyDarkModeToUserControl(this);
        Load += (_, _) => startup = GuardAsync(async () =>
        {
            owningPage = Parent as TabPage;
            owningTabs = owningPage?.Parent as TabControl;
            if (owningTabs is not null) owningTabs.ControlRemoved += ParentTabRemoved;
            await BeginAsync();
        });
        timer.Tick += async (_, _) => await GuardAsync(PollAsync);
        toggle.Click += async (_, _) => await GuardAsync(async () =>
        {
            if (session is null) await BeginAsync(); else await PauseAsync();
        });
        refresh.Click += async (_, _) => await GuardAsync(async () =>
        {
            if (session is not null) { await client.RefreshAsync(session.Id, lifetime.Token); status.Text = "Atualizacao do inventario solicitada."; }
        });
        locations.SelectedIndexChanged += async (_, _) =>
        {
            if (!initializing) await GuardAsync(LoadTreeAsync);
        };
    }

    private async Task BeginAsync()
    {
        if (session is not null) return;
        toggle.Text = "Iniciar coleta";
        status.Text = "Conectando ao monitoramento...";
        if (myAnimeId.HasValue && target is null)
        {
            target = await catalog.ObterLocalizacaoMonitoramentoAsync(myAnimeId.Value, lifetime.Token);
            if (target is null && !await ChooseFolderAsync()) { status.Text = "Nenhuma pasta associada. Coleta parada."; toggle.Text = "Iniciar coleta"; return; }
        }
        session = await client.StartAsync(new StartMonitoringRequest(target?.RootKey, target?.RelativePath ?? ""), lifetime.Token);
        eventCursor = 0;
        historyCursor = 0;
        events.Clear();
        history.Clear();
        displayedSnapshot = null;
        initializing = true;
        locations.Items.Clear();
        foreach (var location in session.Locations) locations.Items.Add(new LocationChoice(location.Id, location.RootKey + "\\" + location.RelativePath));
        if (locations.Items.Count > 0) locations.SelectedIndex = 0;
        initializing = false;
        toggle.Text = "Pausar coleta";
        timer.Start();
        await PollAsync();
    }

    private async Task<bool> ChooseFolderAsync()
    {
        var roots = await client.RootsAsync(lifetime.Token);
        using var dialog = new FolderBrowserDialog { Description = $"Pasta existente da colecao MyAnime #{myAnimeId}", ShowNewFolderButton = false, UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return false;
        var chosen = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dialog.SelectedPath));
        var root = roots.FirstOrDefault(item => chosen.StartsWith(item.Path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));
        if (root is null) throw new InvalidOperationException("A pasta deve estar dentro de uma das letras autorizadas.");
        var relative = Path.GetRelativePath(root.Path, chosen);
        if (!MonitoringPathPolicy.IsValidRelativePath(relative))
            throw new InvalidOperationException("Essa pasta esta excluida do monitoramento.");
        if (DarkMessageBox.Show($"Associar MyAnime #{myAnimeId} a:\n{chosen}\n\nSomente o vinculo no banco sera salvo.", "Monitoramento", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
        var selectedTarget = new MyAnimeMonitoringLocationDto(root.Key, relative);
        await catalog.SalvarLocalizacaoMonitoramentoAsync(myAnimeId!.Value, selectedTarget, lifetime.Token);
        target = selectedTarget;
        return true;
    }

    private async Task PollAsync()
    {
        if (session is null) return;
        session = await client.HeartbeatAsync(session.Id, lifetime.Token);
        var files = session.Locations.Sum(location => location.Files);
        var bytes = session.Locations.Sum(location => location.Bytes);
        status.Text = $"{session.State} | {files:N0} arquivos | {bytes / 1_000_000_000d:N2} GB | "
            + (session.InventoryOutdated ? "Inventario com alteracoes pendentes de atualizacao." : "Inventario da sessao.")
            + (session.Error is null ? "" : " " + session.Error);
        var page = await client.EventsAsync(session.Id, eventCursor, eventCursor == 0, lifetime.Token);
        foreach (var item in page) { events.Add(item); eventCursor = item.Id; }
        Trim(events);
        await LoadTreeAsync();
    }

    private async Task LoadTreeAsync()
    {
        if (session is null || locations.SelectedItem is not LocationChoice choice) return;
        var location = session.Locations.First(item => item.Id == choice.Id);
        if (location.SnapshotId is not Guid snapshot || snapshot == displayedSnapshot) return;
        var collected = new List<MonitoringEntryDto>();
        long cursor = 0;
        var lastRenewal = DateTimeOffset.UtcNow;
        while (true)
        {
            if (DateTimeOffset.UtcNow - lastRenewal > TimeSpan.FromSeconds(10))
            {
                await client.HeartbeatAsync(session.Id, lifetime.Token);
                lastRenewal = DateTimeOffset.UtcNow;
            }
            var page = await client.EntriesAsync(session.Id, location.Id, snapshot, cursor, lifetime.Token);
            collected.AddRange(page);
            if (page.Length < 1000) break;
            cursor = page[^1].Id;
        }
        lifetime.Token.ThrowIfCancellationRequested();
        tree.BeginUpdate();
        try
        {
            tree.Nodes.Clear();
            var root = tree.Nodes.Add($"{choice.Label} | {location.Completion} | {location.InventoriedAtUtc?.ToLocalTime():g}");
            var nodes = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase) { [""] = root };
            foreach (var entry in collected.OrderBy(entry => entry.RelativePath.Count(character => character == '\\')).ThenBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                var parent = Path.GetDirectoryName(entry.RelativePath) ?? "";
                if (!nodes.TryGetValue(parent, out var parentNode)) parentNode = root;
                var node = parentNode.Nodes.Add(Path.GetFileName(entry.RelativePath) + (entry.IsDirectory ? "" : $"  ({entry.Bytes:N0} bytes)"));
                node.ToolTipText = entry.RelativePath;
                if (entry.IsDirectory) nodes[entry.RelativePath] = node;
            }
            root.Expand();
            displayedSnapshot = snapshot;
        }
        finally { tree.EndUpdate(); }
    }

    private async Task PauseAsync()
    {
        timer.Stop();
        if (session is not null)
        {
            var id = session.Id;
            await client.StopAsync(id, lifetime.Token);
            session = null;
        }
        toggle.Text = "Iniciar coleta";
        status.Text = "Coleta parada. Exibindo o ultimo estado carregado.";
    }

    private async Task GuardAsync(Func<Task> operation)
    {
        if (busy || IsDisposed) return;
        busy = true;
        try { await operation(); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (HttpRequestException exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            timer.Stop();
            session = null;
            if (!IsDisposed)
            {
                toggle.Text = "Iniciar coleta";
                status.Text = "Sessao expirada ou API reiniciada. Coleta parada; o historico foi preservado.";
            }
        }
        catch (Exception exception)
        {
            if (!IsDisposed)
            {
                if (session is null) toggle.Text = "Iniciar coleta";
                status.Text = exception.Message;
            }
        }
        finally { busy = false; }
    }

    private static DataGridView CreateGrid(BindingList<MonitoringEventDto> source) => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoGenerateColumns = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
        DataSource = source, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };
    private static void Trim(BindingList<MonitoringEventDto> list) { while (list.Count > 2000) list.RemoveAt(0); }
    private void ParentTabRemoved(object? sender, ControlEventArgs args) { if (args.Control == owningPage) Dispose(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !lifetime.IsCancellationRequested)
        {
            timer.Stop();
            timer.Dispose();
            if (owningTabs is not null) owningTabs.ControlRemoved -= ParentTabRemoved;
            lifetime.Cancel();
            _ = FinishAsync();
        }
        base.Dispose(disposing);
    }

    private async Task FinishAsync()
    {
        try
        {
            await startup;
            if (session is not null)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await client.StopAsync(session.Id, timeout.Token);
            }
        }
        catch (Exception exception) { System.Diagnostics.Trace.TraceWarning("Encerramento de monitoramento: " + exception.GetType().Name); }
        finally { client.Dispose(); }
    }

    private sealed record LocationChoice(Guid Id, string Label)
    {
        public override string ToString() => Label;
    }
}
