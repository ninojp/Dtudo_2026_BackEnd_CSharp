using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public partial class Frm_NinoTI : CustomFormNoBorder
{
    public Frm_NinoTI()
    {
        InitializeComponent();
        // Aplicar o tema Dark Mode ao formulário e seus componentes
        ThemeManager.ApplyDarkModeToForm(this);
        // Inicializa o formulário customizado sem barra de título
        MenuStrip? menuStrip = this.Controls.OfType<MenuStrip>().FirstOrDefault();
        if (menuStrip != null)
        {
            InitializeCustomFormNoBorder(menuStrip);
            AddControlButtonsToMenuStrip(menuStrip);
        }
        else
        {
            InitializeCustomFormNoBorder();
        }
    }
}
