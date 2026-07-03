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
        menuStrip1 = new MenuStrip();
        arquivoToolStripMenuItem = new ToolStripMenuItem();
        abrirToolStripMenuItem = new ToolStripMenuItem();
        formTestToolStripMenuItem = new ToolStripMenuItem();
        formHelloWorldToolStripMenuItem = new ToolStripMenuItem();
        sairToolStripMenuItem = new ToolStripMenuItem();
        mDIWindowsToolStripMenuItem = new ToolStripMenuItem();
        HorizontalToolStripMenuItem = new ToolStripMenuItem();
        VerticalToolStripMenuItem = new ToolStripMenuItem();
        CascataToolStripMenuItem = new ToolStripMenuItem();
        menuStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // menuStrip1
        // 
        menuStrip1.ImageScalingSize = new Size(32, 32);
        menuStrip1.Items.AddRange(new ToolStripItem[] { arquivoToolStripMenuItem, mDIWindowsToolStripMenuItem });
        menuStrip1.Location = new Point(0, 0);
        menuStrip1.Name = "menuStrip1";
        menuStrip1.Padding = new Padding(8, 2, 0, 2);
        menuStrip1.Size = new Size(1374, 49);
        menuStrip1.TabIndex = 0;
        menuStrip1.Text = "menuStrip1";
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
        abrirToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { formTestToolStripMenuItem, formHelloWorldToolStripMenuItem });
        abrirToolStripMenuItem.ForeColor = Color.Gold;
        abrirToolStripMenuItem.Image = Properties.Resources.YingYang_HD;
        abrirToolStripMenuItem.Name = "abrirToolStripMenuItem";
        abrirToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.A;
        abrirToolStripMenuItem.Size = new Size(359, 50);
        abrirToolStripMenuItem.Text = "&Abrir";
        // 
        // formTestToolStripMenuItem
        // 
        formTestToolStripMenuItem.BackColor = Color.Transparent;
        formTestToolStripMenuItem.ForeColor = Color.Gold;
        formTestToolStripMenuItem.Image = Properties.Resources.TI_link;
        formTestToolStripMenuItem.Name = "formTestToolStripMenuItem";
        formTestToolStripMenuItem.ShortcutKeyDisplayString = "";
        formTestToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.T;
        formTestToolStripMenuItem.Size = new Size(469, 50);
        formTestToolStripMenuItem.Text = "Form&Test";
        formTestToolStripMenuItem.Click += FormTestToolStripMenuItem_Click;
        // 
        // formHelloWorldToolStripMenuItem
        // 
        formHelloWorldToolStripMenuItem.BackColor = Color.Transparent;
        formHelloWorldToolStripMenuItem.BackgroundImageLayout = ImageLayout.None;
        formHelloWorldToolStripMenuItem.ForeColor = Color.Gold;
        formHelloWorldToolStripMenuItem.Image = Properties.Resources.OlhoBRHacker1024;
        formHelloWorldToolStripMenuItem.Name = "formHelloWorldToolStripMenuItem";
        formHelloWorldToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.H;
        formHelloWorldToolStripMenuItem.Size = new Size(469, 50);
        formHelloWorldToolStripMenuItem.Text = "Form&HelloWorld";
        formHelloWorldToolStripMenuItem.Click += FormHelloWorldToolStripMenuItem_Click;
        // 
        // sairToolStripMenuItem
        // 
        sairToolStripMenuItem.BackColor = Color.Transparent;
        sairToolStripMenuItem.ForeColor = Color.Gold;
        sairToolStripMenuItem.Image = Properties.Resources.SlaveMoney;
        sairToolStripMenuItem.Name = "sairToolStripMenuItem";
        sairToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.S;
        sairToolStripMenuItem.Size = new Size(359, 50);
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
        AutoScaleDimensions = new SizeF(13F, 26F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Desktop;
        BackgroundImageLayout = ImageLayout.Stretch;
        ClientSize = new Size(1374, 829);
        Controls.Add(menuStrip1);
        Icon = (Icon)resources.GetObject("$this.Icon");
        IsMdiContainer = true;
        MainMenuStrip = menuStrip1;
        Margin = new Padding(4, 3, 4, 3);
        Name = "Frm_MyMusicX";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MyMusicX - MDI";
        menuStrip1.ResumeLayout(false);
        menuStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip menuStrip1;
    private ToolStripMenuItem mDIWindowsToolStripMenuItem;
    private ToolStripMenuItem HorizontalToolStripMenuItem;
    private ToolStripMenuItem VerticalToolStripMenuItem;
    private ToolStripMenuItem CascataToolStripMenuItem;
    private ToolStripMenuItem arquivoToolStripMenuItem;
    private ToolStripMenuItem abrirToolStripMenuItem;
    private ToolStripMenuItem formTestToolStripMenuItem;
    private ToolStripMenuItem formHelloWorldToolStripMenuItem;
    private ToolStripMenuItem sairToolStripMenuItem;
}
