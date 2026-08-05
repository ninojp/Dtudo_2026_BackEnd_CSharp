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
        Btn_DtudoSite = new Button();
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
        Lbl_DescricaoMyMusicX = new Label();
        Lbl_DescricaoMyAnimes = new Label();
        label1 = new Label();
        label2 = new Label();
        Mnu_Principal.SuspendLayout();
        SuspendLayout();
        // 
        // Lbl_Titulo
        // 
        Lbl_Titulo.Anchor = AnchorStyles.Top;
        Lbl_Titulo.BackColor = Color.Transparent;
        Lbl_Titulo.Font = new Font("Arial Black", 26F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
        Lbl_Titulo.ForeColor = Color.Gold;
        Lbl_Titulo.Location = new Point(801, 52);
        Lbl_Titulo.Margin = new Padding(2, 0, 2, 0);
        Lbl_Titulo.Name = "Lbl_Titulo";
        Lbl_Titulo.Size = new Size(296, 90);
        Lbl_Titulo.TabIndex = 0;
        Lbl_Titulo.Text = "WinAppDtudo";
        Lbl_Titulo.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // Btn_DtudoSite
        // 
        Btn_DtudoSite.Anchor = AnchorStyles.Top;
        Btn_DtudoSite.BackColor = Color.Transparent;
        Btn_DtudoSite.BackgroundImage = (Image)resources.GetObject("Btn_DtudoSite.BackgroundImage");
        Btn_DtudoSite.BackgroundImageLayout = ImageLayout.Stretch;
        Btn_DtudoSite.FlatAppearance.BorderSize = 0;
        Btn_DtudoSite.FlatStyle = FlatStyle.Flat;
        Btn_DtudoSite.Font = new Font("Segoe UI", 11.1F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Btn_DtudoSite.ForeColor = Color.Gold;
        Btn_DtudoSite.Location = new Point(280, 125);
        Btn_DtudoSite.Margin = new Padding(2);
        Btn_DtudoSite.Name = "Btn_DtudoSite";
        Btn_DtudoSite.Size = new Size(242, 86);
        Btn_DtudoSite.TabIndex = 1;
        Btn_DtudoSite.TextAlign = ContentAlignment.TopCenter;
        Btn_DtudoSite.UseVisualStyleBackColor = false;
        Btn_DtudoSite.Click += Btn_DtudoSite_Click;
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
        Mnu_Principal.Size = new Size(1272, 40);
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
        Btn_MyAnimesForm.Font = new Font("Microsoft Sans Serif", 16.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Btn_MyAnimesForm.Location = new Point(532, 462);
        Btn_MyAnimesForm.Margin = new Padding(1, 2, 1, 2);
        Btn_MyAnimesForm.Name = "Btn_MyAnimesForm";
        Btn_MyAnimesForm.Size = new Size(272, 233);
        Btn_MyAnimesForm.TabIndex = 14;
        Btn_MyAnimesForm.TextAlign = ContentAlignment.TopLeft;
        Btn_MyAnimesForm.UseVisualStyleBackColor = false;
        Btn_MyAnimesForm.Click += Btn_MyAnimesForm_Click;
        // 
        // Btn_MyMusicxForm
        // 
        Btn_MyMusicxForm.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Btn_MyMusicxForm.BackColor = Color.Transparent;
        Btn_MyMusicxForm.BackgroundImage = Properties.Resources.NotaMusica;
        Btn_MyMusicxForm.BackgroundImageLayout = ImageLayout.Stretch;
        Btn_MyMusicxForm.FlatAppearance.BorderSize = 0;
        Btn_MyMusicxForm.FlatStyle = FlatStyle.Flat;
        Btn_MyMusicxForm.Location = new Point(83, 375);
        Btn_MyMusicxForm.Margin = new Padding(1, 2, 1, 2);
        Btn_MyMusicxForm.Name = "Btn_MyMusicxForm";
        Btn_MyMusicxForm.Size = new Size(85, 213);
        Btn_MyMusicxForm.TabIndex = 15;
        Btn_MyMusicxForm.UseVisualStyleBackColor = false;
        Btn_MyMusicxForm.Click += Btn_MyMusicxForm_Click;
        // 
        // Btn_NinoTIForm
        // 
        Btn_NinoTIForm.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Btn_NinoTIForm.BackColor = Color.Transparent;
        Btn_NinoTIForm.BackgroundImage = Properties.Resources.TI_link;
        Btn_NinoTIForm.BackgroundImageLayout = ImageLayout.Stretch;
        Btn_NinoTIForm.FlatAppearance.BorderSize = 0;
        Btn_NinoTIForm.FlatStyle = FlatStyle.Flat;
        Btn_NinoTIForm.Font = new Font("Segoe Fluent Icons", 16.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Btn_NinoTIForm.ForeColor = Color.Gold;
        Btn_NinoTIForm.Location = new Point(1044, 273);
        Btn_NinoTIForm.Margin = new Padding(1, 2, 1, 2);
        Btn_NinoTIForm.Name = "Btn_NinoTIForm";
        Btn_NinoTIForm.Size = new Size(144, 120);
        Btn_NinoTIForm.TabIndex = 16;
        Btn_NinoTIForm.TextAlign = ContentAlignment.BottomLeft;
        Btn_NinoTIForm.UseVisualStyleBackColor = false;
        Btn_NinoTIForm.Click += Btn_NinoTIForm_Click;
        // 
        // Lbl_DescricaoMyMusicX
        // 
        Lbl_DescricaoMyMusicX.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Lbl_DescricaoMyMusicX.BackColor = Color.Transparent;
        Lbl_DescricaoMyMusicX.Font = new Font("Segoe Fluent Icons", 28.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Lbl_DescricaoMyMusicX.Location = new Point(52, 552);
        Lbl_DescricaoMyMusicX.Margin = new Padding(1, 0, 1, 0);
        Lbl_DescricaoMyMusicX.Name = "Lbl_DescricaoMyMusicX";
        Lbl_DescricaoMyMusicX.Size = new Size(160, 53);
        Lbl_DescricaoMyMusicX.TabIndex = 18;
        Lbl_DescricaoMyMusicX.Text = "MyMusicX\r\n";
        Lbl_DescricaoMyMusicX.TextAlign = ContentAlignment.TopCenter;
        // 
        // Lbl_DescricaoMyAnimes
        // 
        Lbl_DescricaoMyAnimes.Anchor = AnchorStyles.Bottom;
        Lbl_DescricaoMyAnimes.BackColor = Color.Transparent;
        Lbl_DescricaoMyAnimes.Font = new Font("Segoe Fluent Icons", 28F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Lbl_DescricaoMyAnimes.Location = new Point(588, 425);
        Lbl_DescricaoMyAnimes.Margin = new Padding(1, 0, 1, 0);
        Lbl_DescricaoMyAnimes.Name = "Lbl_DescricaoMyAnimes";
        Lbl_DescricaoMyAnimes.Size = new Size(157, 46);
        Lbl_DescricaoMyAnimes.TabIndex = 17;
        Lbl_DescricaoMyAnimes.Text = "MyAnimes\r\n";
        // 
        // label1
        // 
        label1.Anchor = AnchorStyles.Bottom;
        label1.BackColor = Color.Transparent;
        label1.Font = new Font("Segoe Fluent Icons", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
        label1.Location = new Point(1058, 244);
        label1.Margin = new Padding(1, 0, 1, 0);
        label1.Name = "label1";
        label1.Size = new Size(111, 37);
        label1.TabIndex = 19;
        label1.Text = "Nino T.I";
        // 
        // label2
        // 
        label2.Anchor = AnchorStyles.Bottom;
        label2.BackColor = Color.Transparent;
        label2.Font = new Font("Segoe Fluent Icons", 28F, FontStyle.Bold, GraphicsUnit.Point, 0);
        label2.Location = new Point(333, 91);
        label2.Margin = new Padding(1, 0, 1, 0);
        label2.Name = "label2";
        label2.Size = new Size(157, 42);
        label2.TabIndex = 20;
        label2.Text = "DtudoSite";
        // 
        // Frm_WinAppDtudo
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = SystemColors.Desktop;
        BackgroundImage = Properties.Resources.code01_background;
        BackgroundImageLayout = ImageLayout.Stretch;
        ClientSize = new Size(1272, 692);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(Lbl_DescricaoMyMusicX);
        Controls.Add(Lbl_DescricaoMyAnimes);
        Controls.Add(Btn_NinoTIForm);
        Controls.Add(Btn_MyMusicxForm);
        Controls.Add(Btn_MyAnimesForm);
        Controls.Add(Btn_DtudoSite);
        Controls.Add(Lbl_Titulo);
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
        MouseDown += Frm_WinAppDtudo_MouseDown;
        Mnu_Principal.ResumeLayout(false);
        Mnu_Principal.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label Lbl_Titulo;
    private Button Btn_DtudoSite;
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
    private Label Lbl_DescricaoMyMusicX;
    private Label Lbl_DescricaoMyAnimes;
    private Label label1;
    private Label label2;
}
