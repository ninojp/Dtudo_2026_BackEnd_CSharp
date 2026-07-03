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
        AbasToolStripMenuItem = new ToolStripMenuItem();
        MnI_AbrirAbas = new ToolStripMenuItem();
        MnI_AbaMascaras = new ToolStripMenuItem();
        MnI_FormMsgBox = new ToolStripMenuItem();
        MnI_FecharAbas = new ToolStripMenuItem();
        MnI_FecharAbaAtual = new ToolStripMenuItem();
        MnI_FecharTodasAbas = new ToolStripMenuItem();
        MnI_FecharAbasAEsquerda = new ToolStripMenuItem();
        MnI_FecharAbasADireita = new ToolStripMenuItem();
        MnI_ProcurarAnimePorNome = new ToolStripMenuItem();
        MnI_ProcurarAnimePorID = new ToolStripMenuItem();
        Tbc_MyAnimes = new TabControl();
        Iml_ImagensList = new ImageList(components);
        Mnu_MenuMyAnimes.SuspendLayout();
        SuspendLayout();
        // 
        // Mnu_MenuMyAnimes
        // 
        Mnu_MenuMyAnimes.ImageScalingSize = new Size(32, 32);
        Mnu_MenuMyAnimes.Items.AddRange(new ToolStripItem[] { AbasToolStripMenuItem, MnI_ProcurarAnimePorNome, MnI_ProcurarAnimePorID });
        Mnu_MenuMyAnimes.Location = new Point(0, 0);
        Mnu_MenuMyAnimes.Name = "Mnu_MenuMyAnimes";
        Mnu_MenuMyAnimes.Padding = new Padding(15, 4, 0, 4);
        Mnu_MenuMyAnimes.Size = new Size(1278, 53);
        Mnu_MenuMyAnimes.TabIndex = 0;
        Mnu_MenuMyAnimes.Text = "menuMyAnimes";
        // 
        // AbasToolStripMenuItem
        // 
        AbasToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { MnI_AbrirAbas, MnI_FecharAbas });
        AbasToolStripMenuItem.Image = Properties.Resources.MaskVendettaReal;
        AbasToolStripMenuItem.Name = "AbasToolStripMenuItem";
        AbasToolStripMenuItem.Size = new Size(135, 45);
        AbasToolStripMenuItem.Text = "Abas";
        // 
        // MnI_AbrirAbas
        // 
        MnI_AbrirAbas.DropDownItems.AddRange(new ToolStripItem[] { MnI_AbaMascaras, MnI_FormMsgBox });
        MnI_AbrirAbas.Image = Properties.Resources.MaskVendettaReal;
        MnI_AbrirAbas.Name = "MnI_AbrirAbas";
        MnI_AbrirAbas.Size = new Size(315, 50);
        MnI_AbrirAbas.Text = "Abrir Abas";
        // 
        // MnI_AbaMascaras
        // 
        MnI_AbaMascaras.Image = Properties.Resources.MaskV;
        MnI_AbaMascaras.Name = "MnI_AbaMascaras";
        MnI_AbaMascaras.Size = new Size(331, 50);
        MnI_AbaMascaras.Text = "AbaMascaras";
        MnI_AbaMascaras.Click += MnI_AbaMascaras_Click;
        // 
        // MnI_FormMsgBox
        // 
        MnI_FormMsgBox.Image = Properties.Resources.InterrogacaoBrasil;
        MnI_FormMsgBox.Name = "MnI_FormMsgBox";
        MnI_FormMsgBox.Size = new Size(331, 50);
        MnI_FormMsgBox.Text = "FormMsgBox";
        MnI_FormMsgBox.Click += MnI_FormMsgBox_Click;
        // 
        // MnI_FecharAbas
        // 
        MnI_FecharAbas.DropDownItems.AddRange(new ToolStripItem[] { MnI_FecharAbaAtual, MnI_FecharTodasAbas, MnI_FecharAbasAEsquerda, MnI_FecharAbasADireita });
        MnI_FecharAbas.Image = Properties.Resources.SlaveMoney;
        MnI_FecharAbas.Name = "MnI_FecharAbas";
        MnI_FecharAbas.Size = new Size(315, 50);
        MnI_FecharAbas.Text = "Fechar Abas";
        // 
        // MnI_FecharAbaAtual
        // 
        MnI_FecharAbaAtual.Name = "MnI_FecharAbaAtual";
        MnI_FecharAbaAtual.Size = new Size(468, 50);
        MnI_FecharAbaAtual.Text = "Fechar Aba Atual";
        MnI_FecharAbaAtual.Click += MnI_FecharAbaAtual_Click;
        // 
        // MnI_FecharTodasAbas
        // 
        MnI_FecharTodasAbas.Name = "MnI_FecharTodasAbas";
        MnI_FecharTodasAbas.Size = new Size(468, 50);
        MnI_FecharTodasAbas.Text = "Fechar Todas Abas";
        MnI_FecharTodasAbas.Click += MnI_FecharTodasAbas_Click;
        // 
        // MnI_FecharAbasAEsquerda
        // 
        MnI_FecharAbasAEsquerda.Name = "MnI_FecharAbasAEsquerda";
        MnI_FecharAbasAEsquerda.Size = new Size(468, 50);
        MnI_FecharAbasAEsquerda.Text = "Fechar Abas à Esquerda";
        MnI_FecharAbasAEsquerda.Click += MnI_FecharAbasAEsquerda_Click;
        // 
        // MnI_FecharAbasADireita
        // 
        MnI_FecharAbasADireita.Name = "MnI_FecharAbasADireita";
        MnI_FecharAbasADireita.Size = new Size(468, 50);
        MnI_FecharAbasADireita.Text = "Fechar Abas à Direita";
        MnI_FecharAbasADireita.Click += MnI_FecharAbasADireita_Click;
        // 
        // MnI_ProcurarAnimePorNome
        // 
        MnI_ProcurarAnimePorNome.Image = Properties.Resources.pngwing_com;
        MnI_ProcurarAnimePorNome.Name = "MnI_ProcurarAnimePorNome";
        MnI_ProcurarAnimePorNome.Size = new Size(307, 45);
        MnI_ProcurarAnimePorNome.Text = "ProcurarPorNome";
        MnI_ProcurarAnimePorNome.Click += MnI_ProcurarAnimePorNome_Click;
        // 
        // MnI_ProcurarAnimePorID
        // 
        MnI_ProcurarAnimePorID.Image = Properties.Resources.RosaDosVentos;
        MnI_ProcurarAnimePorID.Name = "MnI_ProcurarAnimePorID";
        MnI_ProcurarAnimePorID.Size = new Size(254, 45);
        MnI_ProcurarAnimePorID.Text = "ProcurarPorID";
        MnI_ProcurarAnimePorID.Click += MnI_ProcurarAnimePorID_Click;
        // 
        // Tbc_MyAnimes
        // 
        Tbc_MyAnimes.Dock = DockStyle.Fill;
        Tbc_MyAnimes.ImageList = Iml_ImagensList;
        Tbc_MyAnimes.Location = new Point(0, 53);
        Tbc_MyAnimes.Margin = new Padding(4, 3, 4, 3);
        Tbc_MyAnimes.Name = "Tbc_MyAnimes";
        Tbc_MyAnimes.SelectedIndex = 0;
        Tbc_MyAnimes.Size = new Size(1278, 574);
        Tbc_MyAnimes.TabIndex = 1;
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
        AutoScaleDimensions = new SizeF(13F, 26F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1278, 627);
        Controls.Add(Tbc_MyAnimes);
        Controls.Add(Mnu_MenuMyAnimes);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MainMenuStrip = Mnu_MenuMyAnimes;
        Margin = new Padding(4, 3, 4, 3);
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
    private ToolStripMenuItem MnI_ProcurarAnimePorNome;
    private ToolStripMenuItem MnI_ProcurarAnimePorID;
    private TabControl Tbc_MyAnimes;
    private ImageList Iml_ImagensList;
    private ToolStripMenuItem AbasToolStripMenuItem;
    private ToolStripMenuItem MnI_AbrirAbas;
    private ToolStripMenuItem MnI_AbaMascaras;
    private ToolStripMenuItem MnI_FecharAbas;
    private ToolStripMenuItem MnI_FecharTodasAbas;
    private ToolStripMenuItem MnI_FecharAbaAtual;
    private ToolStripMenuItem MnI_FecharAbasAEsquerda;
    private ToolStripMenuItem MnI_FecharAbasADireita;
    private ToolStripMenuItem MnI_FormMsgBox;
}
