using WinAppDtudo.Services;

namespace WinAppDtudo.Controls;

/// <summary>
/// Gerencia as abas de um <see cref="DarkTabControl"/> usado como area de trabalho da janela principal:
/// abre ou reaproveita abas por chave (<see cref="Control.Name"/> da aba), fecha abas descartando seus
/// UserControls, trata o botao X, o clique do meio e o menu de contexto das abas e mantem os icones
/// do cabecalho. Abas <see cref="PinnedTabPage"/> nunca sao fechadas.
/// </summary>
public sealed class TabWorkspace : IDisposable
{
    private readonly DarkTabControl _tabs;
    private readonly bool _ownsImageList;
    private ContextMenuStrip? _contextMenu;
    private PinnedTabPage? _homePage;
    private bool _disposed;

    public TabWorkspace(DarkTabControl tabs)
    {
        _tabs = tabs ?? throw new ArgumentNullException(nameof(tabs));
        if (_tabs.ImageList is null)
        {
            _tabs.ImageList = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                TransparentColor = Color.Transparent
            };
            _ownsImageList = true;
        }

        _tabs.MouseDown += Tabs_MouseDown;
        _tabs.Selected += Tabs_Selected;
    }

    /// <summary>Disparado quando o usuario ou o codigo seleciona uma aba.</summary>
    public event EventHandler<TabControlEventArgs>? TabSelected;

    public DarkTabControl TabControl => _tabs;

    public TabPage? SelectedPage => _tabs.SelectedTab;

    public IReadOnlyList<TabPage> Pages => _tabs.TabPages.Cast<TabPage>().ToList();

    public TabPage? Find(string key)
    {
        foreach (TabPage page in _tabs.TabPages)
        {
            if (string.Equals(page.Name, key, StringComparison.Ordinal))
                return page;
        }

        return null;
    }

    /// <summary>Seleciona a aba com a chave informada, se existir.</summary>
    public bool Activate(string key)
    {
        var page = Find(key);
        if (page is null)
            return false;

        _tabs.SelectedTab = page;
        return true;
    }

    /// <summary>
    /// Seleciona a aba com a chave informada ou, se nao existir, cria a aba com <paramref name="createPage"/>,
    /// adiciona ao controle e a seleciona. A aba criada precisa ter <see cref="Control.Name"/> igual a <paramref name="key"/>.
    /// </summary>
    public TabPage OpenOrActivate(string key, Func<TabPage> createPage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(createPage);

        var existing = Find(key);
        if (existing is not null)
        {
            _tabs.SelectedTab = existing;
            return existing;
        }

        var page = createPage();
        if (!string.Equals(page.Name, key, StringComparison.Ordinal))
        {
            page.Dispose();
            throw new InvalidOperationException($"A aba criada precisa usar a chave '{key}' como Name.");
        }

        Add(page, select: true);
        return page;
    }

    /// <summary>
    /// Define a aba inicial: ela aparece enquanto nao houver outra aba, e e removida (sem ser descartada)
    /// assim que qualquer outra aba for aberta. Ao fechar a ultima aba, ela volta a ser exibida.
    /// </summary>
    public void SetHomePage(PinnedTabPage homePage)
    {
        ArgumentNullException.ThrowIfNull(homePage);
        if (_homePage is not null)
            throw new InvalidOperationException("A aba inicial ja foi definida.");

        _homePage = homePage;
        if (_tabs.TabCount == 0)
            ShowHomePage();
    }

    public void Add(TabPage page, bool select = true)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (Find(page.Name) is not null)
            throw new InvalidOperationException($"Ja existe uma aba com a chave '{page.Name}'.");

        _tabs.TabPages.Add(page);
        if (select)
            _tabs.SelectedTab = page;

        if (_homePage is not null && !ReferenceEquals(page, _homePage) && _tabs.TabPages.Contains(_homePage))
            _tabs.TabPages.Remove(_homePage);
    }

    private void ShowHomePage()
    {
        if (_homePage is null || _homePage.IsDisposed || _tabs.TabPages.Contains(_homePage))
            return;

        _tabs.TabPages.Add(_homePage);
        _tabs.SelectedTab = _homePage;
    }

    public bool Close(string key) => Find(key) is { } page && Close(page);

    /// <summary>Fecha e descarta a aba (e seus controles). Retorna falso se ela for fixa ou nao pertencer ao controle.</summary>
    public bool Close(TabPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (page is PinnedTabPage || !_tabs.TabPages.Contains(page))
            return false;

        var index = _tabs.TabPages.IndexOf(page);
        var wasSelected = _tabs.SelectedIndex == index;
        _tabs.TabPages.Remove(page);
        page.Dispose();

        if (wasSelected && _tabs.TabCount > 0)
            _tabs.SelectedIndex = Math.Min(index, _tabs.TabCount - 1);

        if (_tabs.TabCount == 0)
            ShowHomePage();

        return true;
    }

    /// <summary>Fecha todas as abas que nao sao fixas.</summary>
    public void CloseAll() => CloseWhere(_ => true);

    /// <summary>Fecha todas as abas nao fixas, exceto a informada.</summary>
    public void CloseOthers(TabPage keep) => CloseWhere(page => !ReferenceEquals(page, keep));

    public void CloseToTheRightOf(TabPage reference)
    {
        var referenceIndex = _tabs.TabPages.IndexOf(reference);
        if (referenceIndex >= 0)
            CloseWhere(page => _tabs.TabPages.IndexOf(page) > referenceIndex);
    }

    public void CloseToTheLeftOf(TabPage reference)
    {
        var referenceIndex = _tabs.TabPages.IndexOf(reference);
        if (referenceIndex >= 0)
            CloseWhere(page => _tabs.TabPages.IndexOf(page) < referenceIndex);
    }

    /// <summary>
    /// Retorna o indice do icone no ImageList do cabecalho, adicionando-o na primeira utilizacao da chave.
    /// </summary>
    public int GetImageIndex(string key, Image? image)
    {
        if (image is null || _tabs.ImageList is not { } imageList)
            return -1;

        var existingIndex = imageList.Images.IndexOfKey(key);
        if (existingIndex >= 0)
            return existingIndex;

        imageList.Images.Add(key, new Bitmap(image));
        return imageList.Images.IndexOfKey(key);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _tabs.MouseDown -= Tabs_MouseDown;
        _tabs.Selected -= Tabs_Selected;
        _contextMenu?.Dispose();
        _contextMenu = null;
        if (_homePage is not null && !_tabs.TabPages.Contains(_homePage))
            _homePage.Dispose();
        _homePage = null;
        if (_ownsImageList)
        {
            var imageList = _tabs.ImageList;
            _tabs.ImageList = null;
            imageList?.Dispose();
        }
    }

    private void CloseWhere(Func<TabPage, bool> predicate)
    {
        // A colecao e percorrida de tras para frente porque as abas sao removidas durante a iteracao.
        for (var index = _tabs.TabPages.Count - 1; index >= 0; index--)
        {
            if (index >= _tabs.TabPages.Count)
                continue;

            var page = _tabs.TabPages[index];
            if (page is not PinnedTabPage && predicate(page))
                Close(page);
        }
    }

    private void Tabs_Selected(object? sender, TabControlEventArgs e) => TabSelected?.Invoke(this, e);

    private void Tabs_MouseDown(object? sender, MouseEventArgs e)
    {
        var index = GetTabIndexAt(e.Location);
        if (index < 0)
            return;

        var page = _tabs.TabPages[index];
        switch (e.Button)
        {
            case MouseButtons.Left when _tabs.HasCloseButton(index) && _tabs.GetCloseButtonBounds(index).Contains(e.Location):
            case MouseButtons.Middle when _tabs.HasCloseButton(index):
                Close(page);
                break;

            case MouseButtons.Right:
                ShowContextMenu(page, e.Location);
                break;
        }
    }

    private int GetTabIndexAt(Point location)
    {
        for (var index = 0; index < _tabs.TabCount; index++)
        {
            if (_tabs.GetTabRect(index).Contains(location))
                return index;
        }

        return -1;
    }

    private void ShowContextMenu(TabPage page, Point location)
    {
        _contextMenu?.Dispose();

        var menu = new ContextMenuStrip();
        var closable = page is not PinnedTabPage;
        AddMenuItem(menu, "Fechar aba", "BaixoGatilho", closable, () => Close(page));
        AddMenuItem(menu, "Fechar outras abas", "CimaGatilho", true, () => CloseOthers(page));
        AddMenuItem(menu, "Fechar abas à direita", "DireitaGatilho", true, () => CloseToTheRightOf(page));
        AddMenuItem(menu, "Fechar abas à esquerda", "EsquerdaGatilho", true, () => CloseToTheLeftOf(page));
        AddMenuItem(menu, "Fechar todas as abas", null, true, CloseAll);
        ThemeManager.ApplyDarkModeToContextMenuStrip(menu);

        // O menu so e descartado depois de o item clicado ser processado.
        menu.Closed += (_, _) =>
        {
            if (!_tabs.IsDisposed && _tabs.IsHandleCreated)
                _tabs.BeginInvoke(() =>
                {
                    if (ReferenceEquals(_contextMenu, menu))
                        _contextMenu = null;
                    menu.Dispose();
                });
        };

        _contextMenu = menu;
        menu.Show(_tabs, location);
    }

    private static void AddMenuItem(ContextMenuStrip menu, string text, string? imageName, bool enabled, Action action)
    {
        var item = new ToolStripMenuItem(text) { Enabled = enabled };
        if (imageName is not null && Properties.Resources.ResourceManager.GetObject(imageName) is Image image)
            item.Image = image;
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }
}
