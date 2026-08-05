using WinAppDtudo.Forms;
using WinAppDtudo.Services;

namespace WinAppDtudo;

/// <summary>
/// Frm_WinAppDtudo é a classe principal do aplicativo WinForms, representando o formulário principal da aplicação Dtudo.
/// </summary>
public partial class Frm_WinAppDtudo : CustomFormNoBorder
{
    private const float DesignWidth = 1272F;
    private const float DesignContentHeight = 652F;
    private readonly AuthApiService _authApiService = new();
    private bool _isApplyingMainLayout;

    public Frm_WinAppDtudo()
    {
        InitializeComponent();
        // Aplicar o tema Dark Mode ao formulário e seus componentes
        ThemeManager.ApplyDarkModeToForm(this);
        // Inicializa o formulário customizado sem barra de título
        InitializeCustomFormNoBorder(Mnu_Principal);
        AddControlButtonsToMenuStrip(Mnu_Principal);
        ConfigureNavigationButton(Btn_DtudoSite);
        ConfigureNavigationButton(Btn_MyMusicxForm);
        ConfigureNavigationButton(Btn_MyAnimesForm);
        ConfigureNavigationButton(Btn_NinoTIForm);

        //Opções de inicialização do formulário, após a inicialização dos componentes.
        MnI_MyAnimes.Enabled = false;
        MnI_MyMusicX.Enabled = false;
        MnI_NinoTI.Enabled = false;
        MnI_Desconectar.Enabled = false;

        InitializeMainLayout();
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
    private void Btn_DtudoSite_Click(object sender, EventArgs e)
    {
        //Deve verificar se os serviços necessários (ApiMyAnimes e discogsProxy) estão em execução antes de abrir o site. Se não estiverem, deve iniciar os serviços.
        //Deve abrir o FrontEnd. Meu Site, http://localhost:5173/myanimes
        //C:\2026MeusProjetos\Dtudo2026\DtudoSite\
        //    "scripts": {
        //"proxy": "node ./ApiNode/mymusicx/discogsProxy.js",
        //"dev": "vite --config ./DtudoSite/vite.config.js",
        //"api:myanimes:run": "dotnet run --project ApiMyAnimes/ApiMyAnimes.csproj --launch-profile ApiMyAnimes",
        //"api:myanimelist:run": "dotnet run --project ApiMyAnimeList/ApiMyAnimeList.csproj --launch-profile https",
        //"api:myanimes": "node scripts/run-if-down.js https://localhost:63980/apiLocal/Health -- npm run api:myanimes:run",
        //"api:myanimelist": "node scripts/run-if-down.js https://localhost:7146/ApiMyAnimeList/health -- npm run api:myanimelist:run",
        //"serv": "concurrently \"npm run api:myanimes\" \"npm run api:myanimelist\" \"npm run proxy\" \"npm run dev\" ",
    }

    private void Btn_MyAnimesForm_Click(object sender, EventArgs e)
    {
        Frm_MyAnimes formMyAnimes = new();
        formMyAnimes.Show();
    }

    private void Btn_NinoTIForm_Click(object sender, EventArgs e)
    {
        Frm_NinoTI formNinoTI = new();
        formNinoTI.Show();
    }

    private void Btn_MyMusicxForm_Click(object sender, EventArgs e)
    {
        Frm_MyMusicX formMyMusicX = new();
        formMyMusicX.Show();
    }

    private void InitializeMainLayout()
    {
        ConfigureLayoutControl(Lbl_Titulo);
        ConfigureLayoutControl(Btn_DtudoSite);
        ConfigureLayoutControl(Btn_MyAnimesForm);
        ConfigureLayoutControl(Btn_MyMusicxForm);
        ConfigureLayoutControl(Btn_NinoTIForm);
        ConfigureLayoutControl(Lbl_DescricaoMyMusicX);
        ConfigureLayoutControl(Lbl_DescricaoMyAnimes);
        ConfigureLayoutControl(label1);
        ConfigureLayoutControl(label2);

        Resize += Frm_WinAppDtudo_Resize;
        DpiChanged += Frm_WinAppDtudo_DpiChanged;
        Shown += Frm_WinAppDtudo_Shown;
        ApplyMainLayout();
    }

    private static void ConfigureLayoutControl(Control control)
    {
        control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        control.Dock = DockStyle.None;
    }

    private void Frm_WinAppDtudo_Resize(object? sender, EventArgs e)
    {
        ApplyMainLayout();
    }

    private void Frm_WinAppDtudo_DpiChanged(object? sender, EventArgs e)
    {
        ApplyMainLayout();
    }

    private void Frm_WinAppDtudo_Shown(object? sender, EventArgs e)
    {
        ApplyMainLayout();
    }

    private void ApplyMainLayout()
    {
        if (_isApplyingMainLayout || IsDisposed || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        _isApplyingMainLayout = true;
        SuspendLayout();
        try
        {
            var contentTop = Mnu_Principal.Visible ? Mnu_Principal.Bottom : 0;
            var contentWidth = ClientSize.Width;
            var contentHeight = Math.Max(1, ClientSize.Height - contentTop);
            var scale = Math.Min(contentWidth / DesignWidth, contentHeight / DesignContentHeight);
            var offsetX = (contentWidth - DesignWidth * scale) / 2F;
            var offsetY = contentTop + (contentHeight - DesignContentHeight * scale) / 2F;

            SetScaledBounds(Lbl_Titulo, 801F, 12F, 296F, 90F, scale, offsetX, offsetY);
            SetScaledBounds(Btn_DtudoSite, 280F, 85F, 242F, 86F, scale, offsetX, offsetY);
            SetScaledBounds(label2, 333F, 51F, 157F, 42F, scale, offsetX, offsetY);
            SetScaledBounds(Btn_NinoTIForm, 1044F, 233F, 144F, 120F, scale, offsetX, offsetY);
            SetScaledBounds(label1, 1058F, 204F, 111F, 37F, scale, offsetX, offsetY);
            SetScaledBounds(Btn_MyMusicxForm, 86F, 322F, 85F, 213F, scale, offsetX, offsetY);
            SetScaledBounds(Lbl_DescricaoMyMusicX, 42F, 537F, 160F, 53F, scale, offsetX, offsetY);
            SetScaledBounds(Btn_MyAnimesForm, 532F, 420F, 272F, 232F, scale, offsetX, offsetY);
            SetScaledBounds(Lbl_DescricaoMyAnimes, 588F, 385F, 157F, 46F, scale, offsetX, offsetY);
        }
        finally
        {
            ResumeLayout(false);
        }

        _isApplyingMainLayout = false;
    }

    private static void SetScaledBounds(
        Control control,
        float x,
        float y,
        float width,
        float height,
        float scale,
        float offsetX,
        float offsetY)
    {
        var bounds = new Rectangle(
            (int)Math.Round(offsetX + x * scale),
            (int)Math.Round(offsetY + y * scale),
            Math.Max(1, (int)Math.Round(width * scale)),
            Math.Max(1, (int)Math.Round(height * scale)));

        if (control.Bounds != bounds)
            control.Bounds = bounds;
    }

    private static void ConfigureNavigationButton(Button button)
    {
        button.BackColor = Color.Transparent;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.Gold;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.Transparent;
        button.FlatAppearance.MouseDownBackColor = Color.Transparent;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
        button.MouseEnter += (_, _) => button.FlatAppearance.BorderSize = 1;
        button.MouseLeave += (_, _) => button.FlatAppearance.BorderSize = 0;
    }
}
