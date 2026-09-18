using WinAppDtudo.FormsUC;
using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public sealed class Frm_Monitoramento : Form
{
    public Frm_Monitoramento(WinAppAuthenticationService authentication)
    {
        Text = "Monitoramento das colecoes locais";
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(800, 500);
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(new FUC_Monitoramento(authentication));
        ThemeManager.ApplyDarkModeToForm(this);
    }
}
