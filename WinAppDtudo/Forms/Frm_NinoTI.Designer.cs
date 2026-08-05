namespace WinAppDtudo.Forms
{
    partial class Frm_NinoTI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_NinoTI));
            Mnu_MenuNinoTI = new MenuStrip();
            abrirToolStripMenuItem = new ToolStripMenuItem();
            Mnu_MenuNinoTI.SuspendLayout();
            SuspendLayout();
            // 
            // Mnu_MenuNinoTI
            // 
            Mnu_MenuNinoTI.BackColor = Color.DimGray;
            Mnu_MenuNinoTI.ImageScalingSize = new Size(32, 32);
            Mnu_MenuNinoTI.Items.AddRange(new ToolStripItem[] { abrirToolStripMenuItem });
            Mnu_MenuNinoTI.Location = new Point(0, 0);
            Mnu_MenuNinoTI.Name = "Mnu_MenuNinoTI";
            Mnu_MenuNinoTI.Size = new Size(1374, 49);
            Mnu_MenuNinoTI.TabIndex = 0;
            Mnu_MenuNinoTI.Text = "menuNinoTI";
            // 
            // abrirToolStripMenuItem
            // 
            abrirToolStripMenuItem.Name = "abrirToolStripMenuItem";
            abrirToolStripMenuItem.Size = new Size(102, 45);
            abrirToolStripMenuItem.Text = "Abrir";
            // 
            // Frm_NinoTI
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = SystemColors.ControlDarkDark;
            BackgroundImage = Properties.Resources.OlhoBRHacker1024;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1374, 829);
            Controls.Add(Mnu_MenuNinoTI);
            ForeColor = Color.Gold;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = Mnu_MenuNinoTI;
            Name = "Frm_NinoTI";
            Text = "Nino T.I";
            Mnu_MenuNinoTI.ResumeLayout(false);
            Mnu_MenuNinoTI.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip Mnu_MenuNinoTI;
        private ToolStripMenuItem abrirToolStripMenuItem;
    }
}
