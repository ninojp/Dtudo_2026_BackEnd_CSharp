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
        Lbl_Titulo = new Label();
        Btn_Site_Dtudo = new Button();
        Mnu_Principal = new MenuStrip();
        MnI_Arquivo = new ToolStripMenuItem();
        MnI_CadastrarUsuario = new ToolStripMenuItem();
        MnI_Conectar = new ToolStripMenuItem();
        MnI_Desconectar = new ToolStripMenuItem();
        MnI_Sair = new ToolStripMenuItem();
        MnI_MyAnimes = new ToolStripMenuItem();
        MnI_MyMusicX = new ToolStripMenuItem();
        MnI_NinoTI = new ToolStripMenuItem();
        Btn_MyAnimesForm = new Button();
        Btn_MyMusicxForm = new Button();
        Btn_NinoTIForm = new Button();
        Lbl_DescricaoMyAnimes = new Label();
        Lbl_DescricaoMyMusicX = new Label();
        Lbl_DescricaoNinoTI = new Label();
        Mnu_Principal.SuspendLayout();
        SuspendLayout();
        // 
        // Lbl_Titulo
        // 
        Lbl_Titulo.Anchor = AnchorStyles.Top;
        Lbl_Titulo.AutoSize = true;
        Lbl_Titulo.BackColor = Color.Transparent;
        Lbl_Titulo.Font = new Font("Segoe UI Black", 15.9000006F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
        Lbl_Titulo.ForeColor = Color.Gold;
        Lbl_Titulo.Location = new Point(737, 45);
        Lbl_Titulo.Margin = new Padding(2, 0, 2, 0);
        Lbl_Titulo.Name = "Lbl_Titulo";
        Lbl_Titulo.Size = new Size(368, 30);
        Lbl_Titulo.TabIndex = 0;
        Lbl_Titulo.Text = "WinAppDtudo - Controle Central";
        // 
        // Btn_Site_Dtudo
        // 
        Btn_Site_Dtudo.Anchor = AnchorStyles.Top;
        Btn_Site_Dtudo.BackColor = Color.Transparent;
        Btn_Site_Dtudo.BackgroundImage = (Image)resources.GetObject("Btn_Site_Dtudo.BackgroundImage");
        Btn_Site_Dtudo.BackgroundImageLayout = ImageLayout.Stretch;
        Btn_Site_Dtudo.FlatAppearance.BorderSize = 0;
        Btn_Site_Dtudo.FlatStyle = FlatStyle.Flat;
        Btn_Site_Dtudo.Font = new Font("Segoe UI", 11.1F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Btn_Site_Dtudo.ForeColor = Color.Gold;
        Btn_Site_Dtudo.Location = new Point(278, 70);
        Btn_Site_Dtudo.Margin = new Padding(2);
        Btn_Site_Dtudo.Name = "Btn_Site_Dtudo";
        Btn_Site_Dtudo.Size = new Size(216, 72);
        Btn_Site_Dtudo.TabIndex = 1;
        Btn_Site_Dtudo.Text = "DtudoSite";
        Btn_Site_Dtudo.TextAlign = ContentAlignment.TopCenter;
        Btn_Site_Dtudo.UseVisualStyleBackColor = false;
        Btn_Site_Dtudo.Click += Btn_Site_Dtudo_Click;
        // 
        // Mnu_Principal
        // 
        Mnu_Principal.BackColor = Color.Black;
        Mnu_Principal.BackgroundImageLayout = ImageLayout.None;
        Mnu_Principal.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Mnu_Principal.ImageScalingSize = new Size(32, 32);
        Mnu_Principal.Items.AddRange(new ToolStripItem[] { MnI_Arquivo, MnI_MyAnimes, MnI_MyMusicX, MnI_NinoTI });
        Mnu_Principal.Location = new Point(0, 0);
        Mnu_Principal.Name = "Mnu_Principal";
        Mnu_Principal.Padding = new Padding(7, 2, 0, 2);
        Mnu_Principal.RenderMode = ToolStripRenderMode.Professional;
        Mnu_Principal.Size = new Size(1260, 40);
        Mnu_Principal.TabIndex = 7;
        Mnu_Principal.Text = "MenuPrincipal";
        // 
        // MnI_Arquivo
        // 
        MnI_Arquivo.BackColor = Color.Transparent;
        MnI_Arquivo.DropDownItems.AddRange(new ToolStripItem[] { MnI_CadastrarUsuario, MnI_Conectar, MnI_Desconectar, MnI_Sair });
        MnI_Arquivo.Image = Properties.Resources.YingYang_HD;
        MnI_Arquivo.Name = "MnI_Arquivo";
        MnI_Arquivo.Size = new Size(115, 36);
        MnI_Arquivo.Text = "Arquivo";
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
        MnI_MyAnimes.Image = Properties.Resources.TI_link;
        MnI_MyAnimes.Name = "MnI_MyAnimes";
        MnI_MyAnimes.Size = new Size(135, 36);
        MnI_MyAnimes.Text = "MyAnimes";
        MnI_MyAnimes.Click += MnI_MyAnimes_Click;
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
        // Btn_MyAnimesForm
        // 
        Btn_MyAnimesForm.Anchor = AnchorStyles.Bottom;
        Btn_MyAnimesForm.BackColor = Color.Transparent;
        Btn_MyAnimesForm.BackgroundImage = Properties.Resources.onePieceGroup;
        Btn_MyAnimesForm.BackgroundImageLayout = ImageLayout.Stretch;
        Btn_MyAnimesForm.FlatAppearance.BorderSize = 0;
        Btn_MyAnimesForm.FlatStyle = FlatStyle.Flat;
        Btn_MyAnimesForm.Location = new Point(526, 485);
        Btn_MyAnimesForm.Margin = new Padding(1, 2, 1, 2);
        Btn_MyAnimesForm.Name = "Btn_MyAnimesForm";
        Btn_MyAnimesForm.Size = new Size(272, 233);
        Btn_MyAnimesForm.TabIndex = 14;
        Btn_MyAnimesForm.UseVisualStyleBackColor = false;
        // 
        // Btn_MyMusicxForm
        // 
        Btn_MyMusicxForm.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Btn_MyMusicxForm.BackColor = Color.Transparent;
        Btn_MyMusicxForm.BackgroundImage = Properties.Resources.NotaMusica;
        Btn_MyMusicxForm.BackgroundImageLayout = ImageLayout.Stretch;
        Btn_MyMusicxForm.FlatAppearance.BorderSize = 0;
        Btn_MyMusicxForm.FlatStyle = FlatStyle.Flat;
        Btn_MyMusicxForm.Location = new Point(92, 418);
        Btn_MyMusicxForm.Margin = new Padding(1, 2, 1, 2);
        Btn_MyMusicxForm.Name = "Btn_MyMusicxForm";
        Btn_MyMusicxForm.Size = new Size(61, 171);
        Btn_MyMusicxForm.TabIndex = 15;
        Btn_MyMusicxForm.UseVisualStyleBackColor = false;
        // 
        // Btn_NinoTIForm
        // 
        Btn_NinoTIForm.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Btn_NinoTIForm.BackColor = Color.Transparent;
        Btn_NinoTIForm.BackgroundImage = Properties.Resources.TI_link;
        Btn_NinoTIForm.BackgroundImageLayout = ImageLayout.Stretch;
        Btn_NinoTIForm.FlatAppearance.BorderSize = 0;
        Btn_NinoTIForm.FlatStyle = FlatStyle.Flat;
        Btn_NinoTIForm.Location = new Point(1126, 232);
        Btn_NinoTIForm.Margin = new Padding(1, 2, 1, 2);
        Btn_NinoTIForm.Name = "Btn_NinoTIForm";
        Btn_NinoTIForm.Size = new Size(110, 97);
        Btn_NinoTIForm.TabIndex = 16;
        Btn_NinoTIForm.UseVisualStyleBackColor = false;
        // 
        // Lbl_DescricaoMyAnimes
        // 
        Lbl_DescricaoMyAnimes.Anchor = AnchorStyles.Bottom;
        Lbl_DescricaoMyAnimes.AutoSize = true;
        Lbl_DescricaoMyAnimes.BackColor = Color.Transparent;
        Lbl_DescricaoMyAnimes.Location = new Point(553, 388);
        Lbl_DescricaoMyAnimes.Margin = new Padding(1, 0, 1, 0);
        Lbl_DescricaoMyAnimes.Name = "Lbl_DescricaoMyAnimes";
        Lbl_DescricaoMyAnimes.Size = new Size(243, 105);
        Lbl_DescricaoMyAnimes.TabIndex = 17;
        Lbl_DescricaoMyAnimes.Text = resources.GetString("Lbl_DescricaoMyAnimes.Text");
        // 
        // Lbl_DescricaoMyMusicX
        // 
        Lbl_DescricaoMyMusicX.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Lbl_DescricaoMyMusicX.AutoSize = true;
        Lbl_DescricaoMyMusicX.BackColor = Color.Transparent;
        Lbl_DescricaoMyMusicX.Location = new Point(21, 584);
        Lbl_DescricaoMyMusicX.Margin = new Padding(1, 0, 1, 0);
        Lbl_DescricaoMyMusicX.Name = "Lbl_DescricaoMyMusicX";
        Lbl_DescricaoMyMusicX.Size = new Size(237, 105);
        Lbl_DescricaoMyMusicX.TabIndex = 18;
        Lbl_DescricaoMyMusicX.Text = resources.GetString("Lbl_DescricaoMyMusicX.Text");
        // 
        // Lbl_DescricaoNinoTI
        // 
        Lbl_DescricaoNinoTI.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Lbl_DescricaoNinoTI.AutoSize = true;
        Lbl_DescricaoNinoTI.BackColor = Color.Transparent;
        Lbl_DescricaoNinoTI.Location = new Point(1126, 142);
        Lbl_DescricaoNinoTI.Margin = new Padding(1, 0, 1, 0);
        Lbl_DescricaoNinoTI.Name = "Lbl_DescricaoNinoTI";
        Lbl_DescricaoNinoTI.Size = new Size(118, 105);
        Lbl_DescricaoNinoTI.TabIndex = 19;
        Lbl_DescricaoNinoTI.Text = "NinoTI - Frm_NinoTI\nAreas da T.I\nExibir detalhes\nCertificações...\nCursos...\nCriar estruturas\nMonitorar pastas...";
        // 
        // Frm_WinAppDtudo
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Desktop;
        BackgroundImage = Properties.Resources.code01_background;
        BackgroundImageLayout = ImageLayout.Stretch;
        ClientSize = new Size(1260, 715);
        Controls.Add(Lbl_DescricaoNinoTI);
        Controls.Add(Lbl_DescricaoMyMusicX);
        Controls.Add(Lbl_DescricaoMyAnimes);
        Controls.Add(Btn_NinoTIForm);
        Controls.Add(Btn_MyMusicxForm);
        Controls.Add(Btn_MyAnimesForm);
        Controls.Add(Btn_Site_Dtudo);
        Controls.Add(Lbl_Titulo);
        Controls.Add(Mnu_Principal);
        DoubleBuffered = true;
        Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        ForeColor = Color.Gold;
        FormBorderStyle = FormBorderStyle.None;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = Mnu_Principal;
        Margin = new Padding(2);
        Name = "Frm_WinAppDtudo";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "WinApp Dtudo";
        MouseDown += Frm_WinAppDtudo_MouseDown;
        Mnu_Principal.ResumeLayout(false);
        Mnu_Principal.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label Lbl_Titulo;
    private Button Btn_Site_Dtudo;
    private MenuStrip Mnu_Principal;
    private ToolStripMenuItem MnI_Arquivo;
    private ToolStripMenuItem MnI_Sair;
    private ToolStripMenuItem MnI_MyAnimes;
    private ToolStripMenuItem MnI_MyMusicX;
    private ToolStripMenuItem MnI_NinoTI;
    private ToolStripMenuItem MnI_CadastrarUsuario;
    private ToolStripMenuItem MnI_Conectar;
    private ToolStripMenuItem MnI_Desconectar;
    private PictureBox pictureBox1;
    private RichTextBox richTextBox1;
    private RichTextBox richTextBox2;
    private RichTextBox richTextBox3;
    private Button Btn_MyAnimesForm;
    private Button Btn_MyMusicxForm;
    private Button Btn_NinoTIForm;
    private Label Lbl_DescricaoMyAnimes;
    private Label Lbl_DescricaoMyMusicX;
    private Label Lbl_DescricaoNinoTI;
}
