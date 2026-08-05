namespace WinAppDtudo.Forms;

partial class Frm_MyMusicX
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_MyMusicX));
        Mnu_MenuMyMusiX = new MenuStrip();
        arquivoToolStripMenuItem = new ToolStripMenuItem();
        abrirToolStripMenuItem = new ToolStripMenuItem();
        sairToolStripMenuItem = new ToolStripMenuItem();
        mDIWindowsToolStripMenuItem = new ToolStripMenuItem();
        HorizontalToolStripMenuItem = new ToolStripMenuItem();
        VerticalToolStripMenuItem = new ToolStripMenuItem();
        CascataToolStripMenuItem = new ToolStripMenuItem();
        Mnu_MenuMyMusiX.SuspendLayout();
        SuspendLayout();
        // 
        // Mnu_MenuMyMusiX
        // 
        Mnu_MenuMyMusiX.BackColor = Color.DimGray;
        Mnu_MenuMyMusiX.ImageScalingSize = new Size(32, 32);
        Mnu_MenuMyMusiX.Items.AddRange(new ToolStripItem[] { arquivoToolStripMenuItem, mDIWindowsToolStripMenuItem });
        Mnu_MenuMyMusiX.Location = new Point(0, 0);
        Mnu_MenuMyMusiX.Name = "Mnu_MenuMyMusiX";
        Mnu_MenuMyMusiX.Padding = new Padding(8, 2, 0, 2);
        Mnu_MenuMyMusiX.Size = new Size(1374, 49);
        Mnu_MenuMyMusiX.TabIndex = 0;
        Mnu_MenuMyMusiX.Text = "menuMyMusicX";
        // 
        // arquivoToolStripMenuItem
        // 
        arquivoToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { abrirToolStripMenuItem, sairToolStripMenuItem });
        arquivoToolStripMenuItem.Image = Properties.Resources.MaskV;
        arquivoToolStripMenuItem.Name = "arquivoToolStripMenuItem";
        arquivoToolStripMenuItem.Size = new Size(173, 45);
        arquivoToolStripMenuItem.Text = "Arquivo";
        // 
        // abrirToolStripMenuItem
        // 
        abrirToolStripMenuItem.BackColor = Color.Transparent;
        abrirToolStripMenuItem.ForeColor = Color.Gold;
        abrirToolStripMenuItem.Image = Properties.Resources.YingYang_HD;
        abrirToolStripMenuItem.Name = "abrirToolStripMenuItem";
        abrirToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.A;
        abrirToolStripMenuItem.Size = new Size(313, 50);
        abrirToolStripMenuItem.Text = "&Abrir";
        // 
        // sairToolStripMenuItem
        // 
        sairToolStripMenuItem.BackColor = Color.Transparent;
        sairToolStripMenuItem.ForeColor = Color.Gold;
        sairToolStripMenuItem.Image = Properties.Resources.SlaveMoney;
        sairToolStripMenuItem.Name = "sairToolStripMenuItem";
        sairToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.S;
        sairToolStripMenuItem.Size = new Size(313, 50);
        sairToolStripMenuItem.Text = "&Sair";
        // 
        // mDIWindowsToolStripMenuItem
        // 
        mDIWindowsToolStripMenuItem.BackColor = Color.Transparent;
        mDIWindowsToolStripMenuItem.BackgroundImageLayout = ImageLayout.Stretch;
        mDIWindowsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { HorizontalToolStripMenuItem, VerticalToolStripMenuItem, CascataToolStripMenuItem });
        mDIWindowsToolStripMenuItem.Name = "mDIWindowsToolStripMenuItem";
        mDIWindowsToolStripMenuItem.Size = new Size(225, 45);
        mDIWindowsToolStripMenuItem.Text = "MDI Windows";
        // 
        // HorizontalToolStripMenuItem
        // 
        HorizontalToolStripMenuItem.Name = "HorizontalToolStripMenuItem";
        HorizontalToolStripMenuItem.Size = new Size(365, 50);
        HorizontalToolStripMenuItem.Text = "Horizontal Wins";
        HorizontalToolStripMenuItem.Click += HorizontalToolStripMenuItem_Click;
        // 
        // VerticalToolStripMenuItem
        // 
        VerticalToolStripMenuItem.Name = "VerticalToolStripMenuItem";
        VerticalToolStripMenuItem.Size = new Size(365, 50);
        VerticalToolStripMenuItem.Text = "Vertical Wins";
        VerticalToolStripMenuItem.Click += VerticalToolStripMenuItem_Click;
        // 
        // CascataToolStripMenuItem
        // 
        CascataToolStripMenuItem.Name = "CascataToolStripMenuItem";
        CascataToolStripMenuItem.Size = new Size(365, 50);
        CascataToolStripMenuItem.Text = "Cascata Wins";
        CascataToolStripMenuItem.Click += CascataToolStripMenuItem_Click;
        // 
        // Frm_MyMusicX
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = SystemColors.Desktop;
        BackgroundImage = Properties.Resources.violaoEmChamas;
        BackgroundImageLayout = ImageLayout.Stretch;
        ClientSize = new Size(1374, 829);
        Controls.Add(Mnu_MenuMyMusiX);
        ForeColor = Color.Gold;
        FormBorderStyle = FormBorderStyle.None;
        Icon = (Icon)resources.GetObject("$this.Icon");
        IsMdiContainer = true;
        MainMenuStrip = Mnu_MenuMyMusiX;
        Margin = new Padding(4, 3, 4, 3);
        Name = "Frm_MyMusicX";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MyMusicX - MDI";
        Mnu_MenuMyMusiX.ResumeLayout(false);
        Mnu_MenuMyMusiX.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip Mnu_MenuMyMusiX;
    private ToolStripMenuItem mDIWindowsToolStripMenuItem;
    private ToolStripMenuItem HorizontalToolStripMenuItem;
    private ToolStripMenuItem VerticalToolStripMenuItem;
    private ToolStripMenuItem CascataToolStripMenuItem;
    private ToolStripMenuItem arquivoToolStripMenuItem;
    private ToolStripMenuItem abrirToolStripMenuItem;
    private ToolStripMenuItem sairToolStripMenuItem;
}
