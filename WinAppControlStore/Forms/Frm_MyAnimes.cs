namespace WinAppControlStore;

public partial class Frm_MyAnimes : Form
{
    public Frm_MyAnimes()
    {
        InitializeComponent();
    }
    private void AbaMascarasToolStripMenuItem_Click(object sender, EventArgs e)
    {
        FUC_Mascaras ucMascaras = new()
        {
            Dock = DockStyle.Fill
        };
        TabPage tabPage = new()
        {
            Text = "Máscaras",
            Name = "tabMascaras",
            ImageIndex = 3,
        };
        tabPage.Controls.Add(ucMascaras);
        Tbc_MyAnimes.TabPages.Add(tabPage);
    }
}
