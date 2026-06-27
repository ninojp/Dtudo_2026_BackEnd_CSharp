using WinAppControlStore.Forms;
using WinAppControlStore.FormsUC;

namespace WinAppControlStore;

public partial class Frm_MyAnimes : Form
{
    public int _tabIndexMascaras = 0;
    public int _tabIndexMyAnimes = 0;
    public int _tabIndexMyAnimesID = 0;
    public Frm_MyAnimes()
    {
        InitializeComponent();
    }
    private void AbaMascarasToolStripMenuItem_Click(object sender, EventArgs e)
    {
        _tabIndexMascaras++;
        FUC_Mascaras ucMascaras = new()
        {
            Dock = DockStyle.Fill
        };
        TabPage tabPage = new()
        {
            Text = $"{_tabIndexMascaras} Máscaras",
            Name = $"{_tabIndexMascaras} Mascaras",
            ImageIndex = 3,
        };
        tabPage.Controls.Add(ucMascaras);
        Tbc_MyAnimes.TabPages.Add(tabPage);
    }

    private void ProcurarAnimeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        _tabIndexMyAnimes++;
        FUC_BuscarPorNome ucMascaras = new()
        {
            //Dock = DockStyle.Fill
        };
        TabPage tabPage = new()
        {
            Text = $"{_tabIndexMyAnimes} Procurar Anime",
            Name = $"{_tabIndexMyAnimes} ProcurarAnime",
            ImageIndex = 2,
        };
        tabPage.Controls.Add(ucMascaras);
        Tbc_MyAnimes.TabPages.Add(tabPage);
    }

    private void ProcurarAnimePorMalidToolStripMenuItem_Click(object sender, EventArgs e)
    {
        _tabIndexMyAnimesID++;
        FUC_BuscarPorID ucMascaras = new()
        {
            //Dock = DockStyle.Fill
        };
        TabPage tabPage = new()
        {
            Text = $"{_tabIndexMyAnimesID} Procurar Anime ID",
            Name = $"{_tabIndexMyAnimesID} ProcurarAnimeID",
            ImageIndex = 1,
        };
        tabPage.Controls.Add(ucMascaras);
        Tbc_MyAnimes.TabPages.Add(tabPage);
    }
    //=============================================================================
    private void FecharAbaAtualToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            Tbc_MyAnimes.TabPages.Remove(Tbc_MyAnimes.SelectedTab);
        }
    }
    //=============================================================================
    //Item de menu para abrir o formulário Frm_Questao
    private void Msb_MsgBoxToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Frm_Questao frmQuestao = new("pngwing.com", "Deseja continuar?");
        frmQuestao.ShowDialog();
        if (frmQuestao.DialogResult == DialogResult.Yes)
        {
            MessageBox.Show("Você clicou em Continuar!");
        }
        if (frmQuestao.DialogResult == DialogResult.Cancel)
        {
            MessageBox.Show("Você clicou em Parar!");
        }
    }
}
