using WinAppDtudo.Forms;
using WinAppDtudo.Services;

namespace WinAppDtudo;

/// <summary>
/// Frm_WinAppDtudo é a classe principal do aplicativo WinForms, representando o formulário principal da aplicação Dtudo.
/// </summary>
public partial class Frm_WinAppDtudo : CustomFormNoBorder
{
    private readonly AuthApiService _authApiService = new();

    public Frm_WinAppDtudo()
    {
        InitializeComponent();
        // Aplicar o tema Dark Mode ao formulário e seus componentes
        ThemeManager.ApplyDarkModeToForm(this);
        // Inicializa o formulário customizado sem barra de título
        InitializeCustomFormNoBorder(Mnu_Principal);
        AddControlButtonsToMenuStrip(Mnu_Principal);

        //Opções de inicialização do formulário, após a inicialização dos componentes.
        MnI_MyAnimes.Enabled = false;
        MnI_MyMusicX.Enabled = false;
        MnI_NinoTI.Enabled = false;
        MnI_Desconectar.Enabled = false;
    }
    //=========================================================
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
    //Menu NinoTI - Abrir formulário Frm_NinoTI.
    private void MnI_NinoTI_Click(object sender, EventArgs e)
    {
        Frm_NinoTI formNinoTI = new();
        formNinoTI.Show();
    }
    //==============================================================
    //Menu Cadastrar Usuário - Abrir formulário Frm_CadastrarUsuario.
    private void MnI_CadastrarUsuario_Click(object sender, EventArgs e)
    {
        Frm_CadastrarUsuario formCadastrarUsuario = new();
        formCadastrarUsuario.Show();
    }
    //Menu Conectar - Abrir formulário Frm_Login.
    private async void MnI_Conectar_Click(object sender, EventArgs e)
    {
        using Frm_Login formLogin = new();
        var resultado = formLogin.ShowDialog();
        if (resultado == DialogResult.OK)
        {
            string senha = formLogin.Senha;
            string login = formLogin.Login;
            try
            {
                var authResponse = await _authApiService.LoginAsync(login, senha);
                if (authResponse.Success && authResponse.User is not null)
                {
                    MnI_Conectar.Enabled = false;
                    MnI_MyAnimes.Enabled = true;
                    MnI_MyMusicX.Enabled = true;
                    MnI_NinoTI.Enabled = true;
                    MnI_Desconectar.Enabled = true;
                    WinAppDtudo.Services.DarkMessageBox.Show($"Login realizado com sucesso! Bem-vindo, {authResponse.User.Name}.", "Mensagem", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    WinAppDtudo.Services.DarkMessageBox.Show(authResponse.Message ?? "Usuario ou senha invalida.", "Mensagem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                WinAppDtudo.Services.DarkMessageBox.Show($"Nao foi possivel conectar a ApiMyAnimes em {AppConfigurationService.ApiMyAnimesBaseUrl}.\n\n{ex.Message}", "Erro de conexao", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else if (resultado == DialogResult.Cancel)
        { WinAppDtudo.Services.DarkMessageBox.Show($"Login cancelado."); }
    }
    //Menu Desconectar - Desconectar o usuário atual.
    private void MnI_Desconectar_Click(object sender, EventArgs e)
    {
        Frm_Questao formQuestao = new("InterrogacaoBrasil", "Deseja realmente se desconectar?");
        var resultado = formQuestao.ShowDialog();
        if (resultado == DialogResult.OK)
        {
            MnI_Conectar.Enabled = true;
            MnI_MyAnimes.Enabled = false;
            MnI_MyMusicX.Enabled = false;
            MnI_NinoTI.Enabled = false;
            MnI_Desconectar.Enabled = false;
            //Fecha todos os formulários abertos, exceto o THIS.Frm_WinAppControlStore.
            foreach (Form form in Application.OpenForms.Cast<Form>().ToList())
            { if (form != this) form.Close(); }
            WinAppDtudo.Services.DarkMessageBox.Show("Você foi Desconectado!");
        }
        else if (resultado == DialogResult.Cancel)
        { WinAppDtudo.Services.DarkMessageBox.Show($"Desconexão cancelada."); }
    }
    //Menu Sair - Fechar a aplicação Toda.
    private void MnI_Sair_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }
    //===========================================================================================
    //Captura o evento MouseDown do formulário Frm_WinAppDtudo e exibe um menu de contexto ao clicar com o botão direito do mouse.
    private void Frm_WinAppDtudo_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            //string message = $"MouseDown, na posição ({e.X}, {e.Y}) com o botão {e.Button}";
            //WinAppDtudo.Services.DarkMessageBox.Show(message);
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
        WinAppDtudo.Services.DarkMessageBox.Show("Opção 1 selecionada");
    }
    void MenuFlutuanteItem2_Click(object? sender, EventArgs e)
    {
        WinAppDtudo.Services.DarkMessageBox.Show("Opção 2 selecionada");
    }
    void MenuFlutuanteItem3_Click(object? sender, EventArgs e)
    {
        WinAppDtudo.Services.DarkMessageBox.Show("Opção 3 selecionada");
    }
    //=================================================================
    //Botão - Devera abrir o site Dtudo...
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
}
