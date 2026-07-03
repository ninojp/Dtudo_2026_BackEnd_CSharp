namespace WinAppDtudo.Controls;

partial class UC_MiniAnimeCard
{
    /// <summary>
    /// Variável de designer necessária.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Limpar os recursos que estão sendo usados.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Código gerado pelo Designer de Componentes

    /// <summary>
    /// Método necessário para suporte ao Designer - não modifique
    /// o conteúdo deste método com o editor de código.
    /// </summary>
    private void InitializeComponent()
    {
        Pbx_Capa = new PictureBox();
        Lbl_MalId = new Label();
        Lbl_Nome = new Label();
        Lbl_Tipo = new Label();
        ((System.ComponentModel.ISupportInitialize)Pbx_Capa).BeginInit();
        SuspendLayout();
        //
        // Pbx_Capa
        //
        Pbx_Capa.BackColor = Color.FromArgb(45, 45, 55);
        Pbx_Capa.Location = new Point(5, 5);
        Pbx_Capa.Name = "Pbx_Capa";
        Pbx_Capa.Size = new Size(100, 130);
        Pbx_Capa.SizeMode = PictureBoxSizeMode.Zoom;
        Pbx_Capa.TabStop = false;
        Pbx_Capa.Cursor = Cursors.Hand;
        //
        // Lbl_MalId
        //
        Lbl_MalId.AutoSize = false;
        Lbl_MalId.Font = new Font("Segoe UI", 7.5F);
        Lbl_MalId.ForeColor = Color.Gray;
        Lbl_MalId.Location = new Point(3, 137);
        Lbl_MalId.Name = "Lbl_MalId";
        Lbl_MalId.Size = new Size(104, 15);
        Lbl_MalId.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_MalId.Cursor = Cursors.Hand;
        //
        // Lbl_Nome
        //
        Lbl_Nome.AutoSize = false;
        Lbl_Nome.AutoEllipsis = true;
        Lbl_Nome.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        Lbl_Nome.ForeColor = Color.FromArgb(30, 30, 30);
        Lbl_Nome.Location = new Point(3, 153);
        Lbl_Nome.Name = "Lbl_Nome";
        Lbl_Nome.Size = new Size(104, 38);
        Lbl_Nome.TextAlign = ContentAlignment.TopCenter;
        Lbl_Nome.Cursor = Cursors.Hand;
        //
        // Lbl_Tipo
        //
        Lbl_Tipo.AutoSize = false;
        Lbl_Tipo.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
        Lbl_Tipo.ForeColor = Color.RoyalBlue;
        Lbl_Tipo.Location = new Point(3, 193);
        Lbl_Tipo.Name = "Lbl_Tipo";
        Lbl_Tipo.Size = new Size(104, 16);
        Lbl_Tipo.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Tipo.Cursor = Cursors.Hand;
        //
        // UC_MiniAnimeCard
        //
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(247, 248, 252);
        Controls.Add(Pbx_Capa);
        Controls.Add(Lbl_MalId);
        Controls.Add(Lbl_Nome);
        Controls.Add(Lbl_Tipo);
        Cursor = Cursors.Hand;
        Name = "UC_MiniAnimeCard";
        Size = new Size(112, 215);
        ((System.ComponentModel.ISupportInitialize)Pbx_Capa).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private PictureBox Pbx_Capa;
    private Label Lbl_MalId;
    private Label Lbl_Nome;
    private Label Lbl_Tipo;
}
