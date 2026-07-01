using WinAppDtudo.Forms;
using LibDtudo.Shared.Utils;
namespace WinAppDtudo;
/// <summary>
/// Frm_WinAppDtudo é a classe principal do aplicativo WinForms, representando o formulário principal da aplicação Dtudo.
/// </summary>
public partial class Frm_WinAppDtudo : Form
{
    public Frm_WinAppDtudo()
    {
        InitializeComponent();
        //Opções de inicialização do formulário, após a inicialização dos componentes.
        MnI_Abrir.Enabled = false;
        MnI_MyAnimes.Enabled = true;
        MnI_MyMusicX.Enabled = false;
        MnI_NinoTI.Enabled = false;
        MnI_Desconectar.Enabled = false;
    }
    //==============================================
    //Menu Cadastrar Usuário - Abrir formulário Frm_CadastrarUsuario.
    private void MnI_CadastrarUsuario_Click(object sender, EventArgs e)
    {
        Frm_CadastrarUsuario formCadastrarUsuario = new();
        formCadastrarUsuario.Show();
    }
    //Menu Conectar - Abrir formulário Frm_Login.
    private void MnI_Conectar_Click(object sender, EventArgs e)
    {
        using Frm_Login formLogin = new();
        var resultado = formLogin.ShowDialog();
        if (resultado == DialogResult.OK)
        {
            string senha = formLogin.Senha;
            string login = formLogin.Login;
            if (ValidaSenhaLogin.ValidarSenhaDoLogin(login, senha) == true)
            {
                MnI_Conectar.Enabled = false;
                MnI_Abrir.Enabled = true;
                MnI_MyAnimes.Enabled = true;
                MnI_MyMusicX.Enabled = true;
                MnI_NinoTI.Enabled = true;
                MnI_Desconectar.Enabled = true;
                MessageBox.Show($"Login realizado com sucesso! Bem-vindo, {login}.", "Mensagem", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Usuário ou Senha Inválida!", "Mensagem", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else if (resultado == DialogResult.Cancel)
        {
            MessageBox.Show($"Login cancelado.");
        }
    }
    //Menu Desconectar - Desconectar o usuário atual.
    private void MnI_Desconectar_Click(object sender, EventArgs e)
    {
        Frm_Questao formQuestao = new("InterrogacaoBrasil", "Deseja realmente se desconectar?");
        var resultado = formQuestao.ShowDialog();
        if (resultado == DialogResult.OK)
        {
            MnI_Conectar.Enabled = true;
            MnI_Abrir.Enabled = false;
            MnI_MyAnimes.Enabled = false;
            MnI_MyMusicX.Enabled = false;
            MnI_NinoTI.Enabled = false;
            MnI_Desconectar.Enabled = false;
            //Fecha todos os formulários abertos, exceto o THIS.Frm_WinAppControlStore.
            foreach (Form form in Application.OpenForms.Cast<Form>().ToList())
            {
                if (form != this) form.Close();
            }
            MessageBox.Show("Você foi Desconectado!");
        }
        else if (resultado == DialogResult.Cancel)
        {
            MessageBox.Show($"Desconexão cancelada.");
        }
    }
    //Menu Sair - Fechar a aplicação Toda.
    private void MnI_Sair_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }
    //Menu HelloWorld - Abrir formulário Frm_HelloWorld.
    private void MnI_FormHelloWorld_Click(object sender, EventArgs e)
    {
        Frm_HelloWorld formHelloWorld = new();
        formHelloWorld.Show();
    }
    //Menu FormTest - Abrir formulário Frm_FormTest.
    private void MnI_FormTest_Click(object sender, EventArgs e)
    {
        Frm_FormTest formTest = new();
        formTest.Show();
    }
    //Menu MyAnimes - Abrir formulário Frm_MyAnimes.
    private void MnI_MyAnimes_Click(object sender, EventArgs e)
    {
        Frm_MyAnimes formMyAnimes = new();
        formMyAnimes.Show();
    }
    //Menu MyMusicX - Abrir formulário Frm_MyMusicX.
    private void MnI_MyMusicX_Click(object sender, EventArgs e)
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
        this.Cursor = Cursors.WaitCursor;
        for (int i = 0; i < 5; i++)
        {
            System.Threading.Thread.Sleep(1000);
        }
        MessageBox.Show($"Fechando o Formulário...");
        this.Cursor = Cursors.Default;
        this.Close();
    }
    //Botão - Abrir formulário Frm_FormTest.
    private void Btn_Abrir_Form_Click(object sender, EventArgs e)
    {
        Frm_FormTest formTest = new();
        formTest.ShowDialog();
    }
    //===========================================================================================
    //Captura o evento MouseDown do formulário Frm_WinAppDtudo e exibe um menu de contexto ao clicar com o botão direito do mouse.
    private void Frm_WinAppDtudo_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            //string message = $"MouseDown, na posição ({e.X}, {e.Y}) com o botão {e.Button}";
            //MessageBox.Show(message);
            ContextMenuStrip contextMenu = new();
            ToolStripMenuItem menuFlutuanteItem1 = CriaMenuFlutuanteItem("Opção 1", "CaveraMetal");
            ToolStripMenuItem menuFlutuanteItem2 = CriaMenuFlutuanteItem("Opção 2", "CaveraMetal");
            ToolStripMenuItem menuFlutuanteItem3 = CriaMenuFlutuanteItem("Opção 3", "CaveraMetal");
            contextMenu.Items.Add(menuFlutuanteItem1);
            contextMenu.Items.Add(menuFlutuanteItem2);
            contextMenu.Items.Add(menuFlutuanteItem3);
            contextMenu.Show(this, new Point(e.X, e.Y));
            menuFlutuanteItem1.Click += new EventHandler(MenuFlutuanteItem1_Click);
            menuFlutuanteItem2.Click += new EventHandler(MenuFlutuanteItem2_Click);
            menuFlutuanteItem3.Click += new EventHandler(MenuFlutuanteItem3_Click);
        }
    }
    private static ToolStripMenuItem CriaMenuFlutuanteItem(string textMenuItem, string imageName)
    {
        ToolStripMenuItem menuFlutuanteItem = new(textMenuItem);
        if (Properties.Resources.ResourceManager.GetObject(imageName) is Image imgMenuItem)
            { menuFlutuanteItem.Image = imgMenuItem; }
        return menuFlutuanteItem;
    }
    void MenuFlutuanteItem1_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Opção 1 selecionada");
    }
    void MenuFlutuanteItem2_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Opção 2 selecionada");
    }
    void MenuFlutuanteItem3_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Opção 3 selecionada");
    }
}
