using WinAppControlStore.Forms;
using WinAppControlStore.FormsUC;

namespace WinAppControlStore;

public partial class Frm_MyAnimes : Form
{
    public int _tabIndexMascaras = 0;
    public int _tabIndexMyAnimesPorNome = 0;
    public int _tabIndexMyAnimesPorID = 0;
    public Frm_MyAnimes()
    {
        InitializeComponent();
    }
    private void MnI_AbaMascaras_Click(object sender, EventArgs e)
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

    private void MnI_ProcurarAnimePorNome_Click(object sender, EventArgs e)
    {
        _tabIndexMyAnimesPorNome++;
        FUC_BuscarPorNome ucMascaras = new()
        {
            //Dock = DockStyle.Fill
        };
        TabPage tabPage = new()
        {
            Text = $"{_tabIndexMyAnimesPorNome} Procurar Anime",
            Name = $"{_tabIndexMyAnimesPorNome} ProcurarAnime",
            ImageIndex = 2,
        };
        tabPage.Controls.Add(ucMascaras);
        Tbc_MyAnimes.TabPages.Add(tabPage);
    }

    private void MnI_ProcurarAnimePorID_Click(object sender, EventArgs e)
    {
        _tabIndexMyAnimesPorID++;
        FUC_BuscarPorID ucMascaras = new()
        {
            //Dock = DockStyle.Fill
        };
        TabPage tabPage = new()
        {
            Text = $"{_tabIndexMyAnimesPorID} Procurar Anime ID",
            Name = $"{_tabIndexMyAnimesPorID} ProcurarAnimeID",
            ImageIndex = 1,
        };
        tabPage.Controls.Add(ucMascaras);
        Tbc_MyAnimes.TabPages.Add(tabPage);
    }
    //=============================================================================
    private void MnI_FecharAbaAtual_Click(object sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            Tbc_MyAnimes.TabPages.Remove(Tbc_MyAnimes.SelectedTab);
        }
    }
    private void MnI_FecharTodasAbas_Click(object sender, EventArgs e)
    {
        //Na REMOÇÃO de itens de uma COLLECTION, devemos percorrer a COLLECTION de trás para frente, para evitar problemas de índice.
        for (int i = Tbc_MyAnimes.TabPages.Count - 1; i >= 0; i--)
        {
            Tbc_MyAnimes.TabPages.RemoveAt(i);
        }
    }
    //=============================================================================
    //Item de menu para abrir o formulário Frm_Questao
    private void MnI_FormMsgBox_Click(object sender, EventArgs e)
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
