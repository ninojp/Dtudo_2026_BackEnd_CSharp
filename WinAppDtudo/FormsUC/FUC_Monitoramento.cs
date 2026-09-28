using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using LibDtudo.Shared.Dtos.FileMonitoring;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public sealed class FUC_Monitoramento : UserControl
{
    private static readonly HashSet<string> imageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".apng", ".avif", ".bmp", ".gif", ".heic", ".heif", ".ico", ".jfif", ".jpe", ".jpeg", ".jpg", ".png", ".svg", ".tif", ".tiff", ".webp"
    };
    private static readonly HashSet<string> videoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".3g2", ".3gp", ".asf", ".avi", ".divx", ".flv", ".m2ts", ".m2v", ".m4v", ".mkv", ".mov", ".mp4", ".mpe", ".mpeg", ".mpg", ".mts", ".ogv", ".rm", ".rmvb", ".ts", ".vob", ".webm", ".wmv"
    };

    private readonly MonitoringApiClient client;
    private readonly ApiMyAnimesService catalog;
    private readonly int? myAnimeId;
    private readonly CancellationTokenSource lifetime = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 3000 };
    private readonly Label status = new() { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8), Text = "Iniciando..." };
    private readonly ComboBox locations = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 460 };
    private readonly TreeView tree = new()
    {
        Dock = DockStyle.Fill,
        HideSelection = false,
        ShowNodeToolTips = true,
        DrawMode = TreeViewDrawMode.OwnerDrawText
    };
    private readonly BindingList<MonitoringEventDto> events = [];
    private readonly BindingList<MonitoringEventDto> history = [];
    private readonly BindingList<CollectionSyncEventDto> synchronizationEvents = [];
    private readonly BindingList<CollectionSyncEventDto> visibleSynchronizationEvents = [];
    private readonly Dictionary<string, string> rootPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly Button toggle = new() { Text = "Pausar coleta", AutoSize = true };
    private readonly Button refresh = new() { Text = "Atualizar inventario", AutoSize = true };
    private readonly Button synchronize = new() { Text = "Sincronizar colecoes", AutoSize = true };
    private readonly ComboBox synchronizationFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly Button exportText = new() { Text = "Exportar TXT", AutoSize = true };
    private readonly Button exportCsv = new() { Text = "Exportar CSV", AutoSize = true };
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
    private TabControl? monitorTabs;
    private TabPage? synchronizationPage;
    private DataGridView? synchronizationGrid;
    private readonly Action<int>? openMyAnimeStructure;

    public FUC_Monitoramento(WinAppAuthenticationService authentication, int? collectionId = null, Action<int>? openMyAnimeStructure = null)
    {
        myAnimeId = collectionId;
        this.openMyAnimeStructure = openMyAnimeStructure;
        client = new MonitoringApiClient(authentication);
        catalog = new ApiMyAnimesService(authentication);
        Dock = DockStyle.Fill;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(8) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        actions.Controls.AddRange([toggle, refresh, locations]);
        if (!myAnimeId.HasValue) actions.Controls.Add(synchronize);
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
        tree.DrawNode += Tree_DrawNode;
        tree.NodeMouseClick += Tree_NodeMouseClick;
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
        synchronizationFilter.Items.AddRange(["Todos", "Erros", "Alteracoes", "Atualizacoes"]);
        synchronizationFilter.SelectedIndex = 0;
        synchronizationFilter.SelectedIndexChanged += (_, _) => ApplySynchronizationFilter();
        exportText.Click += (_, _) => ExportSynchronizationReport(false);
        exportCsv.Click += (_, _) => ExportSynchronizationReport(true);
        synchronizationGrid = CreateGrid(visibleSynchronizationEvents);
        synchronizationGrid.CellFormatting += SynchronizationGrid_CellFormatting;
        synchronizationGrid.CellClick += SynchronizationGrid_CellClick;
        var synchronizationActions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        synchronizationActions.Controls.AddRange([synchronizationFilter, exportText, exportCsv]);
        var synchronizationLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        synchronizationLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        synchronizationLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        synchronizationLayout.Controls.Add(synchronizationActions, 0, 0);
        synchronizationLayout.Controls.Add(synchronizationGrid, 0, 1);
        synchronizationPage = new TabPage("Sincronizacao");
        synchronizationPage.Controls.Add(synchronizationLayout);
        tabs.TabPages.AddRange([structure, alerts, historic]);
        tabs.TabPages.Add(synchronizationPage);
        monitorTabs = tabs;
        layout.Controls.Add(actions, 0, 0);
        layout.Controls.Add(status, 0, 1);
        layout.Controls.Add(tabs, 0, 2);
        Controls.Add(layout);
        ThemeManager.ApplyDarkModeToUserControl(this);
        IncreaseFontsByThreePoints(this);
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
        synchronize.Click += async (_, _) => await GuardAsync(SynchronizeCollectionsAsync);
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
        if (rootPaths.Count == 0) await LoadRootsAsync();
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
        var roots = await LoadRootsAsync();
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

    private async Task<MonitoringRootDto[]> LoadRootsAsync()
    {
        var roots = await client.RootsAsync(lifetime.Token);
        rootPaths.Clear();
        foreach (var root in roots)
            rootPaths[root.Key] = Path.GetFullPath(root.Path);
        return roots;
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

    private async Task SynchronizeCollectionsAsync()
    {
        if (myAnimeId.HasValue) return;
        var resumeMonitoring = session is not null;
        synchronize.Enabled = false;
        synchronizationEvents.Clear();
        try
        {
            if (resumeMonitoring) await PauseAsync();
            status.Text = "Descobrindo colecoes locais...";
            var discovery = await client.DiscoverCollectionsAsync(lifetime.Token);
            status.Text = $"Aplicando sincronizacao: {discovery.Folders.Count:N0} pastas descobertas...";
            var result = await catalog.SincronizarColecoesAsync(discovery, lifetime.Token);
            foreach (var item in result.Events) synchronizationEvents.Add(item);
            ApplySynchronizationFilter();
            status.Text = $"Sincronizacao {result.Status}: {result.AddedMappings:N0} adicionadas, "
                + $"{result.UpdatedMappings:N0} atualizadas, {result.Warnings:N0} avisos, {result.Errors:N0} erros.";
            if (monitorTabs is not null && synchronizationPage is not null) monitorTabs.SelectedTab = synchronizationPage;
        }
        finally
        {
            synchronize.Enabled = true;
            if (resumeMonitoring && !lifetime.IsCancellationRequested) await BeginAsync();
        }
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
            root.ForeColor = Color.Yellow;
            root.Tag = new DirectoryEntryDisplay(location.RootKey, location.RelativePath ?? "");
            root.ToolTipText = "Clique para abrir no Windows Explorer.";
            var nodes = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase) { [""] = root };
            foreach (var entry in collected.OrderBy(entry => entry.RelativePath.Count(character => character == '\\')).ThenBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                var parent = Path.GetDirectoryName(entry.RelativePath) ?? "";
                if (!nodes.TryGetValue(parent, out var parentNode)) parentNode = root;
                var name = Path.GetFileName(entry.RelativePath);
                var sizeText = GetFileSizeText(entry);
                var node = parentNode.Nodes.Add(name + sizeText);
                node.ToolTipText = entry.IsDirectory
                    ? $"{entry.RelativePath}\nClique para abrir no Windows Explorer."
                    : entry.RelativePath;
                if (entry.IsDirectory)
                {
                    node.ForeColor = Color.Yellow;
                    var relativePath = string.IsNullOrEmpty(location.RelativePath)
                        ? entry.RelativePath
                        : Path.Combine(location.RelativePath, entry.RelativePath);
                    node.Tag = new DirectoryEntryDisplay(location.RootKey, relativePath);
                    nodes[entry.RelativePath] = node;
                }
                else
                {
                    node.ForeColor = GetFileNameColor(entry.RelativePath);
                    node.Tag = new FileEntryDisplay(name, sizeText);
                }
            }
            root.Expand();
            displayedSnapshot = snapshot;
        }
        finally { tree.EndUpdate(); }
    }

    private Color GetFileNameColor(string path)
    {
        var extension = Path.GetExtension(path);
        if (imageExtensions.Contains(extension)) return Color.White;
        if (videoExtensions.Contains(extension)) return Color.LimeGreen;
        return tree.ForeColor;
    }

    private static string GetFileSizeText(MonitoringEntryDto entry)
    {
        if (entry.IsDirectory) return "";
        var isVideo = videoExtensions.Contains(Path.GetExtension(entry.RelativePath));
        var divisor = isVideo ? 1024d * 1024d : 1024d;
        var unit = isVideo ? "MB" : "KB";
        return $"  ({entry.Bytes / divisor:N2} {unit})";
    }

    private void Tree_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        var node = e.Node;
        if (e.Button != MouseButtons.Left || node is null || !node.Bounds.Contains(e.Location)
            || node.Tag is not DirectoryEntryDisplay directory) return;

        try
        {
            var path = ResolveDirectoryPath(directory);
            if (!Directory.Exists(path))
            {
                DarkMessageBox.Show("A pasta nao existe mais ou esta inacessivel.", "Monitoramento",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
                UseShellExecute = true
            };
            startInfo.ArgumentList.Add(path);
            Process.Start(startInfo);
        }
        catch (Exception exception)
        {
            DarkMessageBox.Show($"Nao foi possivel abrir a pasta no Windows Explorer.\n\n{exception.Message}",
                "Monitoramento", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string ResolveDirectoryPath(DirectoryEntryDisplay directory)
    {
        if (!rootPaths.TryGetValue(directory.RootKey, out var configuredRoot))
            throw new InvalidOperationException("A raiz autorizada nao esta disponivel nesta sessao.");
        if (Path.IsPathRooted(directory.RelativePath))
            throw new InvalidOperationException("O caminho monitorado nao e relativo a raiz autorizada.");

        var rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configuredRoot));
        var rootPrefix = rootPath.EndsWith(Path.DirectorySeparatorChar) ? rootPath : rootPath + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(rootPath, directory.RelativePath));
        if (!string.Equals(path, rootPath, StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O caminho monitorado esta fora da raiz autorizada.");
        return path;
    }

    private static void Tree_DrawNode(object? sender, DrawTreeNodeEventArgs e)
    {
        var node = e.Node;
        if (node is null) return;
        var font = node.NodeFont ?? node.TreeView?.Font ?? SystemFonts.DefaultFont;
        var measureFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
        var drawFlags = measureFlags | TextFormatFlags.VerticalCenter;
        if (node.Tag is FileEntryDisplay file)
        {
            var nameWidth = TextRenderer.MeasureText(e.Graphics, file.Name, font, Size.Empty, measureFlags).Width;
            var nameBounds = new Rectangle(e.Bounds.X, e.Bounds.Y, nameWidth, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, file.Name, font, nameBounds, node.ForeColor, drawFlags);
            var sizeBounds = new Rectangle(nameBounds.Right, e.Bounds.Y, Math.Max(0, e.Bounds.Right - nameBounds.Right), e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, file.SizeText, font, sizeBounds, Color.LightGray, drawFlags);
            return;
        }

        TextRenderer.DrawText(e.Graphics, node.Text, font, e.Bounds, node.ForeColor, drawFlags);
    }

    private static void IncreaseFontsByThreePoints(Control root)
    {
        var originalFonts = new List<(Control Control, Font Font)>();
        CaptureOriginalFonts(root, originalFonts);
        foreach (var (control, font) in originalFonts)
            control.Font = new Font(font.FontFamily, font.SizeInPoints + 3f, font.Style, GraphicsUnit.Point, font.GdiCharSet, font.GdiVerticalFont);
    }

    private static void CaptureOriginalFonts(Control control, List<(Control Control, Font Font)> fonts)
    {
        fonts.Add((control, control.Font));
        foreach (Control child in control.Controls)
            CaptureOriginalFonts(child, fonts);
    }

    private void ApplySynchronizationFilter()
    {
        if (synchronizationFilter.SelectedItem is not string filter) return;
        visibleSynchronizationEvents.Clear();
        foreach (var item in synchronizationEvents.Where(item => MatchesSynchronizationFilter(item, filter)))
            visibleSynchronizationEvents.Add(item);
    }

    private static bool MatchesSynchronizationFilter(CollectionSyncEventDto item, string filter) => filter switch
    {
        "Erros" => GetSynchronizationRowKind(item) == SynchronizationRowKind.Error,
        "Alteracoes" => GetSynchronizationRowKind(item) == SynchronizationRowKind.Change,
        "Atualizacoes" => GetSynchronizationRowKind(item) == SynchronizationRowKind.Update,
        _ => true
    };

    private void SynchronizationGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || synchronizationGrid?.Rows[e.RowIndex].DataBoundItem is not CollectionSyncEventDto item) return;
        if (item.MyAnimeId is int collectionId && collectionId > 0)
        {
            openMyAnimeStructure?.Invoke(collectionId);
            return;
        }
        DarkMessageBox.Show("Esta ocorrencia nao possui um MyAnime associado para abrir.", "Estrutura MyAnime", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void SynchronizationGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || sender is not DataGridView grid || grid.Rows[e.RowIndex].DataBoundItem is not CollectionSyncEventDto item) return;
        var color = GetSynchronizationRowKind(item) switch
        {
            SynchronizationRowKind.Error => Color.FromArgb(115, 45, 45),
            SynchronizationRowKind.Change => Color.FromArgb(45, 75, 125),
            SynchronizationRowKind.Update => Color.FromArgb(45, 105, 65),
            _ => (Color?)null
        };
        if (color is not Color background) return;
        e.CellStyle.BackColor = background;
        e.CellStyle.ForeColor = Color.White;
        e.CellStyle.SelectionBackColor = ControlPaint.Light(background);
        e.CellStyle.SelectionForeColor = Color.White;
    }

    private void ExportSynchronizationReport(bool csv)
    {
        if (visibleSynchronizationEvents.Count == 0)
        {
            DarkMessageBox.Show("Nao ha ocorrencias visiveis para exportar.", "Sincronizacao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = csv ? "Planilha CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*" : "Arquivo de texto (*.txt)|*.txt|Todos os arquivos (*.*)|*.*",
            DefaultExt = csv ? "csv" : "txt",
            AddExtension = true,
            FileName = $"sincronizacao-{DateTime.Now:yyyyMMdd-HHmmss}.{(csv ? "csv" : "txt")}",
            OverwritePrompt = true,
            Title = csv ? "Exportar relatorio para planilha" : "Exportar relatorio para texto"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var content = csv ? BuildSynchronizationCsv() : BuildSynchronizationText();
            File.WriteAllText(dialog.FileName, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
        catch (Exception exception)
        {
            DarkMessageBox.Show($"Nao foi possivel exportar o relatorio.\n\n{exception.Message}",
                "Exportacao", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string BuildSynchronizationText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Data;Severidade;Codigo;Raiz;Caminho;MyAnimeId;Raiz anterior;Raiz nova;Detalhe");
        foreach (var item in visibleSynchronizationEvents)
            builder.AppendLine(string.Join(";", item.OccurredAtUtc.ToLocalTime().ToString("G", CultureInfo.CurrentCulture), item.Severity,
                item.Code, item.RootKey, item.RelativePath, item.MyAnimeId?.ToString() ?? "", item.PreviousRootKey ?? "", item.NewRootKey ?? "", item.Detail));
        return builder.ToString();
    }

    private string BuildSynchronizationCsv()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Data,Severidade,Codigo,Raiz,Caminho,MyAnimeId,Raiz anterior,Raiz nova,Detalhe");
        foreach (var item in visibleSynchronizationEvents)
            builder.AppendLine(string.Join(",", new[] { item.OccurredAtUtc.ToLocalTime().ToString("O", CultureInfo.InvariantCulture), item.Severity,
                item.Code, item.RootKey, item.RelativePath, item.MyAnimeId?.ToString(CultureInfo.InvariantCulture) ?? "", item.PreviousRootKey ?? "", item.NewRootKey ?? "", item.Detail }
                .Select(EscapeCsv)));
        return builder.ToString();
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static SynchronizationRowKind GetSynchronizationRowKind(CollectionSyncEventDto item) =>
        item.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase)
            ? SynchronizationRowKind.Error
            : item.Code == "MAPPING_UPDATED" || item.Code == "EXISTING_MAPPING_NOT_DISCOVERED"
                ? SynchronizationRowKind.Change
                : item.Code is "MAPPING_ADDED" or "MAPPING_UNCHANGED"
                    ? SynchronizationRowKind.Update
                    : SynchronizationRowKind.Other;

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

    private static DataGridView CreateGrid<T>(BindingList<T> source) => new()
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

    private sealed record FileEntryDisplay(string Name, string SizeText);
    private sealed record DirectoryEntryDisplay(string RootKey, string RelativePath);

    private enum SynchronizationRowKind
    {
        Other,
        Error,
        Change,
        Update
    }
}
