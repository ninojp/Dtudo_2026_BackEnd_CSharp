namespace WinAppDtudo.Forms;

partial class Frm_Login
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
        Btn_Cancelar = new Button();
        Btn_Login = new Button();
        Txb_Senha = new TextBox();
        Lbl_SenhaLabel = new Label();
        Txb_Login = new TextBox();
        Lbl_NomeLabel = new Label();
        pictureBox1 = new PictureBox();
        ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
        SuspendLayout();
        // 
        // Btn_Cancelar
        // 
        Btn_Cancelar.Anchor = AnchorStyles.Top;
        Btn_Cancelar.BackColor = Color.Black;
        Btn_Cancelar.Location = new Point(558, 327);
        Btn_Cancelar.Name = "Btn_Cancelar";
        Btn_Cancelar.Size = new Size(203, 51);
        Btn_Cancelar.TabIndex = 25;
        Btn_Cancelar.Text = "Cancelar";
        Btn_Cancelar.UseVisualStyleBackColor = false;
        Btn_Cancelar.Click += Btn_Cancelar_Click;
        // 
        // Btn_Login
        // 
        Btn_Login.Anchor = AnchorStyles.Top;
        Btn_Login.BackColor = Color.Black;
        Btn_Login.Location = new Point(271, 327);
        Btn_Login.Name = "Btn_Login";
        Btn_Login.Size = new Size(203, 51);
        Btn_Login.TabIndex = 24;
        Btn_Login.Text = "Login";
        Btn_Login.UseVisualStyleBackColor = false;
        Btn_Login.Click += Btn_Login_Click;
        // 
        // Txb_Senha
        // 
        Txb_Senha.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        Txb_Senha.Location = new Point(267, 219);
        Txb_Senha.Name = "Txb_Senha";
        Txb_Senha.Size = new Size(497, 31);
        Txb_Senha.TabIndex = 23;
        // 
        // Lbl_SenhaLabel
        // 
        Lbl_SenhaLabel.Anchor = AnchorStyles.Top;
        Lbl_SenhaLabel.AutoSize = true;
        Lbl_SenhaLabel.Font = new Font("Segoe UI", 14.1428576F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Lbl_SenhaLabel.Location = new Point(453, 191);
        Lbl_SenhaLabel.Name = "Lbl_SenhaLabel";
        Lbl_SenhaLabel.Size = new Size(140, 25);
        Lbl_SenhaLabel.TabIndex = 22;
        Lbl_SenhaLabel.Text = "Insira a Senha:";
        // 
        // Txb_Login
        // 
        Txb_Login.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        Txb_Login.Location = new Point(267, 82);
        Txb_Login.Name = "Txb_Login";
        Txb_Login.Size = new Size(497, 31);
        Txb_Login.TabIndex = 21;
        // 
        // Lbl_NomeLabel
        // 
        Lbl_NomeLabel.Anchor = AnchorStyles.Top;
        Lbl_NomeLabel.AutoSize = true;
        Lbl_NomeLabel.Font = new Font("Segoe UI", 14.1428576F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Lbl_NomeLabel.Location = new Point(453, 54);
        Lbl_NomeLabel.Name = "Lbl_NomeLabel";
        Lbl_NomeLabel.Size = new Size(139, 25);
        Lbl_NomeLabel.TabIndex = 20;
        Lbl_NomeLabel.Text = "Insira o Login:";
        // 
        // pictureBox1
        // 
        pictureBox1.Image = Properties.Resources.CaveraMetal;
        pictureBox1.Location = new Point(57, 82);
        pictureBox1.Name = "pictureBox1";
        pictureBox1.Size = new Size(143, 168);
        pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
        pictureBox1.TabIndex = 26;
        pictureBox1.TabStop = false;
        // 
        // Frm_Login
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Desktop;
        ClientSize = new Size(950, 444);
        Controls.Add(pictureBox1);
        Controls.Add(Btn_Cancelar);
        Controls.Add(Btn_Login);
        Controls.Add(Txb_Senha);
        Controls.Add(Lbl_SenhaLabel);
        Controls.Add(Txb_Login);
        Controls.Add(Lbl_NomeLabel);
        ForeColor = Color.Gold;
        FormBorderStyle = FormBorderStyle.None;
        Name = "Frm_Login";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Frm_Login";
        ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Button Btn_Cancelar;
    private Button Btn_Login;
    private TextBox Txb_Senha;
    private Label Lbl_SenhaLabel;
    private TextBox Txb_Login;
    private Label Lbl_NomeLabel;
    private PictureBox pictureBox1;
}
