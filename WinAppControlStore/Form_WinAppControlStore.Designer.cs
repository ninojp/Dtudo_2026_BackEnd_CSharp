namespace WinAppControlStore
{
    partial class Form_WinAppControlStore
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_WinAppControlStore));
            Lbl_Titulo = new Label();
            Btn_Site_Dtudo = new Button();
            Btn_Sair_App = new Button();
            Btn_Modifica_Label = new Button();
            Txb_Texto_Temp = new TextBox();
            Btn_Abrir_Form = new Button();
            Lbl_Modifica_Titulo = new Label();
            SuspendLayout();
            // 
            // Lbl_Titulo
            // 
            Lbl_Titulo.AutoSize = true;
            Lbl_Titulo.Font = new Font("Segoe UI Black", 15.9000006F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            Lbl_Titulo.Location = new Point(163, 9);
            Lbl_Titulo.Name = "Lbl_Titulo";
            Lbl_Titulo.Size = new Size(491, 30);
            Lbl_Titulo.TabIndex = 0;
            Lbl_Titulo.Text = "Aplicativo para controle de Arquivos Locais.";
            // 
            // Btn_Site_Dtudo
            // 
            Btn_Site_Dtudo.BackColor = SystemColors.ActiveCaption;
            Btn_Site_Dtudo.Location = new Point(29, 85);
            Btn_Site_Dtudo.Name = "Btn_Site_Dtudo";
            Btn_Site_Dtudo.Size = new Size(164, 23);
            Btn_Site_Dtudo.TabIndex = 1;
            Btn_Site_Dtudo.Text = "Abrir (FrontEnd) Site Dtudo";
            Btn_Site_Dtudo.UseVisualStyleBackColor = false;
            Btn_Site_Dtudo.Click += Btn_Site_Dtudo_Click;
            // 
            // Btn_Sair_App
            // 
            Btn_Sair_App.BackColor = SystemColors.ActiveCaption;
            Btn_Sair_App.Location = new Point(668, 391);
            Btn_Sair_App.Name = "Btn_Sair_App";
            Btn_Sair_App.Size = new Size(87, 23);
            Btn_Sair_App.TabIndex = 2;
            Btn_Sair_App.Text = "Sair do App";
            Btn_Sair_App.UseVisualStyleBackColor = false;
            Btn_Sair_App.Click += Btn_Sair_App_Click;
            // 
            // Btn_Modifica_Label
            // 
            Btn_Modifica_Label.BackColor = SystemColors.ActiveCaption;
            Btn_Modifica_Label.Location = new Point(568, 114);
            Btn_Modifica_Label.Name = "Btn_Modifica_Label";
            Btn_Modifica_Label.Size = new Size(187, 23);
            Btn_Modifica_Label.TabIndex = 3;
            Btn_Modifica_Label.Text = "Modifica o texto (Titulo) ";
            Btn_Modifica_Label.UseVisualStyleBackColor = false;
            Btn_Modifica_Label.Click += Btn_Modifica_Label_Click;
            // 
            // Txb_Texto_Temp
            // 
            Txb_Texto_Temp.BackColor = SystemColors.ActiveCaption;
            Txb_Texto_Temp.Location = new Point(567, 85);
            Txb_Texto_Temp.Name = "Txb_Texto_Temp";
            Txb_Texto_Temp.Size = new Size(188, 23);
            Txb_Texto_Temp.TabIndex = 4;
            // 
            // Btn_Abrir_Form
            // 
            Btn_Abrir_Form.BackColor = SystemColors.ActiveCaption;
            Btn_Abrir_Form.Location = new Point(29, 391);
            Btn_Abrir_Form.Name = "Btn_Abrir_Form";
            Btn_Abrir_Form.Size = new Size(110, 23);
            Btn_Abrir_Form.TabIndex = 5;
            Btn_Abrir_Form.Text = "Abrir Formulário";
            Btn_Abrir_Form.UseVisualStyleBackColor = false;
            // 
            // Lbl_Modifica_Titulo
            // 
            Lbl_Modifica_Titulo.AutoSize = true;
            Lbl_Modifica_Titulo.Location = new Point(600, 67);
            Lbl_Modifica_Titulo.Name = "Lbl_Modifica_Titulo";
            Lbl_Modifica_Titulo.Size = new Size(114, 15);
            Lbl_Modifica_Titulo.TabIndex = 6;
            Lbl_Modifica_Titulo.Text = "Digite o Novo Título";
            // 
            // Form_WinAppControlStore
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.WindowFrame;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Center;
            ClientSize = new Size(800, 450);
            Controls.Add(Lbl_Modifica_Titulo);
            Controls.Add(Btn_Abrir_Form);
            Controls.Add(Txb_Texto_Temp);
            Controls.Add(Btn_Modifica_Label);
            Controls.Add(Btn_Sair_App);
            Controls.Add(Btn_Site_Dtudo);
            Controls.Add(Lbl_Titulo);
            DoubleBuffered = true;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form_WinAppControlStore";
            Text = "Windows App Controle Armazenamento";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label Lbl_Titulo;
        private Button Btn_Site_Dtudo;
        private Button Btn_Sair_App;
        private Button Btn_Modifica_Label;
        private TextBox Txb_Texto_Temp;
        private Button Btn_Abrir_Form;
        private Label Lbl_Modifica_Titulo;
    }
}
