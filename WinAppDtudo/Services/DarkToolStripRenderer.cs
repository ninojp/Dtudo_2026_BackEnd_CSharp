namespace WinAppDtudo.Services;

public sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer()
        : base(new DarkToolStripColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(DarkModeColors.BackgroundSecondaryColor);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var selected = e.Item.Selected || e.Item.Pressed;
        var bounds = new Rectangle(Point.Empty, e.Item.Size);
        using var brush = new SolidBrush(selected ? DarkModeColors.HoverColor : DarkModeColors.BackgroundSecondaryColor);
        e.Graphics.FillRectangle(brush, bounds);

        if (selected)
        {
            using var pen = new Pen(DarkModeColors.ActiveBorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, bounds.Width - 1, bounds.Height - 1);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? DarkModeColors.TextColor : DarkModeColors.DisabledTextColor;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(DarkModeColors.BorderColor);
        e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 4, y);
    }
}
