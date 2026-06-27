namespace WinAppControlStore.Forms
{
    partial class Frm_HelloWorld
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_HelloWorld));
            Lbl_Modifica_Titulo = new Label();
            Txb_Texto_Temp = new TextBox();
            Btn_Modifica_Label = new Button();
            Lbl_Titulo = new Label();
            SuspendLayout();
            // 
            // Lbl_Modifica_Titulo
            // 
            Lbl_Modifica_Titulo.AutoSize = true;
            Lbl_Modifica_Titulo.BackColor = Color.Transparent;
            Lbl_Modifica_Titulo.Font = new Font("Segoe UI", 9.900001F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Lbl_Modifica_Titulo.ForeColor = Color.Gold;
            Lbl_Modifica_Titulo.Location = new Point(267, 190);
            Lbl_Modifica_Titulo.Margin = new Padding(4, 0, 4, 0);
            Lbl_Modifica_Titulo.Name = "Lbl_Modifica_Titulo";
            Lbl_Modifica_Titulo.Size = new Size(145, 19);
            Lbl_Modifica_Titulo.TabIndex = 7;
            Lbl_Modifica_Titulo.Text = "Digite o Novo Título";
            // 
            // Txb_Texto_Temp
            // 
            Txb_Texto_Temp.BackColor = SystemColors.ActiveCaption;
            Txb_Texto_Temp.Location = new Point(205, 214);
            Txb_Texto_Temp.Margin = new Padding(4, 5, 4, 5);
            Txb_Texto_Temp.Name = "Txb_Texto_Temp";
            Txb_Texto_Temp.Size = new Size(267, 31);
            Txb_Texto_Temp.TabIndex = 8;
            // 
            // Btn_Modifica_Label
            // 
            Btn_Modifica_Label.BackColor = Color.Black;
            Btn_Modifica_Label.FlatAppearance.BorderSize = 0;
            Btn_Modifica_Label.FlatStyle = FlatStyle.Flat;
            Btn_Modifica_Label.Font = new Font("Segoe UI Semibold", 11.1F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Btn_Modifica_Label.ForeColor = Color.Gold;
            Btn_Modifica_Label.Location = new Point(205, 294);
            Btn_Modifica_Label.Margin = new Padding(4, 5, 4, 5);
            Btn_Modifica_Label.Name = "Btn_Modifica_Label";
            Btn_Modifica_Label.Size = new Size(267, 55);
            Btn_Modifica_Label.TabIndex = 9;
            Btn_Modifica_Label.Text = "Click modificar Titulo ";
            Btn_Modifica_Label.UseVisualStyleBackColor = false;
            Btn_Modifica_Label.Click += Btn_Modifica_Label_Click;
            // 
            // Lbl_Titulo
            // 
            Lbl_Titulo.AutoSize = true;
            Lbl_Titulo.BackColor = Color.Transparent;
            Lbl_Titulo.Font = new Font("Segoe UI Black", 15.9000006F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            Lbl_Titulo.ForeColor = Color.Gold;
            Lbl_Titulo.Location = new Point(217, 74);
            Lbl_Titulo.Margin = new Padding(4, 0, 4, 0);
            Lbl_Titulo.Name = "Lbl_Titulo";
            Lbl_Titulo.Size = new Size(255, 30);
            Lbl_Titulo.TabIndex = 10;
            Lbl_Titulo.Text = "Formulário de testes...";
            // 
            // Frm_HelloWorld
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Desktop;
            ClientSize = new Size(676, 420);
            Controls.Add(Lbl_Titulo);
            Controls.Add(Btn_Modifica_Label);
            Controls.Add(Txb_Texto_Temp);
            Controls.Add(Lbl_Modifica_Titulo);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            Name = "Frm_HelloWorld";
            Text = "Frm_HelloWorld";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label Lbl_Modifica_Titulo;
        private TextBox Txb_Texto_Temp;
        private Button Btn_Modifica_Label;
        private Label Lbl_Titulo;
    }
}
