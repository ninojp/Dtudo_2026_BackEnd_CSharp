namespace WinAppControlStore
{
    partial class FUC_Mascaras
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
            Msk_TextBox = new MaskedTextBox();
            Lbl_MascaraAtual = new Label();
            Lbl_MascaraAtiva = new Label();
            Lbl_MascaraConteudo = new Label();
            Lbl_Conteudo = new Label();
            Btn_CEP = new Button();
            Btn_Hora = new Button();
            Btn_Moeda = new Button();
            Btn_Data = new Button();
            Btn_Senha = new Button();
            Btn_Telefone = new Button();
            Btn_VerConteudo = new Button();
            SuspendLayout();
            // 
            // Msk_TextBox
            // 
            Msk_TextBox.Location = new Point(73, 122);
            Msk_TextBox.Name = "Msk_TextBox";
            Msk_TextBox.Size = new Size(483, 31);
            Msk_TextBox.TabIndex = 0;
            // 
            // Lbl_MascaraAtual
            // 
            Lbl_MascaraAtual.AutoSize = true;
            Lbl_MascaraAtual.Location = new Point(73, 64);
            Lbl_MascaraAtual.Name = "Lbl_MascaraAtual";
            Lbl_MascaraAtual.Size = new Size(127, 25);
            Lbl_MascaraAtual.TabIndex = 1;
            Lbl_MascaraAtual.Text = "Mascara Atual:";
            // 
            // Lbl_MascaraAtiva
            // 
            Lbl_MascaraAtiva.AutoSize = true;
            Lbl_MascaraAtiva.Location = new Point(206, 64);
            Lbl_MascaraAtiva.Name = "Lbl_MascaraAtiva";
            Lbl_MascaraAtiva.Size = new Size(117, 25);
            Lbl_MascaraAtiva.TabIndex = 2;
            Lbl_MascaraAtiva.Text = "MascaraAtiva";
            // 
            // Lbl_MascaraConteudo
            // 
            Lbl_MascaraConteudo.AutoSize = true;
            Lbl_MascaraConteudo.Location = new Point(73, 187);
            Lbl_MascaraConteudo.Name = "Lbl_MascaraConteudo";
            Lbl_MascaraConteudo.Size = new Size(165, 25);
            Lbl_MascaraConteudo.TabIndex = 3;
            Lbl_MascaraConteudo.Text = "Mascara Conteudo:";
            // 
            // Lbl_Conteudo
            // 
            Lbl_Conteudo.AutoSize = true;
            Lbl_Conteudo.Location = new Point(244, 197);
            Lbl_Conteudo.Name = "Lbl_Conteudo";
            Lbl_Conteudo.Size = new Size(0, 25);
            Lbl_Conteudo.TabIndex = 4;
            // 
            // Btn_CEP
            // 
            Btn_CEP.BackColor = Color.Transparent;
            Btn_CEP.Location = new Point(73, 261);
            Btn_CEP.Name = "Btn_CEP";
            Btn_CEP.Size = new Size(137, 60);
            Btn_CEP.TabIndex = 5;
            Btn_CEP.Text = "CEP";
            Btn_CEP.UseVisualStyleBackColor = false;
            Btn_CEP.Click += Btn_CEP_Click;
            // 
            // Btn_Hora
            // 
            Btn_Hora.Location = new Point(246, 261);
            Btn_Hora.Name = "Btn_Hora";
            Btn_Hora.Size = new Size(137, 60);
            Btn_Hora.TabIndex = 6;
            Btn_Hora.Text = "Hora";
            Btn_Hora.UseVisualStyleBackColor = true;
            Btn_Hora.Click += Btn_Hora_Click;
            // 
            // Btn_Moeda
            // 
            Btn_Moeda.Location = new Point(419, 261);
            Btn_Moeda.Name = "Btn_Moeda";
            Btn_Moeda.Size = new Size(137, 60);
            Btn_Moeda.TabIndex = 7;
            Btn_Moeda.Text = "Moeda";
            Btn_Moeda.UseVisualStyleBackColor = true;
            Btn_Moeda.Click += Btn_Moeda_Click;
            // 
            // Btn_Data
            // 
            Btn_Data.Location = new Point(73, 341);
            Btn_Data.Name = "Btn_Data";
            Btn_Data.Size = new Size(137, 60);
            Btn_Data.TabIndex = 8;
            Btn_Data.Text = "Data";
            Btn_Data.UseVisualStyleBackColor = true;
            Btn_Data.Click += Btn_Data_Click;
            // 
            // Btn_Senha
            // 
            Btn_Senha.Location = new Point(246, 341);
            Btn_Senha.Name = "Btn_Senha";
            Btn_Senha.Size = new Size(137, 60);
            Btn_Senha.TabIndex = 9;
            Btn_Senha.Text = "Senha";
            Btn_Senha.UseVisualStyleBackColor = true;
            Btn_Senha.Click += Btn_Senha_Click;
            // 
            // Btn_Telefone
            // 
            Btn_Telefone.Location = new Point(419, 341);
            Btn_Telefone.Name = "Btn_Telefone";
            Btn_Telefone.Size = new Size(137, 60);
            Btn_Telefone.TabIndex = 10;
            Btn_Telefone.Text = "Telefone";
            Btn_Telefone.UseVisualStyleBackColor = true;
            Btn_Telefone.Click += Btn_Telefone_Click;
            // 
            // Btn_VerConteudo
            // 
            Btn_VerConteudo.Location = new Point(73, 428);
            Btn_VerConteudo.Name = "Btn_VerConteudo";
            Btn_VerConteudo.Size = new Size(483, 60);
            Btn_VerConteudo.TabIndex = 11;
            Btn_VerConteudo.Text = "Ver conteudo Mascara";
            Btn_VerConteudo.UseVisualStyleBackColor = true;
            Btn_VerConteudo.Click += Btn_VerConteudo_Click;
            // 
            // FUC_Mascaras
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.WindowText;
            Controls.Add(Btn_VerConteudo);
            Controls.Add(Btn_Telefone);
            Controls.Add(Btn_Senha);
            Controls.Add(Btn_Data);
            Controls.Add(Btn_Moeda);
            Controls.Add(Btn_Hora);
            Controls.Add(Btn_CEP);
            Controls.Add(Lbl_Conteudo);
            Controls.Add(Lbl_MascaraConteudo);
            Controls.Add(Lbl_MascaraAtiva);
            Controls.Add(Lbl_MascaraAtual);
            Controls.Add(Msk_TextBox);
            ForeColor = Color.Gold;
            Name = "FUC_Mascaras";
            Size = new Size(633, 551);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MaskedTextBox Msk_TextBox;
        private Label Lbl_MascaraAtual;
        private Label Lbl_MascaraAtiva;
        private Label Lbl_MascaraConteudo;
        private Label Lbl_Conteudo;
        private Button Btn_CEP;
        private Button Btn_Hora;
        private Button Btn_Moeda;
        private Button Btn_Data;
        private Button Btn_Senha;
        private Button Btn_Telefone;
        private Button Btn_VerConteudo;
    }
}
