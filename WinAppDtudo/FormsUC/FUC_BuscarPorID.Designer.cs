namespace WinAppDtudo.FormsUC;

partial class FUC_BuscarPorID
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
        Lbl_FraseBuscarPorNome = new Label();
        Btn_BuscarPorID = new Button();
        Txb_InputBuscarPorID = new TextBox();
        Lbl_TituloMyAnimesID = new Label();
        SuspendLayout();
        // 
        // Lbl_FraseBuscarPorNome
        // 
        Lbl_FraseBuscarPorNome.AutoSize = true;
        Lbl_FraseBuscarPorNome.Location = new Point(89, 184);
        Lbl_FraseBuscarPorNome.Name = "Lbl_FraseBuscarPorNome";
        Lbl_FraseBuscarPorNome.Size = new Size(220, 25);
        Lbl_FraseBuscarPorNome.TabIndex = 7;
        Lbl_FraseBuscarPorNome.Text = "Digite aqui o ID do Anime";
        // 
        // Btn_BuscarPorID
        // 
        Btn_BuscarPorID.Location = new Point(83, 296);
        Btn_BuscarPorID.Name = "Btn_BuscarPorID";
        Btn_BuscarPorID.Size = new Size(226, 41);
        Btn_BuscarPorID.TabIndex = 6;
        Btn_BuscarPorID.Text = "Procurar";
        Btn_BuscarPorID.UseVisualStyleBackColor = true;
        // 
        // Txb_InputBuscarPorID
        // 
        Txb_InputBuscarPorID.Location = new Point(83, 212);
        Txb_InputBuscarPorID.Name = "Txb_InputBuscarPorID";
        Txb_InputBuscarPorID.Size = new Size(226, 31);
        Txb_InputBuscarPorID.TabIndex = 5;
        // 
        // Lbl_TituloMyAnimesID
        // 
        Lbl_TituloMyAnimesID.AutoSize = true;
        Lbl_TituloMyAnimesID.Font = new Font("Segoe UI Black", 15.8571434F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Lbl_TituloMyAnimesID.Location = new Point(64, 49);
        Lbl_TituloMyAnimesID.Name = "Lbl_TituloMyAnimesID";
        Lbl_TituloMyAnimesID.Size = new Size(271, 30);
        Lbl_TituloMyAnimesID.TabIndex = 4;
        Lbl_TituloMyAnimesID.Text = "Procurar Animes por ID";
        // 
        // FUC_BuscarPorID
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(Lbl_FraseBuscarPorNome);
        Controls.Add(Btn_BuscarPorID);
        Controls.Add(Txb_InputBuscarPorID);
        Controls.Add(Lbl_TituloMyAnimesID);
        Name = "FUC_BuscarPorID";
        Size = new Size(402, 365);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label Lbl_FraseBuscarPorNome;
    private Button Btn_BuscarPorID;
    private TextBox Txb_InputBuscarPorID;
    private Label Lbl_TituloMyAnimesID;
}
