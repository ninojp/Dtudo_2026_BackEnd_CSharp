namespace WinAppControlStore
{
    partial class Frm_DemonstracaoKey
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
            Txt_Input = new TextBox();
            Txt_Msg = new TextBox();
            Btn_Reset = new Button();
            Lbl_Minus = new Label();
            Lbl_Maius = new Label();
            Lbl_Upper = new Label();
            Lbl_Lower = new Label();
            SuspendLayout();
            // 
            // Txt_Input
            // 
            Txt_Input.BackColor = SystemColors.ActiveCaption;
            Txt_Input.Location = new Point(102, 15);
            Txt_Input.Name = "Txt_Input";
            Txt_Input.Size = new Size(150, 23);
            Txt_Input.TabIndex = 0;
            Txt_Input.KeyDown += Txt_Input_KeyDown;
            // 
            // Txt_Msg
            // 
            Txt_Msg.BackColor = SystemColors.ActiveCaption;
            Txt_Msg.Location = new Point(23, 60);
            Txt_Msg.Multiline = true;
            Txt_Msg.Name = "Txt_Msg";
            Txt_Msg.ScrollBars = ScrollBars.Vertical;
            Txt_Msg.Size = new Size(293, 189);
            Txt_Msg.TabIndex = 1;
            Txt_Msg.TabStop = false;
            // 
            // Btn_Reset
            // 
            Btn_Reset.BackColor = SystemColors.ActiveCaption;
            Btn_Reset.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Btn_Reset.ForeColor = SystemColors.ActiveCaptionText;
            Btn_Reset.Location = new Point(382, 8);
            Btn_Reset.Name = "Btn_Reset";
            Btn_Reset.Size = new Size(79, 30);
            Btn_Reset.TabIndex = 2;
            Btn_Reset.Text = "Limpar";
            Btn_Reset.UseVisualStyleBackColor = false;
            Btn_Reset.Click += Btn_Reset_Click;
            // 
            // Lbl_Minus
            // 
            Lbl_Minus.BackColor = SystemColors.ActiveCaptionText;
            Lbl_Minus.Font = new Font("Arial Black", 15.9000006F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Lbl_Minus.ForeColor = SystemColors.ActiveCaption;
            Lbl_Minus.Location = new Point(349, 187);
            Lbl_Minus.Name = "Lbl_Minus";
            Lbl_Minus.Size = new Size(150, 30);
            Lbl_Minus.TabIndex = 3;
            Lbl_Minus.Text = "Minúsculas";
            // 
            // Lbl_Maius
            // 
            Lbl_Maius.BackColor = SystemColors.ActiveCaptionText;
            Lbl_Maius.Font = new Font("Arial Black", 15.9000006F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Lbl_Maius.ForeColor = SystemColors.ActiveCaption;
            Lbl_Maius.Location = new Point(349, 60);
            Lbl_Maius.Name = "Lbl_Maius";
            Lbl_Maius.Size = new Size(150, 30);
            Lbl_Maius.TabIndex = 4;
            Lbl_Maius.Text = "Maiúsculas";
            // 
            // Lbl_Upper
            // 
            Lbl_Upper.BackColor = SystemColors.ActiveCaption;
            Lbl_Upper.BorderStyle = BorderStyle.Fixed3D;
            Lbl_Upper.Location = new Point(382, 90);
            Lbl_Upper.Name = "Lbl_Upper";
            Lbl_Upper.Size = new Size(79, 30);
            Lbl_Upper.TabIndex = 5;
            // 
            // Lbl_Lower
            // 
            Lbl_Lower.BackColor = SystemColors.ActiveCaption;
            Lbl_Lower.BorderStyle = BorderStyle.Fixed3D;
            Lbl_Lower.Location = new Point(382, 217);
            Lbl_Lower.Name = "Lbl_Lower";
            Lbl_Lower.Size = new Size(79, 32);
            Lbl_Lower.TabIndex = 6;
            // 
            // Frm_DemonstracaoKey
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ActiveCaptionText;
            ClientSize = new Size(528, 284);
            Controls.Add(Lbl_Lower);
            Controls.Add(Lbl_Upper);
            Controls.Add(Lbl_Maius);
            Controls.Add(Lbl_Minus);
            Controls.Add(Btn_Reset);
            Controls.Add(Txt_Msg);
            Controls.Add(Txt_Input);
            ForeColor = SystemColors.ActiveCaptionText;
            Name = "Frm_DemonstracaoKey";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form Demonstração Event Key";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox Txt_Input;
        private TextBox Txt_Msg;
        private Button Btn_Reset;
        private Label Lbl_Minus;
        private Label Lbl_Maius;
        private Label Lbl_Upper;
        private Label Lbl_Lower;
    }
}