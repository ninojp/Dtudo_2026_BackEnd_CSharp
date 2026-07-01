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
        Btn_Sair_App = new Button();
        Btn_Abrir_Form = new Button();
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
        Lbl_Titulo.Location = new Point(407, 52);
        Lbl_Titulo.Margin = new Padding(4, 0, 4, 0);
        Lbl_Titulo.Name = "Lbl_Titulo";
        Lbl_Titulo.Size = new Size(424, 30);
        Lbl_Titulo.TabIndex = 0;
        Lbl_Titulo.Text = "WinApp Dtudo - Controle de Arquivos";
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
        Btn_Site_Dtudo.Location = new Point(341, 255);
        Btn_Site_Dtudo.Margin = new Padding(4, 5, 4, 5);
        Btn_Site_Dtudo.Name = "Btn_Site_Dtudo";
        Btn_Site_Dtudo.Size = new Size(444, 167);
        Btn_Site_Dtudo.TabIndex = 1;
        Btn_Site_Dtudo.Text = "Abrir FrontEnd Dtudo";
        Btn_Site_Dtudo.TextAlign = ContentAlignment.TopRight;
        Btn_Site_Dtudo.UseVisualStyleBackColor = false;
        Btn_Site_Dtudo.Click += Btn_Site_Dtudo_Click;
        // 
        // Btn_Sair_App
        // 
        Btn_Sair_App.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        Btn_Sair_App.BackColor = Color.Transparent;
        Btn_Sair_App.FlatAppearance.BorderSize = 0;
        Btn_Sair_App.FlatStyle = FlatStyle.Flat;
        Btn_Sair_App.Font = new Font("Segoe UI", 9.900001F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Btn_Sair_App.ForeColor = Color.Gold;
        Btn_Sair_App.Location = new Point(929, 651);
        Btn_Sair_App.Margin = new Padding(4, 5, 4, 5);
        Btn_Sair_App.Name = "Btn_Sair_App";
        Btn_Sair_App.Size = new Size(150, 46);
        Btn_Sair_App.TabIndex = 2;
        Btn_Sair_App.Text = "Sair do App";
        Btn_Sair_App.UseVisualStyleBackColor = false;
        Btn_Sair_App.Click += Btn_Sair_App_Click;
        // 
        // Btn_Abrir_Form
        // 
        Btn_Abrir_Form.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Btn_Abrir_Form.BackColor = Color.Transparent;
        Btn_Abrir_Form.BackgroundImage = (Image)resources.GetObject("Btn_Abrir_Form.BackgroundImage");
        Btn_Abrir_Form.BackgroundImageLayout = ImageLayout.Stretch;
        Btn_Abrir_Form.FlatAppearance.BorderSize = 0;
        Btn_Abrir_Form.FlatStyle = FlatStyle.Flat;
        Btn_Abrir_Form.Font = new Font("Arial", 9.900001F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Btn_Abrir_Form.ForeColor = Color.Gold;
        Btn_Abrir_Form.ImageAlign = ContentAlignment.TopLeft;
        Btn_Abrir_Form.Location = new Point(62, 601);
        Btn_Abrir_Form.Margin = new Padding(4, 5, 4, 5);
        Btn_Abrir_Form.Name = "Btn_Abrir_Form";
        Btn_Abrir_Form.Size = new Size(156, 129);
        Btn_Abrir_Form.TabIndex = 5;
        Btn_Abrir_Form.Text = "FormTest";
        Btn_Abrir_Form.TextAlign = ContentAlignment.BottomRight;
        Btn_Abrir_Form.UseVisualStyleBackColor = false;
        Btn_Abrir_Form.Click += Btn_Abrir_Form_Click;
        // 
        // Mnu_Principal
        // 
        Mnu_Principal.BackColor = Color.DimGray;
        Mnu_Principal.BackgroundImageLayout = ImageLayout.None;
        Mnu_Principal.Font = new Font("Segoe UI", 12.8571434F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Mnu_Principal.Items.AddRange(new ToolStripItem[] { MnI_Arquivo, MnI_MyAnimes, MnI_MyMusicX, MnI_NinoTI });
        Mnu_Principal.Location = new Point(0, 0);
        Mnu_Principal.Name = "Mnu_Principal";
        Mnu_Principal.Padding = new Padding(9, 4, 0, 4);
        Mnu_Principal.Size = new Size(1142, 37);
        Mnu_Principal.TabIndex = 7;
        Mnu_Principal.Text = "MenuPrincipal";
        // 
        // MnI_Arquivo
        // 
        MnI_Arquivo.DropDownItems.AddRange(new ToolStripItem[] { MnI_Abrir, MnI_CadastrarUsuario, MnI_Conectar, MnI_Desconectar, MnI_Sair });
        MnI_Arquivo.Image = Properties.Resources.MaskV;
        MnI_Arquivo.Name = "MnI_Arquivo";
        MnI_Arquivo.Size = new Size(108, 29);
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
        MnI_Abrir.Size = new Size(235, 30);
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
        MnI_FormTest.Size = new Size(288, 30);
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
        MnI_FormHelloWorld.Size = new Size(288, 30);
        MnI_FormHelloWorld.Text = "Form&HelloWorld";
        MnI_FormHelloWorld.Click += MnI_FormHelloWorld_Click;
        // 
        // MnI_CadastrarUsuario
        // 
        MnI_CadastrarUsuario.ForeColor = Color.Gold;
        MnI_CadastrarUsuario.Image = Properties.Resources.CaveraMetal;
        MnI_CadastrarUsuario.Name = "MnI_CadastrarUsuario";
        MnI_CadastrarUsuario.Size = new Size(235, 30);
        MnI_CadastrarUsuario.Text = "Cadastrar Usuário";
        MnI_CadastrarUsuario.Click += MnI_CadastrarUsuario_Click;
        // 
        // MnI_Conectar
        // 
        MnI_Conectar.ForeColor = Color.Gold;
        MnI_Conectar.Image = Properties.Resources.CaveraMetal;
        MnI_Conectar.Name = "MnI_Conectar";
        MnI_Conectar.Size = new Size(235, 30);
        MnI_Conectar.Text = "Conectar";
        MnI_Conectar.Click += MnI_Conectar_Click;
        // 
        // MnI_Desconectar
        // 
        MnI_Desconectar.ForeColor = Color.Gold;
        MnI_Desconectar.Image = Properties.Resources.CaveraMetal;
        MnI_Desconectar.Name = "MnI_Desconectar";
        MnI_Desconectar.Size = new Size(235, 30);
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
        MnI_Sair.Size = new Size(235, 30);
        MnI_Sair.Text = "&Sair";
        MnI_Sair.Click += MnI_Sair_Click;
        // 
        // MnI_MyAnimes
        // 
        MnI_MyAnimes.Image = Properties.Resources.TI_link;
        MnI_MyAnimes.Name = "MnI_MyAnimes";
        MnI_MyAnimes.Size = new Size(130, 29);
        MnI_MyAnimes.Text = "MyAnimes";
        MnI_MyAnimes.Click += MnI_MyAnimes_Click;
        // 
        // MnI_MyMusicX
        // 
        MnI_MyMusicX.Image = Properties.Resources.NotaMusica;
        MnI_MyMusicX.Name = "MnI_MyMusicX";
        MnI_MyMusicX.Size = new Size(129, 29);
        MnI_MyMusicX.Text = "MyMusicX";
        MnI_MyMusicX.Click += MnI_MyMusicX_Click;
        // 
        // MnI_NinoTI
        // 
        MnI_NinoTI.Image = Properties.Resources.OlhoBRHacker1024;
        MnI_NinoTI.Name = "MnI_NinoTI";
        MnI_NinoTI.Size = new Size(98, 29);
        MnI_NinoTI.Text = "NinoTI";
        // 
        // Frm_WinAppDtudo
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Desktop;
        BackgroundImage = Properties.Resources.code01_background;
        BackgroundImageLayout = ImageLayout.Stretch;
        ClientSize = new Size(1142, 750);
        Controls.Add(Btn_Abrir_Form);
        Controls.Add(Btn_Sair_App);
        Controls.Add(Btn_Site_Dtudo);
        Controls.Add(Lbl_Titulo);
        Controls.Add(Mnu_Principal);
        DoubleBuffered = true;
        ForeColor = Color.Gold;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = Mnu_Principal;
        Margin = new Padding(4, 5, 4, 5);
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
    private Button Btn_Sair_App;
    private Button Btn_Abrir_Form;
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
}
