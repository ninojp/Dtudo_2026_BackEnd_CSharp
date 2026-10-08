namespace WinAppDtudo.Controls;

public sealed class AnimeDetailsContentPanel : Panel
{
    private abstract record ContentItem;
    private sealed record DetailItem(Label Field, Label Value) : ContentItem;
    private sealed record SectionItem(Control Control, int SpaceBefore, int SpaceAfter) : ContentItem;

    private readonly List<ContentItem> _items = [];
    private bool _arranging;

    public void ClearContent()
    {
        _items.Clear();
        foreach (var control in Controls.Cast<Control>().ToArray())
            control.Dispose();
    }

    public void AddDetail(Label field, Label value)
    {
        _items.Add(new DetailItem(field, value));
        Controls.Add(field);
        Controls.Add(value);
    }

    public void AddSection(Control control, int spaceBefore = 0, int spaceAfter = 12)
    {
        _items.Add(new SectionItem(control, spaceBefore, spaceAfter));
        Controls.Add(control);
    }

    public int ArrangeContent(int width)
    {
        if (_arranging)
            return Height;

        _arranging = true;
        try
        {
            Width = Math.Max(1, width);
            var availableWidth = Math.Max(1, ClientSize.Width - Padding.Horizontal);
            var columnGap = LogicalToDeviceUnits(16);
            var columns = availableWidth >= LogicalToDeviceUnits(720) ? 2 : 1;
            var columnWidth = Math.Max(1, (availableWidth - columnGap * (columns - 1)) / columns);
            var y = Padding.Top;

            for (var index = 0; index < _items.Count;)
            {
                if (_items[index] is SectionItem section)
                {
                    y += LogicalToDeviceUnits(section.SpaceBefore);
                    ArrangeSection(section.Control, Padding.Left, y, availableWidth);
                    y += section.Control.Height + LogicalToDeviceUnits(section.SpaceAfter);
                    index++;
                    continue;
                }

                var row = new List<DetailItem>(columns);
                while (index < _items.Count && row.Count < columns && _items[index] is DetailItem detail)
                {
                    row.Add(detail);
                    index++;
                }

                var fieldWidth = Math.Max(1, Math.Min(LogicalToDeviceUnits(150), columnWidth / 3));
                var valueGap = Math.Min(LogicalToDeviceUnits(8), Math.Max(0, columnWidth / 12));
                var valueWidth = Math.Max(1, columnWidth - fieldWidth - valueGap);
                var rowHeight = LogicalToDeviceUnits(34);
                foreach (var detail in row)
                {
                    rowHeight = Math.Max(rowHeight, Math.Max(
                        MeasureLabelHeight(detail.Field, fieldWidth),
                        MeasureLabelHeight(detail.Value, valueWidth)));
                }

                for (var column = 0; column < row.Count; column++)
                {
                    var x = Padding.Left + column * (columnWidth + columnGap);
                    row[column].Field.SetBounds(x, y, fieldWidth, rowHeight);
                    row[column].Value.SetBounds(x + fieldWidth + valueGap, y, valueWidth, rowHeight);
                }
                y += rowHeight + LogicalToDeviceUnits(12);
            }

            Height = y + Padding.Bottom;
            return Height;
        }
        finally
        {
            _arranging = false;
        }
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        ArrangeContent(Width);
        base.OnLayout(levent);
    }

    private void ArrangeSection(Control control, int x, int y, int width)
    {
        control.SetBounds(x, y, width, control.Height);
        switch (control)
        {
            case Label label:
                label.Height = Math.Max(label.MinimumSize.Height, MeasureLabelHeight(label, width));
                break;

            case FlowLayoutPanel cards:
                cards.AutoScroll = false;
                cards.AutoSize = false;
                foreach (Control card in cards.Controls)
                {
                    var availableWidth = Math.Max(1, width - cards.Padding.Horizontal - card.Margin.Horizontal);
                    card.Size = card.GetPreferredSize(new Size(availableWidth, 0));
                }
                cards.PerformLayout();
                cards.Height = Math.Max(
                    cards.GetPreferredSize(new Size(width, 0)).Height,
                    cards.Controls.Cast<Control>().Select(card => card.Bottom + card.Margin.Bottom + cards.Padding.Bottom)
                        .DefaultIfEmpty(cards.Padding.Vertical).Max());
                cards.PerformLayout();
                break;
        }
    }

    private int MeasureLabelHeight(Label label, int width) =>
        Math.Max(label.Font.Height, TextRenderer.MeasureText(
            label.Text,
            label.Font,
            new Size(Math.Max(1, width - label.Padding.Horizontal), int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height) +
        label.Padding.Vertical + LogicalToDeviceUnits(4);
}
