using WinAppDtudo.Controls;

namespace WinAppDtudo.Services;

/// <summary>
/// Aplica o tema escuro global do WinAppDtudo em formularios, controles e itens criados em runtime.
/// </summary>
public static class ThemeManager
{
    private static readonly DarkToolStripRenderer ToolStripRenderer = new();
    private static bool _isDarkModeEnabled = true;

    public static void Initialize()
    {
        _isDarkModeEnabled = true;
        ToolStripManager.Renderer = ToolStripRenderer;
    }

    public static bool IsDarkModeEnabled => _isDarkModeEnabled;

    public static void ApplyDarkModeToForm(Form form)
    {
        if (!_isDarkModeEnabled)
            return;

        WindowsDarkMode.ApplyTo(form);
        form.BackColor = DarkModeColors.BackgroundColor;
        form.ForeColor = DarkModeColors.TextColor;
        form.TransparencyKey = Color.Empty;

        HookDynamicChildren(form);
        ApplyDarkModeToControls(form.Controls);
    }

    public static void ApplyDarkModeToUserControl(UserControl userControl)
    {
        if (!_isDarkModeEnabled)
            return;

        WindowsDarkMode.ApplyTo(userControl);
        userControl.BackColor = DarkModeColors.BackgroundColor;
        userControl.ForeColor = DarkModeColors.TextColor;
        HookDynamicChildren(userControl);
        ApplyDarkModeToControls(userControl.Controls);
    }

    public static void ApplyDarkModeToControl(Control control)
    {
        if (!_isDarkModeEnabled || control.IsDisposed)
            return;

        WindowsDarkMode.ApplyTo(control);
        HookDynamicChildren(control);

        switch (control)
        {
            case MenuStrip menuStrip:
                ApplyDarkModeToToolStrip(menuStrip);
                return;

            case ContextMenuStrip contextMenuStrip:
                ApplyDarkModeToContextMenuStrip(contextMenuStrip);
                return;

            case StatusStrip statusStrip:
                ApplyDarkModeToToolStrip(statusStrip);
                return;

            case ToolStrip toolStrip:
                ApplyDarkModeToToolStrip(toolStrip);
                return;

            case DarkTabControl darkTabControl:
                ApplyDarkModeToTabControl(darkTabControl);
                return;

            case TabControl tabControl:
                ApplyDarkModeToTabControl(tabControl);
                return;

            case TabPage tabPage:
                ApplyDarkModeToTabPage(tabPage);
                return;

            case DataGridView dataGridView:
                ApplyDarkModeToDataGridView(dataGridView);
                return;

            case TextBox textBox:
                ApplyTextBox(textBox);
                return;

            case RichTextBox richTextBox:
                ApplyRichTextBox(richTextBox);
                return;

            case ComboBox comboBox:
                ApplyComboBox(comboBox);
                return;

            case Button button:
                ApplyButton(button);
                return;

            case LinkLabel linkLabel:
                ApplyLinkLabel(linkLabel);
                return;

            case Label label:
                ApplyLabel(label);
                return;

            case ListBox listBox:
                listBox.BackColor = DarkModeColors.BackgroundColor;
                listBox.ForeColor = DarkModeColors.TextColor;
                listBox.BorderStyle = BorderStyle.FixedSingle;
                return;

            case ListView listView:
                listView.BackColor = DarkModeColors.BackgroundColor;
                listView.ForeColor = DarkModeColors.TextColor;
                listView.BorderStyle = BorderStyle.FixedSingle;
                return;

            case TreeView treeView:
                treeView.BackColor = DarkModeColors.BackgroundColor;
                treeView.ForeColor = DarkModeColors.TextColor;
                treeView.LineColor = DarkModeColors.BorderColor;
                return;

            case PictureBox pictureBox:
                ApplyPictureBox(pictureBox);
                return;

            case CheckBox or RadioButton:
                control.BackColor = Color.Transparent;
                control.ForeColor = DarkModeColors.TextColor;
                return;

            case Panel or FlowLayoutPanel or TableLayoutPanel:
                control.BackColor = DarkModeColors.BackgroundColor;
                control.ForeColor = DarkModeColors.TextColor;
                return;

            case GroupBox:
                control.BackColor = DarkModeColors.BackgroundColor;
                control.ForeColor = DarkModeColors.TextColor;
                return;
        }

        control.BackColor = DarkModeColors.BackgroundColor;
        control.ForeColor = DarkModeColors.TextColor;
    }

    public static void ApplyDarkModeToTabPage(TabPage tabPage)
    {
        tabPage.UseVisualStyleBackColor = false;
        tabPage.BackColor = DarkModeColors.ActiveTabBackgroundColor;
        tabPage.ForeColor = DarkModeColors.TextColor;
        HookDynamicChildren(tabPage);
        ApplyDarkModeToControls(tabPage.Controls);
        ApplyActiveTabSurfaceToContent(tabPage);
    }

