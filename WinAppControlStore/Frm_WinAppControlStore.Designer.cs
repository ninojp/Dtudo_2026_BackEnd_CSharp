namespace WinAppControlStore
{
    partial class Frm_WinAppControlStore
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_WinAppControlStore));
            Lbl_Titulo = new Label();
            Btn_Site_Dtudo = new Button();
            Btn_Sair_App = new Button();
            Btn_Abrir_Form = new Button();
            Mnu_Principal = new MenuStrip();
            arquivoToolStripMenuItem = new ToolStripMenuItem();
            abrirToolStripMenuItem = new ToolStripMenuItem();
            formTestToolStripMenuItem = new ToolStripMenuItem();
            formHelloWorldToolStripMenuItem = new ToolStripMenuItem();
            outroFormToolStripMenuItem = new ToolStripMenuItem();
            sairToolStripMenuItem = new ToolStripMenuItem();
            MnI_MyAnimesMenuItem = new ToolStripMenuItem();
            myMusicXToolStripMenuItem = new ToolStripMenuItem();
            ninoTIToolStripMenuItem = new ToolStripMenuItem();
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
            Lbl_Titulo.Size = new Size(378, 30);
            Lbl_Titulo.TabIndex = 0;
            Lbl_Titulo.Text = "Controle de Arquivos em DiscoHD";
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
            Mnu_Principal.Items.AddRange(new ToolStripItem[] { arquivoToolStripMenuItem, MnI_MyAnimesMenuItem, myMusicXToolStripMenuItem, ninoTIToolStripMenuItem });
            Mnu_Principal.Location = new Point(0, 0);
            Mnu_Principal.Name = "Mnu_Principal";
            Mnu_Principal.Padding = new Padding(9, 4, 0, 4);
            Mnu_Principal.Size = new Size(1142, 37);
            Mnu_Principal.TabIndex = 7;
            Mnu_Principal.Text = "MenuPrincipal";
            // 
            // arquivoToolStripMenuItem
            // 
            arquivoToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { abrirToolStripMenuItem, sairToolStripMenuItem });
            arquivoToolStripMenuItem.Image = Properties.Resources.MaskV;
            arquivoToolStripMenuItem.Name = "arquivoToolStripMenuItem";
            arquivoToolStripMenuItem.Size = new Size(108, 29);
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
            abrirToolStripMenuItem.Size = new Size(190, 30);
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
            formTestToolStripMenuItem.Size = new Size(288, 30);
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
            formHelloWorldToolStripMenuItem.Size = new Size(288, 30);
            formHelloWorldToolStripMenuItem.Text = "Form&HelloWorld";
            formHelloWorldToolStripMenuItem.Click += FormHelloWorldToolStripMenuItem_Click;
            // 
            // outroFormToolStripMenuItem
            // 
            outroFormToolStripMenuItem.Name = "outroFormToolStripMenuItem";
            outroFormToolStripMenuItem.Size = new Size(288, 30);
            outroFormToolStripMenuItem.Text = "OutroForm...";
            // 
            // sairToolStripMenuItem
            // 
            sairToolStripMenuItem.BackColor = Color.Transparent;
            sairToolStripMenuItem.ForeColor = Color.Gold;
            sairToolStripMenuItem.Image = Properties.Resources.SlaveMoney;
            sairToolStripMenuItem.Name = "sairToolStripMenuItem";
            sairToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.S;
            sairToolStripMenuItem.Size = new Size(190, 30);
            sairToolStripMenuItem.Text = "&Sair";
            sairToolStripMenuItem.Click += SairToolStripMenuItem_Click;
            // 
            // MnI_MyAnimesMenuItem
            // 
            MnI_MyAnimesMenuItem.Image = Properties.Resources.TI_link;
            MnI_MyAnimesMenuItem.Name = "MnI_MyAnimesMenuItem";
            MnI_MyAnimesMenuItem.Size = new Size(130, 29);
            MnI_MyAnimesMenuItem.Text = "MyAnimes";
            MnI_MyAnimesMenuItem.Click += MnI_MyAnimesMenuItem_Click_1;
            // 
            // myMusicXToolStripMenuItem
            // 
            myMusicXToolStripMenuItem.Image = Properties.Resources.NotaMusica;
            myMusicXToolStripMenuItem.Name = "myMusicXToolStripMenuItem";
            myMusicXToolStripMenuItem.Size = new Size(129, 29);
            myMusicXToolStripMenuItem.Text = "MyMusicX";
            myMusicXToolStripMenuItem.Click += myMusicXToolStripMenuItem_Click;
            // 
            // ninoTIToolStripMenuItem
            // 
            ninoTIToolStripMenuItem.Image = Properties.Resources.OlhoBRHacker1024;
            ninoTIToolStripMenuItem.Name = "ninoTIToolStripMenuItem";
            ninoTIToolStripMenuItem.Size = new Size(98, 29);
            ninoTIToolStripMenuItem.Text = "NinoTI";
            // 
            // Frm_WinAppControlStore
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
            Name = "Frm_WinAppControlStore";
            Text = "App Controle Armazenamento";
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
        private ToolStripMenuItem arquivoToolStripMenuItem;
        private ToolStripMenuItem abrirToolStripMenuItem;
        private ToolStripMenuItem sairToolStripMenuItem;
        private ToolStripMenuItem formTestToolStripMenuItem;
        private ToolStripMenuItem formHelloWorldToolStripMenuItem;
        private ToolStripMenuItem outroFormToolStripMenuItem;
        private ToolStripMenuItem MnI_MyAnimesMenuItem;
        private ToolStripMenuItem myMusicXToolStripMenuItem;
        private ToolStripMenuItem ninoTIToolStripMenuItem;
    }
}
