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
            AbasToolStripMenuItem = new ToolStripMenuItem();
            AbrirAbaMascarasToolStripMenuItem = new ToolStripMenuItem();
            abaMascarasToolStripMenuItem = new ToolStripMenuItem();
            FecharAbasToolStripMenuItem = new ToolStripMenuItem();
            FecharAbaAtualToolStripMenuItem = new ToolStripMenuItem();
            FecharTodasAbasToolStripMenuItem = new ToolStripMenuItem();
            FecharAbasÀEsquerdaToolStripMenuItem = new ToolStripMenuItem();
            FecharAbasÀDireitaToolStripMenuItem = new ToolStripMenuItem();
            ProcurarAnimeToolStripMenuItem = new ToolStripMenuItem();
            ProcurarAnimePorMalidToolStripMenuItem = new ToolStripMenuItem();
            TesteCursoToolStripMenuItem = new ToolStripMenuItem();
            Msb_MsgBoxToolStripMenuItem = new ToolStripMenuItem();
            Tbc_MyAnimes = new TabControl();
            Iml_ImagensList = new ImageList(components);
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { AbasToolStripMenuItem, ProcurarAnimeToolStripMenuItem, ProcurarAnimePorMalidToolStripMenuItem, TesteCursoToolStripMenuItem, Msb_MsgBoxToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 33);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // AbasToolStripMenuItem
            // 
            AbasToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { AbrirAbaMascarasToolStripMenuItem, FecharAbasToolStripMenuItem });
            AbasToolStripMenuItem.Name = "AbasToolStripMenuItem";
            AbasToolStripMenuItem.Size = new Size(64, 29);
            AbasToolStripMenuItem.Text = "Abas";
            // 
            // AbrirAbaMascarasToolStripMenuItem
            // 
            AbrirAbaMascarasToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { abaMascarasToolStripMenuItem });
            AbrirAbaMascarasToolStripMenuItem.Image = Properties.Resources.MaskVendettaReal;
            AbrirAbaMascarasToolStripMenuItem.Name = "AbrirAbaMascarasToolStripMenuItem";
            AbrirAbaMascarasToolStripMenuItem.Size = new Size(180, 30);
            AbrirAbaMascarasToolStripMenuItem.Text = "Abrir Abas";
            // 
            // abaMascarasToolStripMenuItem
            // 
            abaMascarasToolStripMenuItem.Image = Properties.Resources.MaskV;
            abaMascarasToolStripMenuItem.Name = "abaMascarasToolStripMenuItem";
            abaMascarasToolStripMenuItem.Size = new Size(189, 30);
            abaMascarasToolStripMenuItem.Text = "AbaMascaras";
            abaMascarasToolStripMenuItem.Click += AbaMascarasToolStripMenuItem_Click;
            // 
            // FecharAbasToolStripMenuItem
            // 
            FecharAbasToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { FecharAbaAtualToolStripMenuItem, FecharTodasAbasToolStripMenuItem, FecharAbasÀEsquerdaToolStripMenuItem, FecharAbasÀDireitaToolStripMenuItem });
            FecharAbasToolStripMenuItem.Image = Properties.Resources.SlaveMoney;
            FecharAbasToolStripMenuItem.Name = "FecharAbasToolStripMenuItem";
            FecharAbasToolStripMenuItem.Size = new Size(180, 30);
            FecharAbasToolStripMenuItem.Text = "Fechar Abas";
            // 
            // FecharAbaAtualToolStripMenuItem
            // 
            FecharAbaAtualToolStripMenuItem.Name = "FecharAbaAtualToolStripMenuItem";
            FecharAbaAtualToolStripMenuItem.Size = new Size(272, 30);
            FecharAbaAtualToolStripMenuItem.Text = "Fechar Aba Atual";
            FecharAbaAtualToolStripMenuItem.Click += FecharAbaAtualToolStripMenuItem_Click;
            // 
            // FecharTodasAbasToolStripMenuItem
            // 
            FecharTodasAbasToolStripMenuItem.Name = "FecharTodasAbasToolStripMenuItem";
            FecharTodasAbasToolStripMenuItem.Size = new Size(272, 30);
            FecharTodasAbasToolStripMenuItem.Text = "Fechar Todas Abas";
            // 
            // FecharAbasÀEsquerdaToolStripMenuItem
            // 
            FecharAbasÀEsquerdaToolStripMenuItem.Name = "FecharAbasÀEsquerdaToolStripMenuItem";
            FecharAbasÀEsquerdaToolStripMenuItem.Size = new Size(272, 30);
            FecharAbasÀEsquerdaToolStripMenuItem.Text = "Fechar Abas à Esquerda";
            // 
            // FecharAbasÀDireitaToolStripMenuItem
            // 
            FecharAbasÀDireitaToolStripMenuItem.Name = "FecharAbasÀDireitaToolStripMenuItem";
            FecharAbasÀDireitaToolStripMenuItem.Size = new Size(272, 30);
            FecharAbasÀDireitaToolStripMenuItem.Text = "Fechar Abas à Direita";
            // 
            // ProcurarAnimeToolStripMenuItem
            // 
            ProcurarAnimeToolStripMenuItem.Name = "ProcurarAnimeToolStripMenuItem";
            ProcurarAnimeToolStripMenuItem.Size = new Size(165, 29);
            ProcurarAnimeToolStripMenuItem.Text = "ProcurarPorNome";
            ProcurarAnimeToolStripMenuItem.Click += ProcurarAnimeToolStripMenuItem_Click;
            // 
            // ProcurarAnimePorMalidToolStripMenuItem
            // 
            ProcurarAnimePorMalidToolStripMenuItem.Name = "ProcurarAnimePorMalidToolStripMenuItem";
            ProcurarAnimePorMalidToolStripMenuItem.Size = new Size(134, 29);
            ProcurarAnimePorMalidToolStripMenuItem.Text = "ProcurarPorID";
            ProcurarAnimePorMalidToolStripMenuItem.Click += ProcurarAnimePorMalidToolStripMenuItem_Click;
            // 
            // TesteCursoToolStripMenuItem
            // 
            TesteCursoToolStripMenuItem.Name = "TesteCursoToolStripMenuItem";
            TesteCursoToolStripMenuItem.Size = new Size(109, 29);
            TesteCursoToolStripMenuItem.Text = "TesteCurso";
            TesteCursoToolStripMenuItem.Click += AbaMascarasToolStripMenuItem_Click;
            // 
            // Msb_MsgBoxToolStripMenuItem
            // 
            Msb_MsgBoxToolStripMenuItem.Name = "Msb_MsgBoxToolStripMenuItem";
            Msb_MsgBoxToolStripMenuItem.Size = new Size(88, 29);
            Msb_MsgBoxToolStripMenuItem.Text = "MsgBox";
            Msb_MsgBoxToolStripMenuItem.Click += Msb_MsgBoxToolStripMenuItem_Click;
            // 
            // Tbc_MyAnimes
            // 
            Tbc_MyAnimes.Dock = DockStyle.Fill;
            Tbc_MyAnimes.ImageList = Iml_ImagensList;
            Tbc_MyAnimes.Location = new Point(0, 33);
            Tbc_MyAnimes.Name = "Tbc_MyAnimes";
            Tbc_MyAnimes.SelectedIndex = 0;
            Tbc_MyAnimes.Size = new Size(800, 417);
            Tbc_MyAnimes.TabIndex = 1;
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
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(Tbc_MyAnimes);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "Frm_MyAnimes";
            StartPosition = FormStartPosition.CenterParent;
            Text = "MyAnimes - Abas";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem ProcurarAnimeToolStripMenuItem;
        private ToolStripMenuItem ProcurarAnimePorMalidToolStripMenuItem;
        private TabControl Tbc_MyAnimes;
        private ImageList Iml_ImagensList;
        private ToolStripMenuItem TesteCursoToolStripMenuItem;
        private ToolStripMenuItem AbasToolStripMenuItem;
        private ToolStripMenuItem AbrirAbaMascarasToolStripMenuItem;
        private ToolStripMenuItem abaMascarasToolStripMenuItem;
        private ToolStripMenuItem FecharAbasToolStripMenuItem;
        private ToolStripMenuItem FecharTodasAbasToolStripMenuItem;
        private ToolStripMenuItem FecharAbaAtualToolStripMenuItem;
        private ToolStripMenuItem FecharAbasÀEsquerdaToolStripMenuItem;
        private ToolStripMenuItem FecharAbasÀDireitaToolStripMenuItem;
        private ToolStripMenuItem Msb_MsgBoxToolStripMenuItem;
    }
}
