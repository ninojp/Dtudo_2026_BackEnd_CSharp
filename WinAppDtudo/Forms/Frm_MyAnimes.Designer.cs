namespace WinAppDtudo;
partial class Frm_MyAnimes
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
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
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_MyAnimes));
        Mnu_MenuMyAnimes = new MenuStrip();
        Mnu_MenuAbas = new ToolStripMenuItem();
        MnI_AbrirAbas = new ToolStripMenuItem();
        MnI_AbaMascaras = new ToolStripMenuItem();
        MnI_FormMsgBox = new ToolStripMenuItem();
        MnI_FecharAbas = new ToolStripMenuItem();
        MnI_FecharAbaAtual = new ToolStripMenuItem();
        MnI_FecharTodasAbas = new ToolStripMenuItem();
        MnI_FecharAbasAEsquerda = new ToolStripMenuItem();
        MnI_FecharAbasADireita = new ToolStripMenuItem();
        MnI_DBLocalBuscarAnime = new ToolStripMenuItem();
        MnI_ApiMyAnimeListBuscarNome = new ToolStripMenuItem();
        Mnu_AnalizarEstruturas = new ToolStripMenuItem();
        Tbc_MyAnimes = new TabControl();
        Iml_ImagensList = new ImageList(components);
        Mnu_MenuMyAnimes.SuspendLayout();
        SuspendLayout();
        // 
        // Mnu_MenuMyAnimes
        // 
        Mnu_MenuMyAnimes.BackColor = Color.Black;
        Mnu_MenuMyAnimes.ImageScalingSize = new Size(32, 32);
        Mnu_MenuMyAnimes.Items.AddRange(new ToolStripItem[] { Mnu_MenuAbas, MnI_DBLocalBuscarAnime, MnI_ApiMyAnimeListBuscarNome, Mnu_AnalizarEstruturas });
        Mnu_MenuMyAnimes.Location = new Point(0, 0);
        Mnu_MenuMyAnimes.Name = "Mnu_MenuMyAnimes";
        Mnu_MenuMyAnimes.Padding = new Padding(7, 2, 0, 2);
        Mnu_MenuMyAnimes.RenderMode = ToolStripRenderMode.Professional;
        Mnu_MenuMyAnimes.Size = new Size(997, 40);
        Mnu_MenuMyAnimes.TabIndex = 0;
        Mnu_MenuMyAnimes.Text = "menuMyAnimes";
        // 
        // Mnu_MenuAbas
        // 
        Mnu_MenuAbas.DropDownItems.AddRange(new ToolStripItem[] { MnI_AbrirAbas, MnI_FecharAbas });
        Mnu_MenuAbas.Font = new Font("Segoe UI Black", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Mnu_MenuAbas.Image = Properties.Resources.YingYang_HD;
        Mnu_MenuAbas.Name = "Mnu_MenuAbas";
        Mnu_MenuAbas.Size = new Size(117, 36);
        Mnu_MenuAbas.Text = "Arquivo";
        // 
        // MnI_AbrirAbas
        // 
        MnI_AbrirAbas.DropDownItems.AddRange(new ToolStripItem[] { MnI_AbaMascaras, MnI_FormMsgBox });
        MnI_AbrirAbas.Image = Properties.Resources.MaskVendettaReal;
        MnI_AbrirAbas.Name = "MnI_AbrirAbas";
        MnI_AbrirAbas.Size = new Size(175, 26);
        MnI_AbrirAbas.Text = "Abrir Abas";
        // 
        // MnI_AbaMascaras
        // 
        MnI_AbaMascaras.Image = Properties.Resources.MaskV;
        MnI_AbaMascaras.Name = "MnI_AbaMascaras";
        MnI_AbaMascaras.Size = new Size(186, 26);
        MnI_AbaMascaras.Text = "AbaMascaras";
        MnI_AbaMascaras.Click += MnI_AbaMascaras_Click;
        // 
        // MnI_FormMsgBox
        // 
        MnI_FormMsgBox.Image = Properties.Resources.InterrogacaoBrasil;
        MnI_FormMsgBox.Name = "MnI_FormMsgBox";
        MnI_FormMsgBox.Size = new Size(186, 26);
        MnI_FormMsgBox.Text = "FormMsgBox";
        MnI_FormMsgBox.Click += MnI_FormMsgBox_Click;
        // 
        // MnI_FecharAbas
        // 
        MnI_FecharAbas.DropDownItems.AddRange(new ToolStripItem[] { MnI_FecharAbaAtual, MnI_FecharTodasAbas, MnI_FecharAbasAEsquerda, MnI_FecharAbasADireita });
        MnI_FecharAbas.Image = Properties.Resources.SlaveMoney;
        MnI_FecharAbas.Name = "MnI_FecharAbas";
        MnI_FecharAbas.Size = new Size(175, 26);
        MnI_FecharAbas.Text = "Fechar Abas";
        // 
        // MnI_FecharAbaAtual
        // 
        MnI_FecharAbaAtual.Name = "MnI_FecharAbaAtual";
        MnI_FecharAbaAtual.Size = new Size(264, 26);
        MnI_FecharAbaAtual.Text = "Fechar Aba Atual";
        MnI_FecharAbaAtual.Click += MnI_FecharAbaAtual_Click;
        // 
        // MnI_FecharTodasAbas
        // 
        MnI_FecharTodasAbas.Name = "MnI_FecharTodasAbas";
        MnI_FecharTodasAbas.Size = new Size(264, 26);
        MnI_FecharTodasAbas.Text = "Fechar Todas Abas";
        MnI_FecharTodasAbas.Click += MnI_FecharTodasAbas_Click;
        // 
        // MnI_FecharAbasAEsquerda
        // 
        MnI_FecharAbasAEsquerda.Name = "MnI_FecharAbasAEsquerda";
        MnI_FecharAbasAEsquerda.Size = new Size(264, 26);
        MnI_FecharAbasAEsquerda.Text = "Fechar Abas à Esquerda";
        MnI_FecharAbasAEsquerda.Click += MnI_FecharAbasAEsquerda_Click;
        // 
        // MnI_FecharAbasADireita
        // 
        MnI_FecharAbasADireita.Name = "MnI_FecharAbasADireita";
        MnI_FecharAbasADireita.Size = new Size(264, 26);
        MnI_FecharAbasADireita.Text = "Fechar Abas à Direita";
        MnI_FecharAbasADireita.Click += MnI_FecharAbasADireita_Click;
        // 
        // MnI_DBLocalBuscarAnime
        // 
        MnI_DBLocalBuscarAnime.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        MnI_DBLocalBuscarAnime.Image = Properties.Resources.Vvendetta;
        MnI_DBLocalBuscarAnime.Name = "MnI_DBLocalBuscarAnime";
        MnI_DBLocalBuscarAnime.Size = new Size(120, 36);
        MnI_DBLocalBuscarAnime.Text = "DB Local";
        // 
        // MnI_ApiMyAnimeListBuscarNome
        // 
        MnI_ApiMyAnimeListBuscarNome.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        MnI_ApiMyAnimeListBuscarNome.Image = Properties.Resources.MyAnimeList_Logo;
        MnI_ApiMyAnimeListBuscarNome.Name = "MnI_ApiMyAnimeListBuscarNome";
        MnI_ApiMyAnimeListBuscarNome.Size = new Size(180, 36);
        MnI_ApiMyAnimeListBuscarNome.Text = "ApiMyAnimeList";
        // 
        // Mnu_AnalizarEstruturas
        // 
        Mnu_AnalizarEstruturas.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Mnu_AnalizarEstruturas.Image = Properties.Resources.MaskVendettaReal;
        Mnu_AnalizarEstruturas.Name = "Mnu_AnalizarEstruturas";
        Mnu_AnalizarEstruturas.Size = new Size(165, 36);
        Mnu_AnalizarEstruturas.Text = "AnalizarPastas";
        Mnu_AnalizarEstruturas.Click += Mnu_AnalizarEstruturas_Click;
        // 
        // Tbc_MyAnimes
        // 
        Tbc_MyAnimes.AccessibleName = "Controle de Abas";
        Tbc_MyAnimes.AllowDrop = true;
        Tbc_MyAnimes.Dock = DockStyle.Fill;
        Tbc_MyAnimes.DrawMode = TabDrawMode.OwnerDrawFixed;
        Tbc_MyAnimes.Font = new Font("Microsoft Sans Serif", 9.3F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Tbc_MyAnimes.ImageList = Iml_ImagensList;
        Tbc_MyAnimes.ImeMode = ImeMode.On;
        Tbc_MyAnimes.ItemSize = new Size(200, 50);
        Tbc_MyAnimes.Location = new Point(0, 40);
        Tbc_MyAnimes.Margin = new Padding(0);
        Tbc_MyAnimes.Name = "Tbc_MyAnimes";
        Tbc_MyAnimes.Padding = new Point(10, 6);
        Tbc_MyAnimes.SelectedIndex = 0;
        Tbc_MyAnimes.Size = new Size(997, 632);
        Tbc_MyAnimes.SizeMode = TabSizeMode.Fixed;
        Tbc_MyAnimes.TabIndex = 1;
        Tbc_MyAnimes.DrawItem += Tbc_MyAnimes_DrawItem;
        Tbc_MyAnimes.MouseDown += Tbc_MyAnimes_MouseDown;
        // 
        // Iml_ImagensList
        // 
        Iml_ImagensList.ColorDepth = ColorDepth.Depth32Bit;
        Iml_ImagensList.ImageStream = (ImageListStreamer)resources.GetObject("Iml_ImagensList.ImageStream");
        Iml_ImagensList.TransparentColor = Color.Transparent;
        Iml_ImagensList.Images.SetKeyName(0, "pngwing.com.png");
        Iml_ImagensList.Images.SetKeyName(1, "RadioAtivo.png");
        Iml_ImagensList.Images.SetKeyName(2, "RosaDosVentos.png");
        Iml_ImagensList.Images.SetKeyName(3, "Vvendetta.png");
        // 
        // Frm_MyAnimes
        // 
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(6F, 13F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        AutoSize = true;
        BackColor = SystemColors.Desktop;
        BackgroundImage = Properties.Resources.AnimesElas;
        BackgroundImageLayout = ImageLayout.Stretch;
        ClientSize = new Size(997, 672);
        Controls.Add(Tbc_MyAnimes);
        Controls.Add(Mnu_MenuMyAnimes);
        DoubleBuffered = true;
        ForeColor = Color.Gold;
        FormBorderStyle = FormBorderStyle.None;
        Icon = (Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = Mnu_MenuMyAnimes;
        Margin = new Padding(2);
        Name = "Frm_MyAnimes";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MyAnimes - Abas";
        Load += Frm_MyAnimes_Load;
        Mnu_MenuMyAnimes.ResumeLayout(false);
        Mnu_MenuMyAnimes.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip Mnu_MenuMyAnimes;
    private ToolStripMenuItem MnI_DBLocalBuscarAnime;
    private TabControl Tbc_MyAnimes;
    private ImageList Iml_ImagensList;
    private ToolStripMenuItem Mnu_MenuAbas;
    private ToolStripMenuItem MnI_AbrirAbas;
    private ToolStripMenuItem MnI_AbaMascaras;
    private ToolStripMenuItem MnI_FecharAbas;
    private ToolStripMenuItem MnI_FecharTodasAbas;
    private ToolStripMenuItem MnI_FecharAbaAtual;
    private ToolStripMenuItem MnI_FecharAbasAEsquerda;
    private ToolStripMenuItem MnI_FecharAbasADireita;
    private ToolStripMenuItem MnI_FormMsgBox;
    private ToolStripMenuItem Mnu_AnalizarEstruturas;
    private ToolStripMenuItem MnI_ApiMyAnimeListBuscarNome;
}
