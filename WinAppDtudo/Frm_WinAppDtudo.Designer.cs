namespace WinAppDtudo;

partial class Frm_WinAppDtudo
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_WinAppDtudo));
        Mnu_Principal = new MenuStrip();
        MnI_Arquivo = new ToolStripMenuItem();
        MnI_Monitoramento = new ToolStripMenuItem();
        MnI_CadastrarUsuario = new ToolStripMenuItem();
        MnI_Saude = new ToolStripMenuItem();
        MnI_Conectar = new ToolStripMenuItem();
        MnI_Desconectar = new ToolStripMenuItem();
        MnI_Sair = new ToolStripMenuItem();
        MnI_MyAnimes = new ToolStripMenuItem();
        MnI_BuscarAnime = new ToolStripMenuItem();
        MnI_AnalizarPastas = new ToolStripMenuItem();
        MnI_MyMusicX = new ToolStripMenuItem();
        MnI_NinoTI = new ToolStripMenuItem();
        MnI_DtudoSite = new ToolStripMenuItem();
        MnI_DtudoWebView = new ToolStripMenuItem();
        Tbc_Principal = new WinAppDtudo.Controls.DarkTabControl
        {
            AutoSizeTabHeaders = true,
            ShowCloseButtons = true,
            AllowTabReordering = true
        };
        Ssb_StatusRodape = new StatusStrip();
        Lbl_StatusRodape = new ToolStripStatusLabel();
        Mnu_Principal.SuspendLayout();
        Ssb_StatusRodape.SuspendLayout();
        SuspendLayout();
        // 
        // Mnu_Principal
        // 
        Mnu_Principal.BackColor = Color.Black;
        Mnu_Principal.BackgroundImageLayout = ImageLayout.None;
        Mnu_Principal.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Mnu_Principal.ImageScalingSize = new Size(32, 32);
        Mnu_Principal.Items.AddRange(new ToolStripItem[] { MnI_Arquivo, MnI_MyAnimes, MnI_DtudoSite, MnI_MyMusicX, MnI_NinoTI });
        Mnu_Principal.Location = new Point(0, 0);
        Mnu_Principal.Name = "Mnu_Principal";
        Mnu_Principal.Padding = new Padding(7, 2, 0, 2);
        Mnu_Principal.RenderMode = ToolStripRenderMode.Professional;
        Mnu_Principal.Size = new Size(1272, 40);
        Mnu_Principal.TabIndex = 7;
        Mnu_Principal.Text = "MenuPrincipal";
        // 
        // MnI_Arquivo
        // 
        MnI_Arquivo.BackColor = Color.Transparent;
        MnI_Arquivo.DropDownItems.AddRange(new ToolStripItem[] { MnI_Monitoramento, MnI_CadastrarUsuario, MnI_Saude, MnI_Conectar, MnI_Desconectar, MnI_Sair });
        MnI_Arquivo.Image = Properties.Resources.YingYang_HD;
        MnI_Arquivo.Name = "MnI_Arquivo";
        MnI_Arquivo.Size = new Size(115, 36);
        MnI_Arquivo.Text = "Arquivo";
        // 
        // MnI_Monitoramento
        // 
        MnI_Monitoramento.ForeColor = Color.Gold;
        MnI_Monitoramento.Image = Properties.Resources.RadioAtivo;
        MnI_Monitoramento.Name = "MnI_Monitoramento";
        MnI_Monitoramento.Size = new Size(231, 38);
        MnI_Monitoramento.Text = "Monitoramento local";
        MnI_Monitoramento.Click += MnI_Monitoramento_Click;
        // 
        // MnI_CadastrarUsuario
        // 
        MnI_CadastrarUsuario.ForeColor = Color.Gold;
        MnI_CadastrarUsuario.Image = Properties.Resources.CaveraMetal;
        MnI_CadastrarUsuario.Name = "MnI_CadastrarUsuario";
        MnI_CadastrarUsuario.Size = new Size(231, 38);
        MnI_CadastrarUsuario.Text = "Cadastrar Usuário";
        MnI_CadastrarUsuario.Click += MnI_CadastrarUsuario_Click;
        // 
        // MnI_Saude
        // 
        MnI_Saude.ForeColor = Color.Gold;
        MnI_Saude.Image = Properties.Resources.CaveraMetal;
        MnI_Saude.Name = "MnI_Saude";
        MnI_Saude.Size = new Size(231, 38);
        MnI_Saude.Text = "Painel de Saude";
        MnI_Saude.Click += MnI_Saude_Click;
        // 
        // MnI_Conectar
        // 
        MnI_Conectar.ForeColor = Color.Gold;
        MnI_Conectar.Image = Properties.Resources.CaveraMetal;
        MnI_Conectar.Name = "MnI_Conectar";
        MnI_Conectar.Size = new Size(231, 38);
        MnI_Conectar.Text = "Conectar";
        MnI_Conectar.Click += MnI_Conectar_Click;
        // 
        // MnI_Desconectar
        // 
        MnI_Desconectar.ForeColor = Color.Gold;
        MnI_Desconectar.Image = Properties.Resources.CaveraMetal;
        MnI_Desconectar.Name = "MnI_Desconectar";
        MnI_Desconectar.Size = new Size(231, 38);
        MnI_Desconectar.Text = "Desconectar";
        MnI_Desconectar.Click += MnI_Desconectar_Click;
        // 
        // MnI_Sair
        // 
        MnI_Sair.BackColor = Color.Transparent;
        MnI_Sair.ForeColor = Color.Gold;
        MnI_Sair.Image = Properties.Resources.SlaveMoney;
        MnI_Sair.Name = "MnI_Sair";
        MnI_Sair.ShortcutKeys = Keys.Alt | Keys.S;
        MnI_Sair.Size = new Size(231, 38);
        MnI_Sair.Text = "&Sair";
        MnI_Sair.Click += MnI_Sair_Click;
        // 
        // MnI_MyAnimes
        // 
        MnI_MyAnimes.DropDownItems.AddRange(new ToolStripItem[] { MnI_BuscarAnime, MnI_AnalizarPastas });
        MnI_MyAnimes.Image = Properties.Resources.TI_link;
        MnI_MyAnimes.Name = "MnI_MyAnimes";
        MnI_MyAnimes.Size = new Size(135, 36);
        MnI_MyAnimes.Text = "MyAnimes";
        // 
        // MnI_BuscarAnime
        // 
        MnI_BuscarAnime.ForeColor = Color.Gold;
        MnI_BuscarAnime.Image = Properties.Resources.Vvendetta;
        MnI_BuscarAnime.Name = "MnI_BuscarAnime";
        MnI_BuscarAnime.Size = new Size(231, 38);
        MnI_BuscarAnime.Text = "Buscar Anime";
        MnI_BuscarAnime.Click += MnI_BuscarAnime_Click;
        // 
        // MnI_AnalizarPastas
        // 
        MnI_AnalizarPastas.ForeColor = Color.Gold;
        MnI_AnalizarPastas.Image = Properties.Resources.MaskVendettaReal;
        MnI_AnalizarPastas.Name = "MnI_AnalizarPastas";
        MnI_AnalizarPastas.Size = new Size(231, 38);
        MnI_AnalizarPastas.Text = "Analizar Pastas";
        MnI_AnalizarPastas.Click += MnI_AnalizarPastas_Click;
        // 
        // MnI_MyMusicX
        // 
        MnI_MyMusicX.Image = Properties.Resources.violaoEmChamas;
        MnI_MyMusicX.Name = "MnI_MyMusicX";
        MnI_MyMusicX.Size = new Size(133, 36);
        MnI_MyMusicX.Text = "MyMusicX";
        MnI_MyMusicX.Click += MnI_MyMusicX_Click;
        // 
        // MnI_NinoTI
        // 
        MnI_NinoTI.Image = Properties.Resources.OlhoBRHacker1024;
        MnI_NinoTI.Name = "MnI_NinoTI";
        MnI_NinoTI.Size = new Size(106, 36);
        MnI_NinoTI.Text = "NinoTI";
        MnI_NinoTI.Click += MnI_NinoTI_Click;
        // 
        // MnI_DtudoSite
        // 
        MnI_DtudoSite.DropDownItems.AddRange(new ToolStripItem[] { MnI_DtudoWebView });
        MnI_DtudoSite.Image = Properties.Resources.DtudoFireLogo;
        MnI_DtudoSite.Name = "MnI_DtudoSite";
        MnI_DtudoSite.Size = new Size(150, 36);
        MnI_DtudoSite.Text = "DtudoSite";
        // 
        // MnI_DtudoWebView
        // 
        MnI_DtudoWebView.ForeColor = Color.Gold;
        MnI_DtudoWebView.Image = Properties.Resources.DtudoFireLogo;
        MnI_DtudoWebView.Name = "MnI_DtudoWebView";
        MnI_DtudoWebView.Size = new Size(231, 38);
        MnI_DtudoWebView.Text = "DtudoWebView2";
        MnI_DtudoWebView.Click += MnI_DtudoWebView_Click;
        // 
        // Tbc_Principal
        // 
        Tbc_Principal.AccessibleName = "Controle de Abas";
        Tbc_Principal.AllowDrop = true;
        Tbc_Principal.Dock = DockStyle.Fill;
        Tbc_Principal.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Tbc_Principal.ForeColor = Color.Gold;
        Tbc_Principal.ImeMode = ImeMode.On;
        Tbc_Principal.ItemSize = new Size(0, 50);
        Tbc_Principal.Location = new Point(0, 40);
        Tbc_Principal.Margin = new Padding(0);
        Tbc_Principal.Name = "Tbc_Principal";
        Tbc_Principal.Padding = new Point(19, 6);
        Tbc_Principal.Size = new Size(1272, 630);
        Tbc_Principal.SizeMode = TabSizeMode.Normal;
        Tbc_Principal.TabIndex = 1;
        // 
        // Ssb_StatusRodape
        // 
        Ssb_StatusRodape.Items.AddRange(new ToolStripItem[] { Lbl_StatusRodape });
        Ssb_StatusRodape.Location = new Point(0, 670);
        Ssb_StatusRodape.Name = "Ssb_StatusRodape";
        Ssb_StatusRodape.Size = new Size(1272, 22);
        Ssb_StatusRodape.SizingGrip = false;
        Ssb_StatusRodape.TabIndex = 2;
        // 
        // Lbl_StatusRodape
        // 
        Lbl_StatusRodape.Name = "Lbl_StatusRodape";
        Lbl_StatusRodape.Size = new Size(0, 17);
        Lbl_StatusRodape.Spring = true;
        Lbl_StatusRodape.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // Frm_WinAppDtudo
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = SystemColors.Desktop;
        ClientSize = new Size(1272, 692);
        Controls.Add(Tbc_Principal);
        Controls.Add(Ssb_StatusRodape);
        Controls.Add(Mnu_Principal);
        Font = new Font("Segoe Fluent Icons", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        ForeColor = Color.Gold;
        FormBorderStyle = FormBorderStyle.None;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = Mnu_Principal;
        Margin = new Padding(2);
        Name = "Frm_WinAppDtudo";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "WinApp Dtudo";
        Mnu_Principal.ResumeLayout(false);
        Mnu_Principal.PerformLayout();
        Ssb_StatusRodape.ResumeLayout(false);
        Ssb_StatusRodape.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip Mnu_Principal;
    private ToolStripMenuItem MnI_Arquivo;
    private ToolStripMenuItem MnI_Sair;
    private ToolStripMenuItem MnI_MyAnimes;
    private ToolStripMenuItem MnI_BuscarAnime;
    private ToolStripMenuItem MnI_AnalizarPastas;
    private ToolStripMenuItem MnI_MyMusicX;
    private ToolStripMenuItem MnI_NinoTI;
    private ToolStripMenuItem MnI_CadastrarUsuario;
    private ToolStripMenuItem MnI_Saude;
    private ToolStripMenuItem MnI_Conectar;
    private ToolStripMenuItem MnI_Desconectar;
    private ToolStripMenuItem MnI_DtudoSite;
    private ToolStripMenuItem MnI_DtudoWebView;
    private ToolStripMenuItem MnI_Monitoramento;
    private WinAppDtudo.Controls.DarkTabControl Tbc_Principal;
    private StatusStrip Ssb_StatusRodape;
    private ToolStripStatusLabel Lbl_StatusRodape;
}
