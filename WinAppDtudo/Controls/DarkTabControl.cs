using WinAppDtudo.Services;
using System.ComponentModel;
using System.Drawing.Imaging;

namespace WinAppDtudo.Controls;

public class DarkTabControl : TabControl
{
    private const int DefaultCloseButtonSize = 24;
    private const string TabPageDragDataFormat = "WinAppDtudo.TabPage";
    private const int TabHeaderHorizontalInset = 8;
    private const int TabHeaderVerticalInset = 4;
    private const int TabImageTextSpacing = 6;
    private const int CloseButtonRightMargin = 5;
    private const int CloseButtonTextSpacing = 1;
    private const string MaxDetailTabHeaderText = "#12345";
    private bool _showCloseButtons;
    private int _closeButtonSize = DefaultCloseButtonSize;
    private bool _autoSizeTabHeaders;
    private bool _updatingHeaderLayout;
    private bool _headerLayoutUpdateQueued;
    private bool _allowTabReordering;
    private bool _reorderingTabs;
    private TabPage? _draggedTab;
    private Point _dragStart;
    private int _dropInsertionIndex = -1;

    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ShowCloseButtons
    {
        get => _showCloseButtons;
        set
        {
            if (_showCloseButtons == value)
                return;
            _showCloseButtons = value;
            UpdateHeaderLayout();
        }
    }

    [DefaultValue(DefaultCloseButtonSize)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int CloseButtonSize
    {
        get => _closeButtonSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            if (_closeButtonSize == value)
                return;
            _closeButtonSize = value;
            UpdateHeaderLayout();
        }
    }

    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool AutoSizeTabHeaders
    {
        get => _autoSizeTabHeaders;
        set
        {
            if (_autoSizeTabHeaders == value)
                return;
            _autoSizeTabHeaders = value;
            UpdateHeaderLayout();
        }
    }

    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool AllowTabReordering
    {
        get => _allowTabReordering;
        set
        {
            _allowTabReordering = value;
            AllowDrop = value;
            if (!value)
                ResetDragState();
        }
    }

