using WinAppControlStore.Forms;

namespace WinAppControlStore;

public partial class Frm_WinAppControlStore : Form
{
    public Frm_WinAppControlStore()
    {
        InitializeComponent();
    }
    //Menu FormTest - Abrir formulário Frm_FormTest.
    private void FormTestToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Frm_FormTest formTest = new();
        formTest.Show();
    }
    //Menu Sair - Fechar a aplicação Toda.
    private void SairToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }
    //Menu HelloWorld - Abrir formulário Frm_HelloWorld.
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
    //Menu MyAnimes - Abrir formulário Frm_MyAnimes.
    private void MnI_MyAnimesMenuItem_Click_1(object sender, EventArgs e)
    {
        Frm_MyAnimes formMyAnimes = new();
        formMyAnimes.Show();
    }
    //Menu MyMusicX - Abrir formulário Frm_MyMusicX.
    private void myMusicXToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Frm_MyMusicX formMyMusicX = new();
        formMyMusicX.Show();
    }
    //=================================================================
    //Botão - Abrir o site Dtudo.
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
    //Botão - Fecha apenas o Formulário atual.
    private void Btn_Sair_App_Click(object sender, EventArgs e)
    {
        this.Close();
    }
    //Botão - Abrir formulário Frm_FormTest.
    private void Btn_Abrir_Form_Click(object sender, EventArgs e)
    {
        Frm_FormTest formTest = new();
        formTest.ShowDialog();
    }

}
