namespace WinAppControlStore.FormsUC
{
    partial class FUC_CadastrarUsuario
    {
        /// <summary> 
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">true se for necessário descartar os recursos gerenciados; caso contrário, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Designer de Componentes

        /// <summary> 
        /// Método necessário para suporte ao Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            Lbl_CadastrarUsuarioTitulo = new Label();
            Lbl_EnderecoImagemLabel = new Label();
            Lbl_EnderecoImagem = new Label();
            pictureBox1 = new PictureBox();
            Lbl_NomeLabel = new Label();
            Txb_Login = new TextBox();
            Lbl_SenhaLabel = new Label();
            Txb_Senha = new TextBox();
            Btn_Cadastrar = new Button();
            Btn_Cancelar = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // Lbl_CadastrarUsuarioTitulo
            // 
            Lbl_CadastrarUsuarioTitulo.Anchor = AnchorStyles.Top;
            Lbl_CadastrarUsuarioTitulo.AutoSize = true;
            Lbl_CadastrarUsuarioTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Lbl_CadastrarUsuarioTitulo.Location = new Point(148, 37);
            Lbl_CadastrarUsuarioTitulo.Name = "Lbl_CadastrarUsuarioTitulo";
            Lbl_CadastrarUsuarioTitulo.Size = new Size(287, 32);
            Lbl_CadastrarUsuarioTitulo.TabIndex = 0;
            Lbl_CadastrarUsuarioTitulo.Text = "Cadastrar Novo Usuário";
            // 
            // Lbl_EnderecoImagemLabel
            // 
            Lbl_EnderecoImagemLabel.AutoSize = true;
            Lbl_EnderecoImagemLabel.Location = new Point(48, 104);
            Lbl_EnderecoImagemLabel.Name = "Lbl_EnderecoImagemLabel";
            Lbl_EnderecoImagemLabel.Size = new Size(89, 25);
            Lbl_EnderecoImagemLabel.TabIndex = 1;
            Lbl_EnderecoImagemLabel.Text = "Endereço:";
            // 
            // Lbl_EnderecoImagem
            // 
            Lbl_EnderecoImagem.AutoSize = true;
            Lbl_EnderecoImagem.Location = new Point(143, 104);
            Lbl_EnderecoImagem.Name = "Lbl_EnderecoImagem";
            Lbl_EnderecoImagem.Size = new Size(59, 25);
            Lbl_EnderecoImagem.TabIndex = 2;
            Lbl_EnderecoImagem.Text = "label1";
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(48, 155);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(200, 233);
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // Lbl_NomeLabel
            // 
            Lbl_NomeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Lbl_NomeLabel.AutoSize = true;
            Lbl_NomeLabel.Location = new Point(335, 155);
            Lbl_NomeLabel.Name = "Lbl_NomeLabel";
            Lbl_NomeLabel.Size = new Size(182, 25);
            Lbl_NomeLabel.TabIndex = 4;
            Lbl_NomeLabel.Text = "Insira o Nome(Login):";
            // 
            // Txb_Login
            // 
            Txb_Login.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Txb_Login.Location = new Point(335, 183);
            Txb_Login.Name = "Txb_Login";
            Txb_Login.Size = new Size(182, 31);
            Txb_Login.TabIndex = 5;
            // 
            // Lbl_SenhaLabel
            // 
            Lbl_SenhaLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Lbl_SenhaLabel.AutoSize = true;
            Lbl_SenhaLabel.Location = new Point(335, 256);
            Lbl_SenhaLabel.Name = "Lbl_SenhaLabel";
            Lbl_SenhaLabel.Size = new Size(125, 25);
            Lbl_SenhaLabel.TabIndex = 6;
            Lbl_SenhaLabel.Text = "Insira a Senha:";
            // 
            // Txb_Senha
            // 
            Txb_Senha.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Txb_Senha.Location = new Point(335, 284);
            Txb_Senha.Name = "Txb_Senha";
            Txb_Senha.Size = new Size(182, 31);
            Txb_Senha.TabIndex = 7;
            // 
            // Btn_Cadastrar
            // 
            Btn_Cadastrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            Btn_Cadastrar.Location = new Point(270, 409);
            Btn_Cadastrar.Name = "Btn_Cadastrar";
            Btn_Cadastrar.Size = new Size(132, 51);
            Btn_Cadastrar.TabIndex = 8;
            Btn_Cadastrar.Text = "Cadastrar";
            Btn_Cadastrar.UseVisualStyleBackColor = true;
            // 
            // Btn_Cancelar
            // 
            Btn_Cancelar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            Btn_Cancelar.Location = new Point(426, 409);
            Btn_Cancelar.Name = "Btn_Cancelar";
            Btn_Cancelar.Size = new Size(132, 51);
            Btn_Cancelar.TabIndex = 9;
            Btn_Cancelar.Text = "Cancelar";
            Btn_Cancelar.UseVisualStyleBackColor = true;
            // 
            // FUC_CadastrarUsuario
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(Btn_Cancelar);
            Controls.Add(Btn_Cadastrar);
            Controls.Add(Txb_Senha);
            Controls.Add(Lbl_SenhaLabel);
            Controls.Add(Txb_Login);
            Controls.Add(Lbl_NomeLabel);
            Controls.Add(pictureBox1);
            Controls.Add(Lbl_EnderecoImagem);
            Controls.Add(Lbl_EnderecoImagemLabel);
            Controls.Add(Lbl_CadastrarUsuarioTitulo);
            Name = "FUC_CadastrarUsuario";
            Size = new Size(590, 504);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label Lbl_CadastrarUsuarioTitulo;
        private Label Lbl_EnderecoImagemLabel;
        private Label Lbl_EnderecoImagem;
        private PictureBox pictureBox1;
        private Label Lbl_NomeLabel;
        private TextBox Txb_Login;
        private Label Lbl_SenhaLabel;
        private TextBox Txb_Senha;
        private Button Btn_Cadastrar;
        private Button Btn_Cancelar;
    }
}
