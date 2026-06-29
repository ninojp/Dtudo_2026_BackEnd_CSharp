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
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { AbasToolStripMenuItem, MnI_ProcurarAnimePorNome, MnI_ProcurarAnimePorID });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(800, 33);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // AbasToolStripMenuItem
            // 
            AbasToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { MnI_AbrirAbas, MnI_FecharAbas });
            AbasToolStripMenuItem.Name = "AbasToolStripMenuItem";
            AbasToolStripMenuItem.Size = new Size(64, 29);
            AbasToolStripMenuItem.Text = "Abas";
            // 
            // MnI_AbrirAbas
            // 
            MnI_AbrirAbas.DropDownItems.AddRange(new ToolStripItem[] { MnI_AbaMascaras, MnI_FormMsgBox });
            MnI_AbrirAbas.Image = Properties.Resources.MaskVendettaReal;
            MnI_AbrirAbas.Name = "MnI_AbrirAbas";
            MnI_AbrirAbas.Size = new Size(180, 30);
            MnI_AbrirAbas.Text = "Abrir Abas";
            // 
            // MnI_AbaMascaras
            // 
            MnI_AbaMascaras.Image = Properties.Resources.MaskV;
            MnI_AbaMascaras.Name = "MnI_AbaMascaras";
            MnI_AbaMascaras.Size = new Size(190, 30);
            MnI_AbaMascaras.Text = "AbaMascaras";
            MnI_AbaMascaras.Click += MnI_AbaMascaras_Click;
            // 
            // MnI_FormMsgBox
            // 
            MnI_FormMsgBox.Image = Properties.Resources.InterrogacaoBrasil;
            MnI_FormMsgBox.Name = "MnI_FormMsgBox";
            MnI_FormMsgBox.Size = new Size(190, 30);
            MnI_FormMsgBox.Text = "FormMsgBox";
            MnI_FormMsgBox.Click += MnI_FormMsgBox_Click;
            // 
            // MnI_FecharAbas
            // 
            MnI_FecharAbas.DropDownItems.AddRange(new ToolStripItem[] { MnI_FecharAbaAtual, MnI_FecharTodasAbas, MnI_FecharAbasAEsquerda, MnI_FecharAbasADireita });
            MnI_FecharAbas.Image = Properties.Resources.SlaveMoney;
            MnI_FecharAbas.Name = "MnI_FecharAbas";
            MnI_FecharAbas.Size = new Size(180, 30);
            MnI_FecharAbas.Text = "Fechar Abas";
            // 
            // MnI_FecharAbaAtual
            // 
            MnI_FecharAbaAtual.Name = "MnI_FecharAbaAtual";
            MnI_FecharAbaAtual.Size = new Size(272, 30);
            MnI_FecharAbaAtual.Text = "Fechar Aba Atual";
            MnI_FecharAbaAtual.Click += MnI_FecharAbaAtual_Click;
            // 
            // MnI_FecharTodasAbas
            // 
            MnI_FecharTodasAbas.Name = "MnI_FecharTodasAbas";
            MnI_FecharTodasAbas.Size = new Size(272, 30);
            MnI_FecharTodasAbas.Text = "Fechar Todas Abas";
            MnI_FecharTodasAbas.Click += MnI_FecharTodasAbas_Click;
            // 
            // MnI_FecharAbasAEsquerda
            // 
            MnI_FecharAbasAEsquerda.Name = "MnI_FecharAbasAEsquerda";
            MnI_FecharAbasAEsquerda.Size = new Size(272, 30);
            MnI_FecharAbasAEsquerda.Text = "Fechar Abas à Esquerda";
            // 
            // MnI_FecharAbasADireita
            // 
            MnI_FecharAbasADireita.Name = "MnI_FecharAbasADireita";
            MnI_FecharAbasADireita.Size = new Size(272, 30);
            MnI_FecharAbasADireita.Text = "Fechar Abas à Direita";
            // 
            // MnI_ProcurarAnimePorNome
            // 
            MnI_ProcurarAnimePorNome.Name = "MnI_ProcurarAnimePorNome";
            MnI_ProcurarAnimePorNome.Size = new Size(165, 29);
            MnI_ProcurarAnimePorNome.Text = "ProcurarPorNome";
            MnI_ProcurarAnimePorNome.Click += MnI_ProcurarAnimePorNome_Click;
            // 
            // MnI_ProcurarAnimePorID
            // 
            MnI_ProcurarAnimePorID.Name = "MnI_ProcurarAnimePorID";
            MnI_ProcurarAnimePorID.Size = new Size(134, 29);
            MnI_ProcurarAnimePorID.Text = "ProcurarPorID";
            MnI_ProcurarAnimePorID.Click += MnI_ProcurarAnimePorID_Click;
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
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MyAnimes - Abas";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
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
}
