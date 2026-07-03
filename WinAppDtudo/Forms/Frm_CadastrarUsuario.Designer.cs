namespace WinAppDtudo.Forms;

partial class Frm_CadastrarUsuario
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_CadastrarUsuario));
        Btn_Cancelar = new Button();
        Btn_Cadastrar = new Button();
        Txb_Senha = new TextBox();
        Lbl_SenhaLabel = new Label();
        Txb_Login = new TextBox();
        Lbl_NomeLabel = new Label();
        Pic_PictureBoxImgTemp = new PictureBox();
        Lbl_EnderecoImagem = new Label();
        Lbl_EnderecoImagemLabel = new Label();
        Lbl_CadastrarUsuarioTitulo = new Label();
        Btn_ImagemPerfil = new Button();
        Btn_FontDialogBox = new Button();
        Btn_CorDialogBox = new Button();
        ((System.ComponentModel.ISupportInitialize)Pic_PictureBoxImgTemp).BeginInit();
        SuspendLayout();
        // 
        // Btn_Cancelar
        // 
        Btn_Cancelar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        Btn_Cancelar.Location = new Point(870, 591);
        Btn_Cancelar.Margin = new Padding(4, 3, 4, 3);
        Btn_Cancelar.Name = "Btn_Cancelar";
        Btn_Cancelar.Size = new Size(172, 53);
        Btn_Cancelar.TabIndex = 18;
        Btn_Cancelar.Text = "Cancelar";
        Btn_Cancelar.UseVisualStyleBackColor = true;
        Btn_Cancelar.Click += Btn_Cancelar_Click;
        // 
        // Btn_Cadastrar
        // 
        Btn_Cadastrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        Btn_Cadastrar.Location = new Point(637, 591);
        Btn_Cadastrar.Margin = new Padding(4, 3, 4, 3);
        Btn_Cadastrar.Name = "Btn_Cadastrar";
        Btn_Cadastrar.Size = new Size(172, 53);
        Btn_Cadastrar.TabIndex = 17;
        Btn_Cadastrar.Text = "Cadastrar";
        Btn_Cadastrar.UseVisualStyleBackColor = true;
        // 
        // Txb_Senha
        // 
        Txb_Senha.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Txb_Senha.Location = new Point(749, 322);
        Txb_Senha.Margin = new Padding(4, 3, 4, 3);
        Txb_Senha.Name = "Txb_Senha";
        Txb_Senha.Size = new Size(235, 32);
        Txb_Senha.TabIndex = 16;
        // 
        // Lbl_SenhaLabel
        // 
        Lbl_SenhaLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Lbl_SenhaLabel.AutoSize = true;
        Lbl_SenhaLabel.Location = new Point(799, 293);
        Lbl_SenhaLabel.Margin = new Padding(4, 0, 4, 0);
        Lbl_SenhaLabel.Name = "Lbl_SenhaLabel";
        Lbl_SenhaLabel.Size = new Size(158, 26);
        Lbl_SenhaLabel.TabIndex = 16;
        Lbl_SenhaLabel.Text = "Insira a Senha:";
        // 
        // Txb_Login
        // 
        Txb_Login.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Txb_Login.Location = new Point(749, 160);
        Txb_Login.Margin = new Padding(4, 3, 4, 3);
        Txb_Login.Name = "Txb_Login";
        Txb_Login.Size = new Size(235, 32);
        Txb_Login.TabIndex = 15;
        // 
        // Lbl_NomeLabel
        // 
        Lbl_NomeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Lbl_NomeLabel.AutoSize = true;
        Lbl_NomeLabel.Location = new Point(749, 131);
        Lbl_NomeLabel.Margin = new Padding(4, 0, 4, 0);
        Lbl_NomeLabel.Name = "Lbl_NomeLabel";
        Lbl_NomeLabel.Size = new Size(221, 26);
        Lbl_NomeLabel.TabIndex = 14;
        Lbl_NomeLabel.Text = "Insira o Nome(Login):";
        // 
        // Pic_PictureBoxImgTemp
        // 
        Pic_PictureBoxImgTemp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Pic_PictureBoxImgTemp.Location = new Point(16, 131);
        Pic_PictureBoxImgTemp.Margin = new Padding(4, 3, 4, 3);
        Pic_PictureBoxImgTemp.Name = "Pic_PictureBoxImgTemp";
        Pic_PictureBoxImgTemp.Size = new Size(539, 513);
        Pic_PictureBoxImgTemp.SizeMode = PictureBoxSizeMode.StretchImage;
        Pic_PictureBoxImgTemp.TabIndex = 13;
        Pic_PictureBoxImgTemp.TabStop = false;
        // 
        // Lbl_EnderecoImagem
        // 
        Lbl_EnderecoImagem.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Lbl_EnderecoImagem.AutoSize = true;
        Lbl_EnderecoImagem.Location = new Point(109, 668);
        Lbl_EnderecoImagem.Margin = new Padding(4, 0, 4, 0);
        Lbl_EnderecoImagem.Name = "Lbl_EnderecoImagem";
        Lbl_EnderecoImagem.Size = new Size(70, 26);
        Lbl_EnderecoImagem.TabIndex = 12;
        Lbl_EnderecoImagem.Text = "label1";
        // 
        // Lbl_EnderecoImagemLabel
        // 
        Lbl_EnderecoImagemLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Lbl_EnderecoImagemLabel.AutoSize = true;
        Lbl_EnderecoImagemLabel.Location = new Point(4, 668);
        Lbl_EnderecoImagemLabel.Margin = new Padding(4, 0, 4, 0);
        Lbl_EnderecoImagemLabel.Name = "Lbl_EnderecoImagemLabel";
        Lbl_EnderecoImagemLabel.Size = new Size(111, 26);
        Lbl_EnderecoImagemLabel.TabIndex = 11;
        Lbl_EnderecoImagemLabel.Text = "Endereço:";
        // 
        // Lbl_CadastrarUsuarioTitulo
        // 
        Lbl_CadastrarUsuarioTitulo.Anchor = AnchorStyles.Top;
        Lbl_CadastrarUsuarioTitulo.AutoSize = true;
        Lbl_CadastrarUsuarioTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Lbl_CadastrarUsuarioTitulo.Location = new Point(360, 26);
        Lbl_CadastrarUsuarioTitulo.Margin = new Padding(4, 0, 4, 0);
        Lbl_CadastrarUsuarioTitulo.Name = "Lbl_CadastrarUsuarioTitulo";
        Lbl_CadastrarUsuarioTitulo.Size = new Size(565, 65);
        Lbl_CadastrarUsuarioTitulo.TabIndex = 10;
        Lbl_CadastrarUsuarioTitulo.Text = "Cadastrar Novo Usuário";
        // 
        // Btn_ImagemPerfil
        // 
        Btn_ImagemPerfil.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Btn_ImagemPerfil.Location = new Point(58, 591);
        Btn_ImagemPerfil.Margin = new Padding(4, 3, 4, 3);
        Btn_ImagemPerfil.Name = "Btn_ImagemPerfil";
        Btn_ImagemPerfil.Size = new Size(192, 53);
        Btn_ImagemPerfil.TabIndex = 19;
        Btn_ImagemPerfil.Text = "Imagem Perfil";
        Btn_ImagemPerfil.UseVisualStyleBackColor = true;
        Btn_ImagemPerfil.Click += Btn_ImagemPerfil_Click;
        // 
        // Btn_FontDialogBox
        // 
        Btn_FontDialogBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Btn_FontDialogBox.Location = new Point(384, 591);
        Btn_FontDialogBox.Margin = new Padding(4, 3, 4, 3);
        Btn_FontDialogBox.Name = "Btn_FontDialogBox";
        Btn_FontDialogBox.Size = new Size(75, 53);
        Btn_FontDialogBox.TabIndex = 21;
        Btn_FontDialogBox.Text = "Font";
        Btn_FontDialogBox.UseVisualStyleBackColor = true;
        Btn_FontDialogBox.Click += Btn_FontDialogBox_Click;
        // 
        // Btn_CorDialogBox
        // 
        Btn_CorDialogBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        Btn_CorDialogBox.Location = new Point(306, 591);
        Btn_CorDialogBox.Margin = new Padding(4, 3, 4, 3);
        Btn_CorDialogBox.Name = "Btn_CorDialogBox";
        Btn_CorDialogBox.Size = new Size(70, 53);
        Btn_CorDialogBox.TabIndex = 20;
        Btn_CorDialogBox.Text = "Cor";
        Btn_CorDialogBox.UseVisualStyleBackColor = true;
        Btn_CorDialogBox.Click += Btn_CorDialogBox_Click;
        // 
        // Frm_CadastrarUsuario
        // 
        AutoScaleDimensions = new SizeF(13F, 26F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1099, 703);
        Controls.Add(Btn_CorDialogBox);
        Controls.Add(Btn_FontDialogBox);
        Controls.Add(Btn_ImagemPerfil);
        Controls.Add(Btn_Cancelar);
        Controls.Add(Btn_Cadastrar);
        Controls.Add(Txb_Senha);
        Controls.Add(Lbl_SenhaLabel);
        Controls.Add(Txb_Login);
        Controls.Add(Lbl_NomeLabel);
        Controls.Add(Pic_PictureBoxImgTemp);
        Controls.Add(Lbl_EnderecoImagem);
        Controls.Add(Lbl_EnderecoImagemLabel);
        Controls.Add(Lbl_CadastrarUsuarioTitulo);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(4, 3, 4, 3);
        Name = "Frm_CadastrarUsuario";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Cadastrar Novo Usuário";
        ((System.ComponentModel.ISupportInitialize)Pic_PictureBoxImgTemp).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Button Btn_Cancelar;
    private Button Btn_Cadastrar;
    private TextBox Txb_Senha;
    private Label Lbl_SenhaLabel;
    private TextBox Txb_Login;
    private Label Lbl_NomeLabel;
    private PictureBox Pic_PictureBoxImgTemp;
    private Label Lbl_EnderecoImagem;
    private Label Lbl_EnderecoImagemLabel;
    private Label Lbl_CadastrarUsuarioTitulo;
    private Button Btn_ImagemPerfil;
    private Button Btn_FontDialogBox;
    private Button Btn_CorDialogBox;
}
