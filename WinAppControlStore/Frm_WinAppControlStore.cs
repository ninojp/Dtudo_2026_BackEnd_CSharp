using WinAppControlStore.Forms;
using ApiCSharp.Shared.Utils;
namespace WinAppControlStore;

public partial class Frm_WinAppControlStore : Form
{
    public Frm_WinAppControlStore()
    {
        InitializeComponent();
        //Opções de inicialização do formulário, após a inicialização dos componentes.
        MnI_Abrir.Enabled = false;
        MnI_MyAnimes.Enabled = false;
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
        this.Close();
    }
    //Botão - Abrir formulário Frm_FormTest.
    private void Btn_Abrir_Form_Click(object sender, EventArgs e)
    {
        Frm_FormTest formTest = new();
        formTest.ShowDialog();
    }


}
