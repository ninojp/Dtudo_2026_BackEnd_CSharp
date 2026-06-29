namespace WinAppControlStore
{
    partial class Frm_FormTest
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_FormTest));
            Btn_Voltar = new Button();
            SuspendLayout();
            // 
            // Btn_Voltar
            // 
            Btn_Voltar.BackColor = SystemColors.ActiveCaptionText;
            Btn_Voltar.BackgroundImage = (Image)resources.GetObject("Btn_Voltar.BackgroundImage");
            Btn_Voltar.BackgroundImageLayout = ImageLayout.Center;
            Btn_Voltar.FlatAppearance.BorderSize = 0;
            Btn_Voltar.FlatStyle = FlatStyle.Flat;
            Btn_Voltar.Font = new Font("Segoe UI", 9.900001F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Btn_Voltar.ForeColor = SystemColors.ActiveCaption;
            Btn_Voltar.Location = new Point(17, 603);
            Btn_Voltar.Margin = new Padding(4, 5, 4, 5);
            Btn_Voltar.Name = "Btn_Voltar";
            Btn_Voltar.Size = new Size(197, 127);
            Btn_Voltar.TabIndex = 0;
            Btn_Voltar.Text = "Abrir Form Principal";
            Btn_Voltar.TextAlign = ContentAlignment.TopCenter;
            Btn_Voltar.UseVisualStyleBackColor = false;
            Btn_Voltar.Click += Btn_Voltar_Click;
            // 
            // Frm_FormTest
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlText;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Center;
            ClientSize = new Size(1143, 750);
            Controls.Add(Btn_Voltar);
            DoubleBuffered = true;
            ForeColor = SystemColors.Highlight;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 5, 4, 5);
            Name = "Frm_FormTest";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Formulário de Testes";
            ResumeLayout(false);
        }

        #endregion

        private Button Btn_Voltar;
    }
}
