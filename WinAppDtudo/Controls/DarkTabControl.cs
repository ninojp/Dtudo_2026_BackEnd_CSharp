using WinAppDtudo.Services;
using System.ComponentModel;

namespace WinAppDtudo.Controls;

public class DarkTabControl : TabControl
{
    private const int DefaultCloseButtonSize = 24;

    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ShowCloseButtons { get; set; }

    [DefaultValue(DefaultCloseButtonSize)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int CloseButtonSize { get; set; } = DefaultCloseButtonSize;

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
        DrawMode = TabDrawMode.OwnerDrawFixed;
    }

    public Rectangle GetCloseButtonBounds(int tabIndex)
    {
        var tabRect = GetTabRect(tabIndex);
        return new Rectangle(
            tabRect.Right - CloseButtonSize - 5,
            tabRect.Top + Math.Max(3, (tabRect.Height - CloseButtonSize) / 2),
            CloseButtonSize,
            CloseButtonSize);
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is TabPage tabPage)
        {
            ThemeManager.ApplyDarkModeToTabPage(tabPage);
            ApplySelectedTabPageBackColor();
        }
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        ApplySelectedTabPageBackColor();
        Invalidate();
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
        var foreground = selected ? DarkModeColors.TextColor : DarkModeColors.InactiveTabTextColor;

        using var backgroundBrush = new SolidBrush(background);
        graphics.FillRectangle(backgroundBrush, tabRect);

        var contentRect = Rectangle.Inflate(tabRect, -8, -4);
        PaintTabImage(graphics, tabPage, ref contentRect);

        if (ShowCloseButtons)
        {
            var closeRect = GetCloseButtonBounds(index);
            PaintCloseButton(graphics, closeRect, foreground);
            contentRect.Width = Math.Max(1, closeRect.Left - contentRect.Left - 6);
        }

        TextRenderer.DrawText(
            graphics,
            tabPage.Text,
            Font,
            contentRect,
            foreground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

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

        using var pen = new Pen(DarkModeColors.ActiveBorderColor);

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
            tabPage.ForeColor = DarkModeColors.TextColor;
        }
    }

    private void PaintTabImage(Graphics graphics, TabPage tabPage, ref Rectangle contentRect)
    {
        if (ImageList is null)
            return;

        var imageIndex = tabPage.ImageIndex;
        if (imageIndex < 0 || imageIndex >= ImageList.Images.Count)
            return;

        var imageSize = ImageList.ImageSize;
        var imageRect = new Rectangle(
            contentRect.Left,
            contentRect.Top + Math.Max(0, (contentRect.Height - imageSize.Height) / 2),
            imageSize.Width,
            imageSize.Height);

        ImageList.Draw(graphics, imageRect.Location, imageIndex);
        contentRect.X += imageSize.Width + 6;
        contentRect.Width = Math.Max(1, contentRect.Width - imageSize.Width - 6);
    }

    private static void PaintCloseButton(Graphics graphics, Rectangle closeRect, Color color)
    {
        using var pen = new Pen(color, 2F);
        const int margin = 6;
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
}
