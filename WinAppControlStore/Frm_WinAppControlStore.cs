using WinAppControlStore.Forms;

namespace WinAppControlStore;

public partial class Frm_WinAppControlStore : Form
{
    public Frm_WinAppControlStore()
    {
        InitializeComponent();
    }

    private void Btn_Site_Dtudo_Click(object sender, EventArgs e)
    {
        //Deve abrir o FrontEnd.
        //"dev": "vite --config DtudoSite/vite.config.js",
        //http://localhost:5173/myanimes
        //Deve abrir o BackEnd.
        //"start": "json-server --watch ./ApiNode/db/animacoes.json --port 3666",
        //"proxy": "node ./ApiNode/mymusicx/discogsProxy.js",
        //OU executar ambos ao mesmo tempo com o comando:
        //"serv": "concurrently \"npm run start\" \"npm run proxy\" \"npm run dev\"",

    }

    private void Btn_Sair_App_Click(object sender, EventArgs e)
    {
        // Fechar apenas o Formulário atual
        this.Close();
    }

    private void Btn_Abrir_Form_Click(object sender, EventArgs e)
    {
        Frm_FormTest formTest = new();
        formTest.ShowDialog();
    }

    private void FormTestToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Frm_FormTest formTest = new();
        formTest.MdiParent = this;
        formTest.Show();
    }

    private void SairToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // Fechar a aplicação
        Application.Exit();
    }

    private void FormHelloWorldToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Frm_HelloWorld formHelloWorld = new();
        formHelloWorld.Show();
    }
    //Menu MyAnimes - Abrir formulário Frm_MyAnimes.
    private void MnI_MyAnimesMenuItem_Click(object sender, EventArgs e)
    {
        Frm_MyAnimes formMyAnimes = new();
        formMyAnimes.Show();
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
}