    public static void ApplyDarkModeToContextMenuStrip(ContextMenuStrip contextMenuStrip)
    {
        contextMenuStrip.Renderer = ToolStripRenderer;
        contextMenuStrip.BackColor = DarkModeColors.BackgroundSecondaryColor;
        contextMenuStrip.ForeColor = DarkModeColors.TextColor;
        ApplyDarkModeToToolStripItems(contextMenuStrip.Items);
    }

    public static Color GetThemeColor(ThemeColorType colorType)
    {
        return colorType switch
        {
            ThemeColorType.Background => DarkModeColors.BackgroundColor,
            ThemeColorType.Surface => DarkModeColors.SurfaceColor,
            ThemeColorType.BackgroundSecondary => DarkModeColors.BackgroundSecondaryColor,
            ThemeColorType.Elevated => DarkModeColors.ElevatedColor,
            ThemeColorType.Text => DarkModeColors.TextColor,
            ThemeColorType.TextSecondary => DarkModeColors.TextSecondaryColor,
            ThemeColorType.Border => DarkModeColors.BorderColor,
            ThemeColorType.ActiveBorder => DarkModeColors.ActiveBorderColor,
            ThemeColorType.Accent => DarkModeColors.AccentColor,
            ThemeColorType.Hover => DarkModeColors.HoverColor,
            ThemeColorType.Selection => DarkModeColors.SelectionColor,
            ThemeColorType.Disabled => DarkModeColors.DisabledColor,
            ThemeColorType.Success => DarkModeColors.SuccessColor,
            ThemeColorType.Error => DarkModeColors.ErrorColor,
            ThemeColorType.Warning => DarkModeColors.WarningColor,
            ThemeColorType.Info => DarkModeColors.InfoColor,
            _ => DarkModeColors.TextColor,
        };
    }

