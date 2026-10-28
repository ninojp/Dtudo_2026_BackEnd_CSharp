using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

/// <summary>
/// Conteudo da aba "Nino T.I": por enquanto apenas a tela tematica (placeholder).
/// </summary>
public sealed class FUC_NinoTI : UserControl
{
    public FUC_NinoTI()
    {
        Name = "FUC_NinoTI";
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        BackColor = DarkModeColors.BackgroundColor;
        ForeColor = DarkModeColors.TextColor;
        BackgroundImage = Properties.Resources.OlhoBRHacker1024;
        BackgroundImageLayout = ImageLayout.Stretch;
    }
}
