using System.ComponentModel;

namespace WinAppDtudo.Controls;

public sealed class AccentTabPage(Color accentColor) : TabPage, IThemeAccentProvider
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor { get; } = accentColor;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? HeaderText { get; set; }
}
