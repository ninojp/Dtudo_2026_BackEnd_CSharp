namespace WinAppControlStore.FormsUC
{
    partial class FUC_BuscarPorNome
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
            Lbl_TituloMyAnimes = new Label();
            Txb_InputBuscarPorNome = new TextBox();
            Btn_BuscarPorNome = new Button();
            Lbl_FraseBuscarPorNome = new Label();
            SuspendLayout();
            // 
            // Lbl_TituloMyAnimes
            // 
            Lbl_TituloMyAnimes.AutoSize = true;
            Lbl_TituloMyAnimes.Font = new Font("Segoe UI Black", 15.8571434F, FontStyle.Bold, GraphicsUnit.Point, 0);
            Lbl_TituloMyAnimes.Location = new Point(168, 33);
            Lbl_TituloMyAnimes.Name = "Lbl_TituloMyAnimes";
            Lbl_TituloMyAnimes.Size = new Size(307, 30);
            Lbl_TituloMyAnimes.TabIndex = 0;
            Lbl_TituloMyAnimes.Text = "Procurar Animes por nome";
            // 
            // Txb_InputBuscarPorNome
            // 
            Txb_InputBuscarPorNome.Location = new Point(203, 218);
            Txb_InputBuscarPorNome.Name = "Txb_InputBuscarPorNome";
            Txb_InputBuscarPorNome.Size = new Size(226, 31);
            Txb_InputBuscarPorNome.TabIndex = 1;
            // 
            // Btn_BuscarPorNome
            // 
            Btn_BuscarPorNome.Location = new Point(203, 302);
            Btn_BuscarPorNome.Name = "Btn_BuscarPorNome";
            Btn_BuscarPorNome.Size = new Size(226, 41);
            Btn_BuscarPorNome.TabIndex = 2;
            Btn_BuscarPorNome.Text = "Procurar";
            Btn_BuscarPorNome.UseVisualStyleBackColor = true;
            // 
            // Lbl_FraseBuscarPorNome
            // 
            Lbl_FraseBuscarPorNome.AutoSize = true;
            Lbl_FraseBuscarPorNome.Location = new Point(192, 190);
            Lbl_FraseBuscarPorNome.Name = "Lbl_FraseBuscarPorNome";
            Lbl_FraseBuscarPorNome.Size = new Size(248, 25);
            Lbl_FraseBuscarPorNome.TabIndex = 3;
            Lbl_FraseBuscarPorNome.Text = "Digite aqui o nome do Anime";
            // 
            // FUC_BuscarPorNome
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(Lbl_FraseBuscarPorNome);
            Controls.Add(Btn_BuscarPorNome);
            Controls.Add(Txb_InputBuscarPorNome);
            Controls.Add(Lbl_TituloMyAnimes);
            Name = "FUC_BuscarPorNome";
            Size = new Size(636, 411);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label Lbl_TituloMyAnimes;
        private TextBox Txb_InputBuscarPorNome;
        private Button Btn_BuscarPorNome;
        private Label Lbl_FraseBuscarPorNome;
    }
}
