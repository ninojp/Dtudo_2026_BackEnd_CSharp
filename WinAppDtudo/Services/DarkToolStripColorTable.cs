namespace WinAppDtudo.Services;

public sealed class DarkToolStripColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => DarkModeColors.BackgroundSecondaryColor;
    public override Color ImageMarginGradientBegin => DarkModeColors.BackgroundSecondaryColor;
    public override Color ImageMarginGradientMiddle => DarkModeColors.BackgroundSecondaryColor;
    public override Color ImageMarginGradientEnd => DarkModeColors.BackgroundSecondaryColor;
    public override Color MenuBorder => DarkModeColors.ActiveBorderColor;
    public override Color MenuItemBorder => DarkModeColors.ActiveBorderColor;
    public override Color MenuItemSelected => DarkModeColors.HoverColor;
    public override Color MenuItemSelectedGradientBegin => DarkModeColors.HoverColor;
    public override Color MenuItemSelectedGradientEnd => DarkModeColors.HoverColor;
    public override Color MenuItemPressedGradientBegin => DarkModeColors.ElevatedColor;
    public override Color MenuItemPressedGradientMiddle => DarkModeColors.ElevatedColor;
    public override Color MenuItemPressedGradientEnd => DarkModeColors.ElevatedColor;
    public override Color SeparatorDark => DarkModeColors.BorderColor;
    public override Color SeparatorLight => DarkModeColors.ActiveBorderColor;
    public override Color ToolStripBorder => DarkModeColors.BorderColor;
    public override Color ToolStripGradientBegin => DarkModeColors.BackgroundSecondaryColor;
    public override Color ToolStripGradientMiddle => DarkModeColors.BackgroundSecondaryColor;
    public override Color ToolStripGradientEnd => DarkModeColors.BackgroundSecondaryColor;
    public override Color StatusStripGradientBegin => DarkModeColors.BackgroundSecondaryColor;
    public override Color StatusStripGradientEnd => DarkModeColors.BackgroundSecondaryColor;
    public override Color ButtonSelectedHighlight => DarkModeColors.HoverColor;
    public override Color ButtonPressedHighlight => DarkModeColors.SelectionColor;
    public override Color ButtonCheckedHighlight => DarkModeColors.ElevatedColor;
}
