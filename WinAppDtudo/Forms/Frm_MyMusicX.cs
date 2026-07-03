using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public partial class Frm_MyMusicX : CustomFormNoBorder
{
    public Frm_MyMusicX()
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
    //Menu Windows - Exibir janelas em cascata.
    private void CascataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.LayoutMdi(MdiLayout.Cascade);
    }
    //Menu Windows - Exibir janelas na horizontal.
    private void HorizontalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.LayoutMdi(MdiLayout.TileHorizontal);
    }
    //Menu Windows - Exibir janelas em vertical.
    private void VerticalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.LayoutMdi(MdiLayout.TileVertical);
    }

    private void FormTestToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Frm_FormTest formTest = new();
        formTest.MdiParent = this;
        formTest.Show();
    }

    private void FormHelloWorldToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Frm_HelloWorld formHelloWorld = new();
        formHelloWorld.MdiParent = this;
        formHelloWorld.Show();
    }
}
