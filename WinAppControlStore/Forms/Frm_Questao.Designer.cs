namespace WinAppControlStore.Forms
{
    partial class Frm_Questao
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_Questao));
            Btn_Continue = new Button();
            Btn_Pare = new Button();
            Lbl_TextoDaCaixa = new Label();
            Pic_PictureBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)Pic_PictureBox).BeginInit();
            SuspendLayout();
            // 
            // Btn_Continue
            // 
            Btn_Continue.Location = new Point(364, 122);
            Btn_Continue.Name = "Btn_Continue";
            Btn_Continue.Size = new Size(195, 58);
            Btn_Continue.TabIndex = 0;
            Btn_Continue.Text = "Continue";
            Btn_Continue.UseVisualStyleBackColor = true;
            Btn_Continue.Click += Btn_Continue_Click;
            // 
            // Btn_Pare
            // 
            Btn_Pare.Location = new Point(364, 235);
            Btn_Pare.Name = "Btn_Pare";
            Btn_Pare.Size = new Size(195, 58);
            Btn_Pare.TabIndex = 1;
            Btn_Pare.Text = "Pare";
            Btn_Pare.UseVisualStyleBackColor = true;
            Btn_Pare.Click += Btn_Pare_Click;
            // 
            // Lbl_TextoDaCaixa
            // 
            Lbl_TextoDaCaixa.AutoSize = true;
            Lbl_TextoDaCaixa.Font = new Font("Segoe UI Black", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Lbl_TextoDaCaixa.Location = new Point(200, 31);
            Lbl_TextoDaCaixa.Name = "Lbl_TextoDaCaixa";
            Lbl_TextoDaCaixa.Size = new Size(263, 32);
            Lbl_TextoDaCaixa.TabIndex = 2;
            Lbl_TextoDaCaixa.Text = "Questão a perguntar?";
            // 
            // Pic_PictureBox
            // 
            Pic_PictureBox.Image = Properties.Resources.InterrogacaoBrasil;
            Pic_PictureBox.Location = new Point(86, 122);
            Pic_PictureBox.Name = "Pic_PictureBox";
            Pic_PictureBox.Size = new Size(166, 166);
            Pic_PictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            Pic_PictureBox.TabIndex = 3;
            Pic_PictureBox.TabStop = false;
            // 
            // Frm_Questao
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(626, 360);
            Controls.Add(Pic_PictureBox);
            Controls.Add(Lbl_TextoDaCaixa);
            Controls.Add(Btn_Pare);
            Controls.Add(Btn_Continue);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Frm_Questao";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Questão?";
            ((System.ComponentModel.ISupportInitialize)Pic_PictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button Btn_Continue;
        private Button Btn_Pare;
        private Label Lbl_TextoDaCaixa;
        private PictureBox Pic_PictureBox;
    }
}