    private static void ApplyDarkModeToControls(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            ApplyDarkModeToControl(control);

            if (control.HasChildren)
                ApplyDarkModeToControls(control.Controls);
        }
    }

    private static void ApplyDarkModeToTabControl(TabControl tabControl)
    {
        tabControl.BackColor = DarkModeColors.BackgroundColor;
        tabControl.ForeColor = DarkModeColors.TextColor;
        tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;

        if (tabControl is not DarkTabControl)
        {
            tabControl.DrawItem -= DrawDarkTabItem;
            tabControl.DrawItem += DrawDarkTabItem;
        }

        foreach (TabPage tabPage in tabControl.TabPages)
            ApplyDarkModeToTabPage(tabPage);
    }

    private static void DrawDarkTabItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tabControl || e.Index < 0 || e.Index >= tabControl.TabPages.Count)
            return;

        var tabPage = tabControl.TabPages[e.Index];
        var tabRect = tabControl.GetTabRect(e.Index);
        var selected = e.Index == tabControl.SelectedIndex;

        using var brush = new SolidBrush(selected ? DarkModeColors.ActiveTabBackgroundColor : DarkModeColors.BackgroundSecondaryColor);
        e.Graphics.FillRectangle(brush, tabRect);

        TextRenderer.DrawText(
            e.Graphics,
            tabPage.Text,
            tabControl.Font,
            Rectangle.Inflate(tabRect, -8, -4),
            selected ? DarkModeColors.TextColor : DarkModeColors.InactiveTabTextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        using var pen = new Pen(selected ? DarkModeColors.ActiveBorderColor : DarkModeColors.BorderColor);
        e.Graphics.DrawRectangle(pen, tabRect.X, tabRect.Y, tabRect.Width - 1, tabRect.Height - 1);
    }

    private static void ApplyDarkModeToToolStrip(ToolStrip toolStrip)
    {
        toolStrip.Renderer = ToolStripRenderer;
        toolStrip.BackColor = DarkModeColors.BackgroundSecondaryColor;
        toolStrip.ForeColor = DarkModeColors.TextColor;
        ApplyDarkModeToToolStripItems(toolStrip.Items);
    }

    private static void ApplyDarkModeToToolStripItems(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.BackColor = DarkModeColors.BackgroundSecondaryColor;
            item.ForeColor = item.Enabled ? DarkModeColors.TextColor : DarkModeColors.DisabledTextColor;

            if (item is ToolStripMenuItem menuItem)
            {
                menuItem.DropDown.BackColor = DarkModeColors.BackgroundSecondaryColor;
                menuItem.DropDown.ForeColor = DarkModeColors.TextColor;
                menuItem.DropDown.Renderer = ToolStripRenderer;
                ApplyDarkModeToToolStripItems(menuItem.DropDownItems);
            }
        }
    }

    private static void ApplyTextBox(TextBox textBox)
    {
        textBox.BackColor = DarkModeColors.SurfaceColor;
        textBox.ForeColor = DarkModeColors.TextColor;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private static void ApplyRichTextBox(RichTextBox richTextBox)
    {
        richTextBox.BackColor = DarkModeColors.SurfaceColor;
        richTextBox.ForeColor = DarkModeColors.TextColor;
        richTextBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private static void ApplyComboBox(ComboBox comboBox)
    {
        comboBox.BackColor = DarkModeColors.SurfaceColor;
        comboBox.ForeColor = DarkModeColors.TextColor;
        comboBox.FlatStyle = FlatStyle.Flat;
    }

    private static void ApplyButton(Button button)
    {
        var isImageOnly = string.IsNullOrWhiteSpace(button.Text)
            && (button.Image is not null || button.BackgroundImage is not null);

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = isImageOnly ? DarkModeColors.BackgroundColor : DarkModeColors.ActiveBorderColor;
        button.FlatAppearance.MouseOverBackColor = DarkModeColors.HoverColor;
        button.FlatAppearance.MouseDownBackColor = DarkModeColors.SelectionColor;
        button.UseVisualStyleBackColor = false;
        button.ForeColor = Color.White;
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.BackColor = isImageOnly ? Color.Transparent : DarkModeColors.AccentColor;
    }

    private static void ApplyLabel(Label label)
    {
        label.BackColor = Color.Transparent;
        label.ForeColor = label.Enabled ? DarkModeColors.TextColor : DarkModeColors.DisabledTextColor;
    }

    private static void ApplyLinkLabel(LinkLabel linkLabel)
    {
        linkLabel.BackColor = Color.Transparent;
        linkLabel.ForeColor = DarkModeColors.TextColor;
        linkLabel.LinkColor = DarkModeColors.TextColor;
        linkLabel.ActiveLinkColor = DarkModeColors.SelectionColor;
        linkLabel.VisitedLinkColor = DarkModeColors.TextSecondaryColor;
    }

    private static void ApplyPictureBox(PictureBox pictureBox)
    {
        pictureBox.BackColor = Color.Transparent;
        pictureBox.ForeColor = DarkModeColors.TextColor;
    }

    private static void ApplyDarkModeToDataGridView(DataGridView dgv)
    {
        dgv.BackgroundColor = DarkModeColors.BackgroundColor;
        dgv.BackColor = DarkModeColors.BackgroundColor;
        dgv.ForeColor = DarkModeColors.TextColor;
        dgv.GridColor = DarkModeColors.BorderColor;
        dgv.BorderStyle = BorderStyle.FixedSingle;
        dgv.EnableHeadersVisualStyles = false;

        dgv.DefaultCellStyle.BackColor = DarkModeColors.SurfaceColor;
        dgv.DefaultCellStyle.ForeColor = DarkModeColors.TextColor;
        dgv.DefaultCellStyle.SelectionBackColor = DarkModeColors.SelectionColor;
        dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
        dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

        dgv.AlternatingRowsDefaultCellStyle.BackColor = DarkModeColors.BackgroundSecondaryColor;
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = DarkModeColors.TextColor;
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = DarkModeColors.SelectionColor;
        dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.Black;

        dgv.ColumnHeadersDefaultCellStyle.BackColor = DarkModeColors.ElevatedColor;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = DarkModeColors.TextColor;
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = DarkModeColors.HoverColor;
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = DarkModeColors.TextColor;

        dgv.RowHeadersDefaultCellStyle.BackColor = DarkModeColors.ElevatedColor;
        dgv.RowHeadersDefaultCellStyle.ForeColor = DarkModeColors.TextColor;
        dgv.RowHeadersDefaultCellStyle.SelectionBackColor = DarkModeColors.HoverColor;
        dgv.RowHeadersDefaultCellStyle.SelectionForeColor = DarkModeColors.TextColor;
    }

    private static void ApplyActiveTabSurfaceToContent(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (ShouldUseActiveTabSurface(control))
                control.BackColor = DarkModeColors.ActiveTabBackgroundColor;

            if (control.HasChildren)
                ApplyActiveTabSurfaceToContent(control);
        }
    }

    private static bool ShouldUseActiveTabSurface(Control control)
    {
        return control is UserControl
            or Panel
            or FlowLayoutPanel
            or TableLayoutPanel
            or GroupBox;
    }

    private static void HookDynamicChildren(Control control)
    {
        control.ControlAdded -= ControlAdded;
        control.ControlAdded += ControlAdded;
    }

    private static void ControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is null)
            return;

        ApplyDarkModeToControl(e.Control);

        if (e.Control.HasChildren)
            ApplyDarkModeToControls(e.Control.Controls);
    }
}

public enum ThemeColorType
{
    Background,
    Surface,
    BackgroundSecondary,
    Elevated,
    Text,
    TextSecondary,
    Border,
    ActiveBorder,
    Accent,
    Hover,
    Selection,
    Disabled,
    Success,
    Error,
    Warning,
    Info
}