    public DarkTabControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        BackColor = DarkModeColors.BackgroundColor;
        ForeColor = DarkModeColors.TextColor;
    }

    public Rectangle GetCloseButtonBounds(int tabIndex)
    {
        var tabRect = GetTabRect(tabIndex);
        var closeSize = LogicalToDeviceUnits(CloseButtonSize);
        return new Rectangle(
            tabRect.Right - closeSize - LogicalToDeviceUnits(CloseButtonRightMargin),
            tabRect.Top + Math.Max(0, (tabRect.Height - closeSize) / 2),
            closeSize,
            closeSize);
    }

    public void MoveTab(TabPage tabPage, int targetIndex)
    {
        ArgumentNullException.ThrowIfNull(tabPage);
        if (!TabPages.Contains(tabPage))
            throw new ArgumentException("The tab must belong to this control.", nameof(tabPage));
        ArgumentOutOfRangeException.ThrowIfNegative(targetIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(targetIndex, TabCount);
        if (TabPages.IndexOf(tabPage) == targetIndex)
            return;

        var selectedTab = SelectedTab;
        _reorderingTabs = true;
        SuspendLayout();
        try
        {
            TabPages.Remove(tabPage);
            TabPages.Insert(targetIndex, tabPage);
            SelectedTab = selectedTab;
        }
        finally
        {
            _reorderingTabs = false;
            ResumeLayout(true);
        }
        Invalidate();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        QueueHeaderLayoutUpdate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        StartupDiagnostics.Mark(
            $"DARKTAB_HANDLE_CREATED entering name={Name} dpi={DeviceDpi} parentHandleCreated={Parent?.IsHandleCreated.ToString() ?? "null"}");

        if (Parent is null || !Parent.IsHandleCreated)
        {
            var parentHandleCreated = Parent?.IsHandleCreated.ToString() ?? "null";
            StartupDiagnostics.Mark(
                $"DARKTAB_EARLY_HANDLE name={Name} dpi={DeviceDpi} parentHandleCreated={parentHandleCreated}{Environment.NewLine}{Environment.StackTrace}");
        }

        base.OnHandleCreated(e);
        StartupDiagnostics.Mark($"DARKTAB_HANDLE_CREATED base returned name={Name}; queueing header layout");
        QueueHeaderLayoutUpdate();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        QueueHeaderLayoutUpdate();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is TabPage tabPage)
        {
            tabPage.TextChanged += TabHeaderChanged;
            tabPage.ForeColorChanged += TabHeaderChanged;
            ThemeManager.ApplyDarkModeToTabPage(tabPage);
            ApplySelectedTabPageBackColor();
            UpdateHeaderLayout();
        }
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if (e.Control is TabPage tabPage)
        {
            tabPage.TextChanged -= TabHeaderChanged;
            tabPage.ForeColorChanged -= TabHeaderChanged;
            if (!_reorderingTabs && ReferenceEquals(tabPage, _draggedTab))
                ResetDragState();
        }
        base.OnControlRemoved(e);
        Invalidate();
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        if (!_reorderingTabs)
            base.OnSelectedIndexChanged(e);
        ApplySelectedTabPageBackColor();
        Invalidate();
    }

    protected override void OnSelected(TabControlEventArgs e)
    {
        if (!_reorderingTabs)
            base.OnSelected(e);
    }

    protected override void OnDeselected(TabControlEventArgs e)
    {
        if (!_reorderingTabs)
            base.OnDeselected(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        ResetDragState();
        var index = GetTabIndexAt(e.Location);
        var candidate = AllowTabReordering && e.Button == MouseButtons.Left && index >= 0 &&
            (!ShowCloseButtons || !GetCloseButtonBounds(index).Contains(e.Location))
                ? TabPages[index]
                : null;
        base.OnMouseDown(e);
        if (candidate is not null && TabPages.Contains(candidate))
        {
            _draggedTab = candidate;
            _dragStart = e.Location;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Button != MouseButtons.Left || _draggedTab is null || !TabPages.Contains(_draggedTab))
            return;

        var dragSize = SystemInformation.DragSize;
        var threshold = new Rectangle(
            _dragStart.X - dragSize.Width / 2,
            _dragStart.Y - dragSize.Height / 2,
            dragSize.Width,
            dragSize.Height);
        if (threshold.Contains(e.Location))
            return;

        try
        {
            var dragData = new DataObject();
            dragData.SetData(TabPageDragDataFormat, _draggedTab);
            DoDragDrop(dragData, DragDropEffects.Move);
        }
        finally
        {
            ResetDragState();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        ResetDragState();
        base.OnMouseUp(e);
    }

    protected override void OnDragEnter(DragEventArgs drgevent)
    {
        base.OnDragEnter(drgevent);
        UpdateDropTarget(drgevent);
    }

    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        UpdateDropTarget(drgevent);
    }

    protected override void OnDragLeave(EventArgs e)
    {
        _dropInsertionIndex = -1;
        Invalidate();
        base.OnDragLeave(e);
    }

    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        UpdateDropTarget(drgevent);
        if (_dropInsertionIndex >= 0 && GetDraggedTab(drgevent) is TabPage tabPage)
        {
            var oldIndex = TabPages.IndexOf(tabPage);
            var targetIndex = _dropInsertionIndex > oldIndex
                ? _dropInsertionIndex - 1
                : _dropInsertionIndex;
            MoveTab(tabPage, Math.Clamp(targetIndex, 0, TabCount - 1));
        }
        ResetDragState();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        using var brush = new SolidBrush(DarkModeColors.BackgroundColor);
        pevent.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(DarkModeColors.BackgroundColor);
        PaintPageArea(e.Graphics);

        for (var i = 0; i < TabPages.Count; i++)
            PaintTab(e.Graphics, i);

        PaintSelectedTabFrame(e.Graphics);
        PaintDropMarker(e.Graphics);
    }

    private void PaintPageArea(Graphics graphics)
    {
        var display = DisplayRectangle;
        if (display.Width <= 0 || display.Height <= 0)
            return;

        using var surfaceBrush = new SolidBrush(DarkModeColors.ActiveTabBackgroundColor);
        graphics.FillRectangle(surfaceBrush, display);
    }

    private void PaintTab(Graphics graphics, int index)
    {
        var tabPage = TabPages[index];
        var tabRect = GetTabRect(index);
        var selected = index == SelectedIndex;

        var background = selected ? DarkModeColors.ActiveTabBackgroundColor : DarkModeColors.BackgroundSecondaryColor;
        var foreground = GetTabForeground(tabPage, selected);

        using var backgroundBrush = new SolidBrush(background);
        graphics.FillRectangle(backgroundBrush, tabRect);

        var contentRect = Rectangle.Inflate(
            tabRect,
            -LogicalToDeviceUnits(TabHeaderHorizontalInset),
            -LogicalToDeviceUnits(TabHeaderVerticalInset));
        PaintTabImage(graphics, tabPage, ref contentRect);

        if (ShowCloseButtons)
        {
            var closeRect = GetCloseButtonBounds(index);
            PaintCloseButton(graphics, closeRect, foreground);
            contentRect.Width = Math.Max(
                1,
                closeRect.Left - contentRect.Left - LogicalToDeviceUnits(CloseButtonTextSpacing));
        }

        var text = tabPage is AccentTabPage { HeaderText: { } headerText }
            ? headerText
            : tabPage.Text;
        var textFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        if (tabPage is not AccentTabPage { HeaderText: not null })
            textFlags |= TextFormatFlags.EndEllipsis;

        TextRenderer.DrawText(
            graphics,
            text,
            Font,
            contentRect,
            foreground,
            textFlags);

        if (!selected)
        {
            using var borderPen = new Pen(DarkModeColors.BorderColor);
            graphics.DrawRectangle(borderPen, tabRect.X, tabRect.Y, tabRect.Width - 1, tabRect.Height - 1);
        }
    }

    private void PaintSelectedTabFrame(Graphics graphics)
    {
        if (SelectedIndex < 0 || SelectedIndex >= TabPages.Count)
            return;

        var display = DisplayRectangle;
        var tabRect = GetTabRect(SelectedIndex);
        if (display.Width <= 0 || display.Height <= 0 || tabRect.Width <= 0 || tabRect.Height <= 0)
            return;

        using var pen = new Pen(TabPages[SelectedIndex] is IThemeAccentProvider provider
            ? provider.AccentColor
            : DarkModeColors.ActiveBorderColor);

        var left = display.Left;
        var right = display.Right - 1;
        var bottom = display.Bottom - 1;
        var top = display.Top;
        var tabLeft = tabRect.Left;
        var tabRight = tabRect.Right - 1;
        var tabTop = tabRect.Top;

        var points = new[]
        {
            new Point(tabLeft, tabTop),
            new Point(tabRight, tabTop),
            new Point(tabRight, top),
            new Point(right, top),
            new Point(right, bottom),
            new Point(left, bottom),
            new Point(left, top),
            new Point(tabLeft, top),
            new Point(tabLeft, tabTop)
        };

        graphics.DrawLines(pen, points);
    }

    private void ApplySelectedTabPageBackColor()
    {
        foreach (TabPage tabPage in TabPages)
        {
            tabPage.UseVisualStyleBackColor = false;
            tabPage.BackColor = DarkModeColors.ActiveTabBackgroundColor;
            tabPage.ForeColor = tabPage is IThemeAccentProvider provider
                ? provider.AccentColor
                : DarkModeColors.TextColor;
        }
    }

    private void PaintTabImage(Graphics graphics, TabPage tabPage, ref Rectangle contentRect)
    {
        if (ImageList is null)
            return;

        var imageIndex = string.IsNullOrEmpty(tabPage.ImageKey)
            ? tabPage.ImageIndex
            : ImageList.Images.IndexOfKey(tabPage.ImageKey);
        if (imageIndex < 0 || imageIndex >= ImageList.Images.Count)
            return;

        var imageSize = ImageList.ImageSize;
        var imageRect = new Rectangle(
            contentRect.Left,
            contentRect.Top + Math.Max(0, (contentRect.Height - imageSize.Height) / 2),
            imageSize.Width,
            imageSize.Height);

        if (tabPage is IThemeAccentProvider provider)
        {
            using var image = ImageList.Images[imageIndex];
            using var attributes = new ImageAttributes();
            var color = provider.AccentColor;
            var red = color.R / 255F;
            var green = color.G / 255F;
            var blue = color.B / 255F;
            // Preserve luminance so opaque logos keep their lettering and internal details.
            attributes.SetColorMatrix(new ColorMatrix
            {
                Matrix00 = 0.2126F * red,
                Matrix01 = 0.2126F * green,
                Matrix02 = 0.2126F * blue,
                Matrix10 = 0.7152F * red,
                Matrix11 = 0.7152F * green,
                Matrix12 = 0.7152F * blue,
                Matrix20 = 0.0722F * red,
                Matrix21 = 0.0722F * green,
                Matrix22 = 0.0722F * blue
            });
            graphics.DrawImage(image, imageRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
        }
        else
        {
            ImageList.Draw(graphics, imageRect.Location, imageIndex);
        }
        var imageSpacing = LogicalToDeviceUnits(TabImageTextSpacing);
        contentRect.X += imageSize.Width + imageSpacing;
        contentRect.Width = Math.Max(1, contentRect.Width - imageSize.Width - imageSpacing);
    }

    private void PaintCloseButton(Graphics graphics, Rectangle closeRect, Color color)
    {
        using var pen = new Pen(color, LogicalToDeviceUnits(1));
        var margin = Math.Min(LogicalToDeviceUnits(9), closeRect.Width / 3);
        graphics.DrawLine(
            pen,
            closeRect.Left + margin,
            closeRect.Top + margin,
            closeRect.Right - margin - 1,
            closeRect.Bottom - margin - 1);
        graphics.DrawLine(
            pen,
            closeRect.Right - margin - 1,
            closeRect.Top + margin,
            closeRect.Left + margin,
            closeRect.Bottom - margin - 1);
    }

    private void UpdateHeaderLayout()
    {
        if (_updatingHeaderLayout || IsDisposed || Disposing ||
            !IsHandleCreated || Parent is null || !Parent.IsHandleCreated)
            return;

        _updatingHeaderLayout = true;
        try
        {
            if (AutoSizeTabHeaders)
            {
                var horizontalSpace = LogicalToDeviceUnits(TabHeaderHorizontalInset);
                if (ShowCloseButtons)
                {
                    horizontalSpace += LogicalToDeviceUnits(CloseButtonSize) +
                        LogicalToDeviceUnits(CloseButtonRightMargin) +
                        LogicalToDeviceUnits(CloseButtonTextSpacing);
                }
                else
                {
                    horizontalSpace += LogicalToDeviceUnits(TabHeaderHorizontalInset + TabImageTextSpacing);
                }
                var padding = new Point((horizontalSpace + 1) / 2, LogicalToDeviceUnits(6));
                if (Padding != padding)
                    Padding = padding;

                var contentHeight = Math.Max(Font.Height, ImageList?.ImageSize.Height ?? 0);
                if (ShowCloseButtons)
                    contentHeight = Math.Max(contentHeight, LogicalToDeviceUnits(CloseButtonSize));
                var currentItemSize = ItemSize;
                var itemSize = new Size(currentItemSize.Width,
                    Math.Max(currentItemSize.Height, contentHeight + LogicalToDeviceUnits(12)));
                if (currentItemSize != itemSize)
                    ItemSize = itemSize;

                if (SizeMode != TabSizeMode.Normal)
                    SizeMode = TabSizeMode.Normal;

                var detailTabs = TabPages
                    .Cast<TabPage>()
                    .OfType<AccentTabPage>()
                    .Where(page => page.HeaderText is not null)
                    .ToArray();
                if (detailTabs.Length > 0)
                    ResizeDetailTabHeaders(detailTabs);
            }
            Invalidate();
        }
        finally
        {
            _updatingHeaderLayout = false;
        }
    }

    private void ResizeDetailTabHeaders(IReadOnlyList<AccentTabPage> detailTabs)
    {
        using var displayGraphics = CreateGraphics();
        var displayedWidth = TextRenderer.MeasureText(
            displayGraphics,
            MaxDetailTabHeaderText,
            Font,
            Size.Empty,
            TextFormatFlags.NoPadding).Width;

        var sizingPage = detailTabs[0];
        var sizingText = sizingPage.Text.StartsWith(MaxDetailTabHeaderText, StringComparison.Ordinal)
            ? sizingPage.Text
            : MaxDetailTabHeaderText;
        if (!string.Equals(sizingPage.Text, sizingText, StringComparison.Ordinal))
            sizingPage.Text = sizingText;

        var availableWidth = GetDetailTabHeaderTextCapacity(sizingPage);
        while (availableWidth < displayedWidth)
        {
            sizingText += " ";
            sizingPage.Text = sizingText;
            availableWidth = GetDetailTabHeaderTextCapacity(sizingPage);
        }

        while (sizingText.Length > MaxDetailTabHeaderText.Length)
        {
            var shorterText = sizingText[..^1];
            sizingPage.Text = shorterText;
            var shorterWidth = GetDetailTabHeaderTextCapacity(sizingPage);
            if (shorterWidth < displayedWidth)
            {
                sizingPage.Text = sizingText;
                break;
            }

            sizingText = shorterText;
            availableWidth = shorterWidth;
        }

        for (var index = 1; index < detailTabs.Count; index++)
        {
            var detailPage = detailTabs[index];
            if (!string.Equals(detailPage.Text, sizingText, StringComparison.Ordinal))
                detailPage.Text = sizingText;
        }
    }

    private int GetDetailTabHeaderTextCapacity(AccentTabPage detailPage)
    {
        var tabIndex = TabPages.IndexOf(detailPage);
        var tabRect = GetTabRect(tabIndex);
        var textLeft = tabRect.Left + LogicalToDeviceUnits(TabHeaderHorizontalInset);

        if (ShowCloseButtons)
        {
            var closeRect = GetCloseButtonBounds(tabIndex);
            return Math.Max(
                1,
                closeRect.Left - textLeft - LogicalToDeviceUnits(CloseButtonTextSpacing));
        }

        return Math.Max(
            1,
            tabRect.Right - textLeft - LogicalToDeviceUnits(TabHeaderHorizontalInset));
    }

    private void QueueHeaderLayoutUpdate()
    {
        if (_headerLayoutUpdateQueued || IsDisposed || !IsHandleCreated ||
            Parent is null || !Parent.IsHandleCreated)
            return;

        _headerLayoutUpdateQueued = true;
        BeginInvoke(new Action(() =>
        {
            _headerLayoutUpdateQueued = false;
            if (IsDisposed || !IsHandleCreated)
                return;

            StartupDiagnostics.Mark($"DARKTAB_HEADER_LAYOUT entering name={Name} dpi={DeviceDpi}");
            try
            {
                UpdateHeaderLayout();
                StartupDiagnostics.Mark($"DARKTAB_HEADER_LAYOUT returned name={Name}");
            }
            catch (Exception exception)
            {
                StartupDiagnostics.Record("DarkTabControl deferred header layout", exception);
                throw;
            }
        }));
    }

    private void TabHeaderChanged(object? sender, EventArgs e) => UpdateHeaderLayout();

    private static Color GetTabForeground(TabPage tabPage, bool selected) =>
        tabPage is IThemeAccentProvider provider
            ? provider.AccentColor
            : selected ? DarkModeColors.TextColor : DarkModeColors.InactiveTabTextColor;

    private int GetTabIndexAt(Point location)
    {
        for (var index = 0; index < TabCount; index++)
        {
            if (GetTabRect(index).Contains(location))
                return index;
        }
        return -1;
    }

    private TabPage? GetDraggedTab(DragEventArgs e)
    {
        if (!AllowTabReordering ||
            e.Data is null ||
            (e.AllowedEffect & DragDropEffects.Move) == 0)
        {
            return null;
        }

        var tabPage = e.Data.GetData(TabPageDragDataFormat) as TabPage;
        return tabPage is not null && TabPages.Contains(tabPage)
            ? tabPage
            : null;
    }

    private void UpdateDropTarget(DragEventArgs e)
    {
        _dropInsertionIndex = -1;
        e.Effect = DragDropEffects.None;
        if (GetDraggedTab(e) is not null)
        {
            var point = PointToClient(new Point(e.X, e.Y));
            if (ClientRectangle.Contains(point))
            {
                for (var index = 0; index < TabCount; index++)
                {
                    var rect = GetTabRect(index);
                    if (point.Y < rect.Top || point.Y >= rect.Bottom || rect.Right <= 0 || rect.Left >= ClientSize.Width)
                        continue;
                    _dropInsertionIndex = point.X < rect.Left + rect.Width / 2 ? index : index + 1;
                    if (point.X < rect.Right)
                        break;
                }
            }
            if (_dropInsertionIndex >= 0)
                e.Effect = DragDropEffects.Move;
        }
        Invalidate();
    }

    private void PaintDropMarker(Graphics graphics)
    {
        if (_dropInsertionIndex < 0 || TabCount == 0)
            return;

        var tabRect = GetTabRect(Math.Min(_dropInsertionIndex, TabCount - 1));
        var x = _dropInsertionIndex == TabCount ? tabRect.Right : tabRect.Left;
        using var pen = new Pen(
            _draggedTab is IThemeAccentProvider provider ? provider.AccentColor : DarkModeColors.TextColor,
            LogicalToDeviceUnits(3));
        graphics.DrawLine(pen, x, tabRect.Top, x, tabRect.Bottom);
    }

    private void ResetDragState()
    {
        _draggedTab = null;
        _dropInsertionIndex = -1;
        Invalidate();
    }
}
