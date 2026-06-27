namespace WinAppControlStore
{
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
            menuStrip1 = new MenuStrip();
            procurarAnimeToolStripMenuItem = new ToolStripMenuItem();
            procurarAnimePorMalidToolStripMenuItem = new ToolStripMenuItem();
            testeCursoToolStripMenuItem = new ToolStripMenuItem();
            Tbc_MyAnimes = new TabControl();
            imageList1 = new ImageList(components);
            abasToolStripMenuItem = new ToolStripMenuItem();
            abrirAbaMascarasToolStripMenuItem = new ToolStripMenuItem();
            abaMascarasToolStripMenuItem = new ToolStripMenuItem();
            fecharAbasToolStripMenuItem = new ToolStripMenuItem();
            fecharTodasAbasToolStripMenuItem = new ToolStripMenuItem();
            fecharAbaAtualToolStripMenuItem = new ToolStripMenuItem();
            fecharAbasÀEsquerdaToolStripMenuItem = new ToolStripMenuItem();
            fecharAbasÀDireitaToolStripMenuItem = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { abasToolStripMenuItem, testeCursoToolStripMenuItem, procurarAnimeToolStripMenuItem, procurarAnimePorMalidToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 33);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // procurarAnimeToolStripMenuItem
            // 
            procurarAnimeToolStripMenuItem.Name = "procurarAnimeToolStripMenuItem";
            procurarAnimeToolStripMenuItem.Size = new Size(165, 29);
            procurarAnimeToolStripMenuItem.Text = "ProcurarPorNome";
            // 
            // procurarAnimePorMalidToolStripMenuItem
            // 
            procurarAnimePorMalidToolStripMenuItem.Name = "procurarAnimePorMalidToolStripMenuItem";
            procurarAnimePorMalidToolStripMenuItem.Size = new Size(134, 29);
            procurarAnimePorMalidToolStripMenuItem.Text = "ProcurarPorID";
            // 
            // testeCursoToolStripMenuItem
            // 
            testeCursoToolStripMenuItem.Name = "testeCursoToolStripMenuItem";
            testeCursoToolStripMenuItem.Size = new Size(109, 29);
            testeCursoToolStripMenuItem.Text = "TesteCurso";
            testeCursoToolStripMenuItem.Click += AbaMascarasToolStripMenuItem_Click;
            // 
            // Tbc_MyAnimes
            // 
            Tbc_MyAnimes.Dock = DockStyle.Fill;
            Tbc_MyAnimes.Location = new Point(0, 33);
            Tbc_MyAnimes.Name = "Tbc_MyAnimes";
            Tbc_MyAnimes.SelectedIndex = 0;
            Tbc_MyAnimes.Size = new Size(800, 417);
            Tbc_MyAnimes.TabIndex = 1;
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
            imageList1.TransparentColor = Color.Transparent;
            imageList1.Images.SetKeyName(0, "pngwing-7.png");
            imageList1.Images.SetKeyName(1, "pngwing-1.png");
            imageList1.Images.SetKeyName(2, "pngwing-2.png");
            // 
            // abasToolStripMenuItem
            // 
            abasToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { abrirAbaMascarasToolStripMenuItem, fecharAbasToolStripMenuItem });
            abasToolStripMenuItem.Name = "abasToolStripMenuItem";
            abasToolStripMenuItem.Size = new Size(64, 29);
            abasToolStripMenuItem.Text = "Abas";
            // 
            // abrirAbaMascarasToolStripMenuItem
            // 
            abrirAbaMascarasToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { abaMascarasToolStripMenuItem });
            abrirAbaMascarasToolStripMenuItem.Image = Properties.Resources.MaskVendettaReal;
            abrirAbaMascarasToolStripMenuItem.Name = "abrirAbaMascarasToolStripMenuItem";
            abrirAbaMascarasToolStripMenuItem.Size = new Size(180, 30);
            abrirAbaMascarasToolStripMenuItem.Text = "Abrir Abas";
            // 
            // abaMascarasToolStripMenuItem
            // 
            abaMascarasToolStripMenuItem.Image = Properties.Resources.MaskV;
            abaMascarasToolStripMenuItem.Name = "abaMascarasToolStripMenuItem";
            abaMascarasToolStripMenuItem.Size = new Size(189, 30);
            abaMascarasToolStripMenuItem.Text = "AbaMascaras";
            // 
            // fecharAbasToolStripMenuItem
            // 
            fecharAbasToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fecharTodasAbasToolStripMenuItem, fecharAbaAtualToolStripMenuItem, fecharAbasÀEsquerdaToolStripMenuItem, fecharAbasÀDireitaToolStripMenuItem });
            fecharAbasToolStripMenuItem.Name = "fecharAbasToolStripMenuItem";
            fecharAbasToolStripMenuItem.Size = new Size(180, 30);
            fecharAbasToolStripMenuItem.Text = "Fechar Abas";
            // 
            // fecharTodasAbasToolStripMenuItem
            // 
            fecharTodasAbasToolStripMenuItem.Name = "fecharTodasAbasToolStripMenuItem";
            fecharTodasAbasToolStripMenuItem.Size = new Size(272, 30);
            fecharTodasAbasToolStripMenuItem.Text = "Fechar Todas Abas";
            // 
            // fecharAbaAtualToolStripMenuItem
            // 
            fecharAbaAtualToolStripMenuItem.Name = "fecharAbaAtualToolStripMenuItem";
            fecharAbaAtualToolStripMenuItem.Size = new Size(272, 30);
            fecharAbaAtualToolStripMenuItem.Text = "Fechar Aba Atual";
            // 
            // fecharAbasÀEsquerdaToolStripMenuItem
            // 
            fecharAbasÀEsquerdaToolStripMenuItem.Name = "fecharAbasÀEsquerdaToolStripMenuItem";
            fecharAbasÀEsquerdaToolStripMenuItem.Size = new Size(272, 30);
            fecharAbasÀEsquerdaToolStripMenuItem.Text = "Fechar Abas à Esquerda";
            // 
            // fecharAbasÀDireitaToolStripMenuItem
            // 
            fecharAbasÀDireitaToolStripMenuItem.Name = "fecharAbasÀDireitaToolStripMenuItem";
            fecharAbasÀDireitaToolStripMenuItem.Size = new Size(272, 30);
            fecharAbasÀDireitaToolStripMenuItem.Text = "Fechar Abas à Direita";
            // 
            // Frm_MyAnimes
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(Tbc_MyAnimes);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "Frm_MyAnimes";
            Text = "MyAnimes";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem procurarAnimeToolStripMenuItem;
        private ToolStripMenuItem procurarAnimePorMalidToolStripMenuItem;
        private TabControl Tbc_MyAnimes;
        private ImageList imageList1;
        private ToolStripMenuItem testeCursoToolStripMenuItem;
        private ToolStripMenuItem abasToolStripMenuItem;
        private ToolStripMenuItem abrirAbaMascarasToolStripMenuItem;
        private ToolStripMenuItem abaMascarasToolStripMenuItem;
        private ToolStripMenuItem fecharAbasToolStripMenuItem;
        private ToolStripMenuItem fecharTodasAbasToolStripMenuItem;
        private ToolStripMenuItem fecharAbaAtualToolStripMenuItem;
        private ToolStripMenuItem fecharAbasÀEsquerdaToolStripMenuItem;
        private ToolStripMenuItem fecharAbasÀDireitaToolStripMenuItem;
    }
}
