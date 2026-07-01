using WinAppDtudo.Forms;
using WinAppDtudo.FormsUC;

namespace WinAppDtudo;

public partial class Frm_MyAnimes : Form
{
    public int _tabIndexMascaras = 0;
    public int _tabIndexMyAnimesPorNome = 0;
    public int _tabIndexMyAnimesPorID = 0;
    public Frm_MyAnimes()
    {
        InitializeComponent();
    }
    //=============================================================================
    private void MnI_ProcurarAnimePorNome_Click(object sender, EventArgs e)
    {
        if(_tabIndexMyAnimesPorNome == 0)
        {
            _tabIndexMyAnimesPorNome++;
            FUC_BuscarPorNome ucMascaras = new()
            {
                //Dock = DockStyle.Fill
            };
            TabPage tabPage = new()
            {
                Text = $"Procurar Anime",
                Name = $"ProcurarAnime",
                ImageIndex = 2,
            };
            tabPage.Controls.Add(ucMascaras);
            Tbc_MyAnimes.TabPages.Add(tabPage);
        }
        else
        {
            MessageBox.Show($"A aba 'Procurar Anime' já está aberta.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    //=============================================================================
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
    //=============================================================================
    private void MnI_FecharAbaAtual_Click(object sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagaAbaAtual(Tbc_MyAnimes.SelectedTab);
        }
    }
    private void MnI_FecharTodasAbas_Click(object sender, EventArgs e)
    {
        //Na REMOÇÃO de itens de uma COLLECTION, devemos percorrer a COLLECTION de trás para frente, para evitar problemas de índice.
        for (int i = Tbc_MyAnimes.TabPages.Count - 1; i >= 0; i--)
        {
            ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
        }
    }
    private void MnI_FecharAbasADireita_Click(object sender, EventArgs e)
    {
        //remover abas a direita da aba selecionada.
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            int selectedIndex = Tbc_MyAnimes.SelectedIndex;
            for (int i = Tbc_MyAnimes.TabCount - 1; i > selectedIndex; i--)
            {
                ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
            }
        }
    }
    private void MnI_FecharAbasAEsquerda_Click(object sender, EventArgs e)
    {
        //remover abas a esquerda da aba selecionada
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            int selectedIndex = Tbc_MyAnimes.SelectedIndex;
            for (int i = selectedIndex - 1; i >= 0; i--)
            {
                ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
            }
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
    //=============================================================================
    //Menu Flutuante - Captura o evento MouseDown do controle Tbc_MyAnimes e exibe um menu de contexto ao clicar com o botão direito do mouse.
    private void Tbc_MyAnimes_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            ContextMenuStrip contextMenu = new();
            ToolStripMenuItem menuFlutuanteItem1 = CriaMenuFlutuanteItem("Apaga AbaAtual", "BaixoGatilho");
            ToolStripMenuItem menuFlutuanteItem2 = CriaMenuFlutuanteItem("Apaga TodasAbas", "CimaGatilho");
            ToolStripMenuItem menuFlutuanteItem3 = CriaMenuFlutuanteItem("Apaga AbaDireita", "DireitaGatilho");
            ToolStripMenuItem menuFlutuanteItem4 = CriaMenuFlutuanteItem("Apaga AbaEsquerda", "EsquerdaGatilho");
            contextMenu.Items.Add(menuFlutuanteItem1);
            contextMenu.Items.Add(menuFlutuanteItem2);
            contextMenu.Items.Add(menuFlutuanteItem3);
            contextMenu.Items.Add(menuFlutuanteItem4);
            contextMenu.Show(this, new Point(e.X, e.Y));
            menuFlutuanteItem1.Click += new EventHandler(MenuFlutuanteItem1_Click);
            menuFlutuanteItem2.Click += new EventHandler(MenuFlutuanteItem2_Click);
            menuFlutuanteItem3.Click += new EventHandler(MenuFlutuanteItem3_Click);
            menuFlutuanteItem4.Click += new EventHandler(MenuFlutuanteItem4_Click);
        }
    }
    private static ToolStripMenuItem CriaMenuFlutuanteItem(string textMenuItem, string imageName)
    {
        ToolStripMenuItem menuFlutuanteItem = new(textMenuItem);
        if (Properties.Resources.ResourceManager.GetObject(imageName) is Image imgMenuItem)
        { menuFlutuanteItem.Image = imgMenuItem; }
        return menuFlutuanteItem;
    }
    void MenuFlutuanteItem1_Click(object sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagaAbaAtual(Tbc_MyAnimes.SelectedTab);
        }
    }
    void MenuFlutuanteItem2_Click(object sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagarDireita(Tbc_MyAnimes.SelectedIndex);
            ApagarEsquerda(Tbc_MyAnimes.SelectedIndex);
        }
    }
    void MenuFlutuanteItem3_Click(object sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagarDireita(Tbc_MyAnimes.SelectedIndex);
        }
    }
    void MenuFlutuanteItem4_Click(object sender, EventArgs e)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            ApagarEsquerda(Tbc_MyAnimes.SelectedIndex);
        }
    }
    void ApagarDireita(int itemSelecionado)
    {
        for (int i = Tbc_MyAnimes.TabCount - 1; i > itemSelecionado; i--)
        {
            ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
        }
    }
    void ApagarEsquerda(int itemSelecionado)
    {
        for (int i = itemSelecionado - 1; i >= 0; i--)
        {
            //RemoveAt remove a aba no índice especificado.
            ApagaAbaAtual(Tbc_MyAnimes.TabPages[i]);
        }
    }
    //=============================================================================
    void ApagaAbaAtual(TabPage tabPage)
    {
        if (Tbc_MyAnimes.SelectedTab != null)
        {
            if (tabPage.Name == "ProcurarAnime")
            {
                _tabIndexMyAnimesPorNome = 0;
            }
            Tbc_MyAnimes.TabPages.Remove(tabPage);
        }
    }
}
