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
        MnI_Abrir = new ToolStripMenuItem();
        MnI_FormTest = new ToolStripMenuItem();
        MnI_FormHelloWorld = new ToolStripMenuItem();
        MnI_CadastrarUsuario = new ToolStripMenuItem();
        MnI_Conectar = new ToolStripMenuItem();
        MnI_Desconectar = new ToolStripMenuItem();
        MnI_Sair = new ToolStripMenuItem();
        MnI_MyAnimes = new ToolStripMenuItem();
        MnI_MyMusicX = new ToolStripMenuItem();
        MnI_NinoTI = new ToolStripMenuItem();
        pictureBox1 = new PictureBox();
        richTextBox1 = new RichTextBox();
        pictureBox2 = new PictureBox();
        richTextBox2 = new RichTextBox();
        pictureBox3 = new PictureBox();
        richTextBox3 = new RichTextBox();
        Mnu_Principal.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
        ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
        ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
        SuspendLayout();
        // 
        // Lbl_Titulo
        // 
        Lbl_Titulo.Anchor = AnchorStyles.Top;
        Lbl_Titulo.AutoSize = true;
        Lbl_Titulo.BackColor = Color.Transparent;
        Lbl_Titulo.Font = new Font("Segoe UI Black", 15.9000006F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
        Lbl_Titulo.ForeColor = Color.Gold;
        Lbl_Titulo.Location = new Point(1034, 147);
        Lbl_Titulo.Margin = new Padding(5, 0, 5, 0);
        Lbl_Titulo.Name = "Lbl_Titulo";
        Lbl_Titulo.Size = new Size(714, 59);
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
        Btn_Site_Dtudo.Location = new Point(412, 72);
        Btn_Site_Dtudo.Margin = new Padding(5);
        Btn_Site_Dtudo.Name = "Btn_Site_Dtudo";
        Btn_Site_Dtudo.Size = new Size(577, 174);
        Btn_Site_Dtudo.TabIndex = 1;
        Btn_Site_Dtudo.Text = "DtudoSite";
        Btn_Site_Dtudo.TextAlign = ContentAlignment.TopCenter;
        Btn_Site_Dtudo.UseVisualStyleBackColor = false;
        Btn_Site_Dtudo.Click += Btn_Site_Dtudo_Click;
        // 
        // Mnu_Principal
        // 
        Mnu_Principal.BackColor = Color.DimGray;
        Mnu_Principal.BackgroundImageLayout = ImageLayout.None;
        Mnu_Principal.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Mnu_Principal.ImageScalingSize = new Size(32, 32);
        Mnu_Principal.Items.AddRange(new ToolStripItem[] { MnI_Arquivo, MnI_MyAnimes, MnI_MyMusicX, MnI_NinoTI });
        Mnu_Principal.Location = new Point(0, 0);
        Mnu_Principal.Name = "Mnu_Principal";
        Mnu_Principal.Padding = new Padding(12, 4, 0, 4);
        Mnu_Principal.RenderMode = ToolStripRenderMode.Professional;
        Mnu_Principal.Size = new Size(1920, 57);
        Mnu_Principal.TabIndex = 7;
        Mnu_Principal.Text = "MenuPrincipal";
        // 
        // MnI_Arquivo
        // 
        MnI_Arquivo.BackColor = Color.Transparent;
        MnI_Arquivo.DropDownItems.AddRange(new ToolStripItem[] { MnI_Abrir, MnI_CadastrarUsuario, MnI_Conectar, MnI_Desconectar, MnI_Sair });
        MnI_Arquivo.Image = Properties.Resources.YingYang_HD;
        MnI_Arquivo.Name = "MnI_Arquivo";
        MnI_Arquivo.Size = new Size(193, 49);
        MnI_Arquivo.Text = "Arquivo";
        // 
        // MnI_Abrir
        // 
        MnI_Abrir.BackColor = Color.Transparent;
        MnI_Abrir.DropDownItems.AddRange(new ToolStripItem[] { MnI_FormTest, MnI_FormHelloWorld });
        MnI_Abrir.ForeColor = Color.Gold;
        MnI_Abrir.Image = Properties.Resources.YingYang_HD;
        MnI_Abrir.Name = "MnI_Abrir";
        MnI_Abrir.ShortcutKeys = Keys.Alt | Keys.A;
        MnI_Abrir.Size = new Size(426, 54);
        MnI_Abrir.Text = "&Abrir";
        // 
        // MnI_FormTest
        // 
        MnI_FormTest.BackColor = Color.Transparent;
        MnI_FormTest.ForeColor = Color.Gold;
        MnI_FormTest.Image = Properties.Resources.TI_link;
        MnI_FormTest.Name = "MnI_FormTest";
        MnI_FormTest.ShortcutKeyDisplayString = "";
        MnI_FormTest.ShortcutKeys = Keys.Alt | Keys.T;
        MnI_FormTest.Size = new Size(523, 54);
        MnI_FormTest.Text = "Form&Test";
        MnI_FormTest.Click += MnI_FormTest_Click;
        // 
        // MnI_FormHelloWorld
        // 
        MnI_FormHelloWorld.BackColor = Color.Transparent;
        MnI_FormHelloWorld.BackgroundImageLayout = ImageLayout.None;
        MnI_FormHelloWorld.ForeColor = Color.Gold;
        MnI_FormHelloWorld.Image = Properties.Resources.OlhoBRHacker1024;
        MnI_FormHelloWorld.Name = "MnI_FormHelloWorld";
        MnI_FormHelloWorld.ShortcutKeys = Keys.Alt | Keys.H;
        MnI_FormHelloWorld.Size = new Size(523, 54);
        MnI_FormHelloWorld.Text = "Form&HelloWorld";
        MnI_FormHelloWorld.Click += MnI_FormHelloWorld_Click;
        // 
        // MnI_CadastrarUsuario
        // 
        MnI_CadastrarUsuario.ForeColor = Color.Gold;
        MnI_CadastrarUsuario.Image = Properties.Resources.CaveraMetal;
        MnI_CadastrarUsuario.Name = "MnI_CadastrarUsuario";
        MnI_CadastrarUsuario.Size = new Size(426, 54);
        MnI_CadastrarUsuario.Text = "Cadastrar Usuário";
        MnI_CadastrarUsuario.Click += MnI_CadastrarUsuario_Click;
        // 
        // MnI_Conectar
        // 
        MnI_Conectar.ForeColor = Color.Gold;
        MnI_Conectar.Image = Properties.Resources.CaveraMetal;
        MnI_Conectar.Name = "MnI_Conectar";
        MnI_Conectar.Size = new Size(426, 54);
        MnI_Conectar.Text = "Conectar";
        MnI_Conectar.Click += MnI_Conectar_Click;
        // 
        // MnI_Desconectar
        // 
        MnI_Desconectar.ForeColor = Color.Gold;
        MnI_Desconectar.Image = Properties.Resources.CaveraMetal;
        MnI_Desconectar.Name = "MnI_Desconectar";
        MnI_Desconectar.Size = new Size(426, 54);
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
        MnI_Sair.Size = new Size(426, 54);
        MnI_Sair.Text = "&Sair";
        MnI_Sair.Click += MnI_Sair_Click;
        // 
        // MnI_MyAnimes
        // 
        MnI_MyAnimes.Image = Properties.Resources.TI_link;
        MnI_MyAnimes.Name = "MnI_MyAnimes";
        MnI_MyAnimes.Size = new Size(231, 49);
        MnI_MyAnimes.Text = "MyAnimes";
        MnI_MyAnimes.Click += MnI_MyAnimes_Click;
        // 
        // MnI_MyMusicX
        // 
        MnI_MyMusicX.Image = Properties.Resources.NotaMusica;
        MnI_MyMusicX.Name = "MnI_MyMusicX";
        MnI_MyMusicX.Size = new Size(229, 49);
        MnI_MyMusicX.Text = "MyMusicX";
        MnI_MyMusicX.Click += MnI_MyMusicX_Click;
        // 
        // MnI_NinoTI
        // 
        MnI_NinoTI.Image = Properties.Resources.OlhoBRHacker1024;
        MnI_NinoTI.Name = "MnI_NinoTI";
        MnI_NinoTI.Size = new Size(174, 49);
        MnI_NinoTI.Text = "NinoTI";
        // 
        // pictureBox1
        // 
        pictureBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        pictureBox1.BackColor = Color.Transparent;
        pictureBox1.BackgroundImage = Properties.Resources.onePieceGroup;
        pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
        pictureBox1.Location = new Point(0, 615);
        pictureBox1.Name = "pictureBox1";
        pictureBox1.Size = new Size(638, 467);
        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBox1.TabIndex = 8;
        pictureBox1.TabStop = false;
        // 
        // richTextBox1
        // 
        richTextBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        richTextBox1.BackColor = SystemColors.Desktop;
        richTextBox1.BorderStyle = BorderStyle.FixedSingle;
        richTextBox1.Font = new Font("Microsoft Sans Serif", 10.85F, FontStyle.Regular, GraphicsUnit.Point, 0);
        richTextBox1.ForeColor = SystemColors.Info;
        richTextBox1.Location = new Point(27, 397);
        richTextBox1.Name = "richTextBox1";
        richTextBox1.Size = new Size(593, 243);
        richTextBox1.TabIndex = 9;
        richTextBox1.Text = resources.GetString("richTextBox1.Text");
        // 
        // pictureBox2
        // 
        pictureBox2.Anchor = AnchorStyles.Bottom;
        pictureBox2.BackColor = Color.Transparent;
        pictureBox2.Image = Properties.Resources.NotaMusica;
        pictureBox2.Location = new Point(858, 254);
        pictureBox2.Name = "pictureBox2";
        pictureBox2.Size = new Size(327, 588);
        pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBox2.TabIndex = 10;
        pictureBox2.TabStop = false;
        // 
        // richTextBox2
        // 
        richTextBox2.Anchor = AnchorStyles.Bottom;
        richTextBox2.BackColor = SystemColors.Desktop;
        richTextBox2.BorderStyle = BorderStyle.FixedSingle;
        richTextBox2.Font = new Font("Microsoft Sans Serif", 10.85F, FontStyle.Regular, GraphicsUnit.Point, 0);
        richTextBox2.ForeColor = SystemColors.Info;
        richTextBox2.Location = new Point(721, 839);
        richTextBox2.Name = "richTextBox2";
        richTextBox2.Size = new Size(610, 243);
        richTextBox2.TabIndex = 11;
        richTextBox2.Text = resources.GetString("richTextBox2.Text");
        // 
        // pictureBox3
        // 
        pictureBox3.BackColor = Color.Transparent;
        pictureBox3.Image = Properties.Resources.TI_link;
        pictureBox3.Location = new Point(1524, 541);
        pictureBox3.Name = "pictureBox3";
        pictureBox3.Size = new Size(309, 252);
        pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBox3.TabIndex = 12;
        pictureBox3.TabStop = false;
        // 
        // richTextBox3
        // 
        richTextBox3.Anchor = AnchorStyles.Bottom;
        richTextBox3.BackColor = SystemColors.Desktop;
        richTextBox3.BorderStyle = BorderStyle.FixedSingle;
        richTextBox3.Font = new Font("Microsoft Sans Serif", 10.85F, FontStyle.Regular, GraphicsUnit.Point, 0);
        richTextBox3.ForeColor = SystemColors.Info;
        richTextBox3.Location = new Point(1447, 323);
        richTextBox3.Name = "richTextBox3";
        richTextBox3.Size = new Size(461, 243);
        richTextBox3.TabIndex = 13;
        richTextBox3.Text = "NinoTI Central\nAtravés de Areas da T.I\nExibimos detalhes do assunto\nCertificações...\nCursos...\nCriar estruturas, pastas e arquivos\nMonitorar e manipular...";
        // 
        // Frm_WinAppDtudo
        // 
        AutoScaleDimensions = new SizeF(13F, 26F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Desktop;
        BackgroundImage = Properties.Resources.code01_background;
        BackgroundImageLayout = ImageLayout.Stretch;
        ClientSize = new Size(1920, 1080);
        Controls.Add(richTextBox3);
        Controls.Add(pictureBox3);
        Controls.Add(richTextBox2);
        Controls.Add(pictureBox2);
        Controls.Add(richTextBox1);
        Controls.Add(pictureBox1);
        Controls.Add(Btn_Site_Dtudo);
        Controls.Add(Lbl_Titulo);
        Controls.Add(Mnu_Principal);
        DoubleBuffered = true;
        ForeColor = Color.Gold;
        FormBorderStyle = FormBorderStyle.None;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = Mnu_Principal;
        Margin = new Padding(5);
        Name = "Frm_WinAppDtudo";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "WinApp Dtudo";
        MouseDown += Frm_WinAppDtudo_MouseDown;
        Mnu_Principal.ResumeLayout(false);
        Mnu_Principal.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
        ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
        ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label Lbl_Titulo;
    private Button Btn_Site_Dtudo;
    private MenuStrip Mnu_Principal;
    private ToolStripMenuItem MnI_Arquivo;
    private ToolStripMenuItem MnI_Abrir;
    private ToolStripMenuItem MnI_Sair;
    private ToolStripMenuItem MnI_FormTest;
    private ToolStripMenuItem MnI_FormHelloWorld;
    private ToolStripMenuItem MnI_MyAnimes;
    private ToolStripMenuItem MnI_MyMusicX;
    private ToolStripMenuItem MnI_NinoTI;
    private ToolStripMenuItem MnI_CadastrarUsuario;
    private ToolStripMenuItem MnI_Conectar;
    private ToolStripMenuItem MnI_Desconectar;
    private PictureBox pictureBox1;
    private RichTextBox richTextBox1;
    private PictureBox pictureBox2;
    private RichTextBox richTextBox2;
    private PictureBox pictureBox3;
    private RichTextBox richTextBox3;
}
