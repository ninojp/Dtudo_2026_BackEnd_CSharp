namespace WinAppControlStore.Forms
{
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
            outroFormToolStripMenuItem = new ToolStripMenuItem();
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
            menuStrip1.Items.AddRange(new ToolStripItem[] { arquivoToolStripMenuItem, mDIWindowsToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 33);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // arquivoToolStripMenuItem
            // 
            arquivoToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { abrirToolStripMenuItem, sairToolStripMenuItem });
            arquivoToolStripMenuItem.Image = Properties.Resources.MaskV;
            arquivoToolStripMenuItem.Name = "arquivoToolStripMenuItem";
            arquivoToolStripMenuItem.Size = new Size(103, 29);
            arquivoToolStripMenuItem.Text = "Arquivo";
            // 
            // abrirToolStripMenuItem
            // 
            abrirToolStripMenuItem.BackColor = Color.Transparent;
            abrirToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { formTestToolStripMenuItem, formHelloWorldToolStripMenuItem, outroFormToolStripMenuItem });
            abrirToolStripMenuItem.ForeColor = Color.Gold;
            abrirToolStripMenuItem.Image = Properties.Resources.YingYang_HD;
            abrirToolStripMenuItem.Name = "abrirToolStripMenuItem";
            abrirToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.A;
            abrirToolStripMenuItem.Size = new Size(181, 30);
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
            formTestToolStripMenuItem.Size = new Size(274, 30);
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
            formHelloWorldToolStripMenuItem.Size = new Size(274, 30);
            formHelloWorldToolStripMenuItem.Text = "Form&HelloWorld";
            formHelloWorldToolStripMenuItem.Click += FormHelloWorldToolStripMenuItem_Click;
            // 
            // outroFormToolStripMenuItem
            // 
            outroFormToolStripMenuItem.Name = "outroFormToolStripMenuItem";
            outroFormToolStripMenuItem.Size = new Size(274, 30);
            outroFormToolStripMenuItem.Text = "OutroForm...";
            // 
            // sairToolStripMenuItem
            // 
            sairToolStripMenuItem.BackColor = Color.Transparent;
            sairToolStripMenuItem.ForeColor = Color.Gold;
            sairToolStripMenuItem.Image = Properties.Resources.SlaveMoney;
            sairToolStripMenuItem.Name = "sairToolStripMenuItem";
            sairToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.S;
            sairToolStripMenuItem.Size = new Size(181, 30);
            sairToolStripMenuItem.Text = "&Sair";
            // 
            // mDIWindowsToolStripMenuItem
            // 
            mDIWindowsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { HorizontalToolStripMenuItem, VerticalToolStripMenuItem, CascataToolStripMenuItem });
            mDIWindowsToolStripMenuItem.Name = "mDIWindowsToolStripMenuItem";
            mDIWindowsToolStripMenuItem.Size = new Size(137, 29);
            mDIWindowsToolStripMenuItem.Text = "MDI Windows";
            // 
            // HorizontalToolStripMenuItem
            // 
            HorizontalToolStripMenuItem.Name = "HorizontalToolStripMenuItem";
            HorizontalToolStripMenuItem.Size = new Size(210, 30);
            HorizontalToolStripMenuItem.Text = "Horizontal Wins";
            HorizontalToolStripMenuItem.Click += HorizontalToolStripMenuItem_Click;
            // 
            // VerticalToolStripMenuItem
            // 
            VerticalToolStripMenuItem.Name = "VerticalToolStripMenuItem";
            VerticalToolStripMenuItem.Size = new Size(210, 30);
            VerticalToolStripMenuItem.Text = "Vertical Wins";
            VerticalToolStripMenuItem.Click += VerticalToolStripMenuItem_Click;
            // 
            // CascataToolStripMenuItem
            // 
            CascataToolStripMenuItem.Name = "CascataToolStripMenuItem";
            CascataToolStripMenuItem.Size = new Size(210, 30);
            CascataToolStripMenuItem.Text = "Cascata Wins";
            CascataToolStripMenuItem.Click += CascataToolStripMenuItem_Click;
            // 
            // Frm_MyMusicX
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            IsMdiContainer = true;
            MainMenuStrip = menuStrip1;
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
        private ToolStripMenuItem outroFormToolStripMenuItem;
        private ToolStripMenuItem sairToolStripMenuItem;
    }
}
