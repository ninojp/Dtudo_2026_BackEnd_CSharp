namespace WinAppDtudo.Controls;

partial class UC_AnimeCard
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
        if (disposing)
        {
            Pbx_Capa?.Image?.Dispose();
        }
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
        Lbl_Titulo = new Label();
        Lbl_Ingles = new Label();
        Lbl_Info = new Label();
        Lbl_Score = new Label();
        ((System.ComponentModel.ISupportInitialize)Pbx_Capa).BeginInit();
        SuspendLayout();
        // 
        // Pbx_Capa
        // 
        Pbx_Capa.BackColor = Color.DimGray;
        Pbx_Capa.Cursor = Cursors.Hand;
        Pbx_Capa.Location = new Point(8, 14);
        Pbx_Capa.Margin = new Padding(4, 3, 4, 3);
        Pbx_Capa.Name = "Pbx_Capa";
        Pbx_Capa.Size = new Size(325, 343);
        Pbx_Capa.SizeMode = PictureBoxSizeMode.Zoom;
        Pbx_Capa.TabIndex = 0;
        Pbx_Capa.TabStop = false;
        // 
        // Lbl_Titulo
        // 
        Lbl_Titulo.Cursor = Cursors.Hand;
        Lbl_Titulo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        Lbl_Titulo.Location = new Point(2, 360);
        Lbl_Titulo.Margin = new Padding(4, 0, 4, 0);
        Lbl_Titulo.Name = "Lbl_Titulo";
        Lbl_Titulo.Size = new Size(335, 42);
        Lbl_Titulo.TabIndex = 1;
        Lbl_Titulo.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // Lbl_Ingles
        // 
        Lbl_Ingles.Cursor = Cursors.Hand;
        Lbl_Ingles.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
        Lbl_Ingles.ForeColor = Color.DimGray;
        Lbl_Ingles.Location = new Point(2, 391);
        Lbl_Ingles.Margin = new Padding(4, 0, 4, 0);
        Lbl_Ingles.Name = "Lbl_Ingles";
        Lbl_Ingles.Size = new Size(335, 29);
        Lbl_Ingles.TabIndex = 2;
        Lbl_Ingles.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Ingles.Visible = false;
        // 
        // Lbl_Info
        // 
        Lbl_Info.Cursor = Cursors.Hand;
        Lbl_Info.Font = new Font("Segoe UI", 9.5F);
        Lbl_Info.ForeColor = Color.DarkSlateGray;
        Lbl_Info.Location = new Point(2, 430);
        Lbl_Info.Margin = new Padding(4, 0, 4, 0);
        Lbl_Info.Name = "Lbl_Info";
        Lbl_Info.Size = new Size(335, 27);
        Lbl_Info.TabIndex = 3;
        Lbl_Info.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // Lbl_Score
        // 
        Lbl_Score.Cursor = Cursors.Hand;
        Lbl_Score.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        Lbl_Score.ForeColor = Color.DarkOrange;
        Lbl_Score.Location = new Point(2, 466);
        Lbl_Score.Margin = new Padding(4, 0, 4, 0);
        Lbl_Score.Name = "Lbl_Score";
        Lbl_Score.Size = new Size(335, 27);
        Lbl_Score.TabIndex = 4;
        Lbl_Score.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // UC_AnimeCard
        // 
        AutoScaleDimensions = new SizeF(13F, 26F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Desktop;
        Controls.Add(Pbx_Capa);
        Controls.Add(Lbl_Titulo);
        Controls.Add(Lbl_Ingles);
        Controls.Add(Lbl_Info);
        Controls.Add(Lbl_Score);
        Cursor = Cursors.Hand;
        ForeColor = Color.Gold;
        Margin = new Padding(4, 3, 4, 3);
        Name = "UC_AnimeCard";
        Size = new Size(341, 510);
        ((System.ComponentModel.ISupportInitialize)Pbx_Capa).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private PictureBox Pbx_Capa;
    private Label Lbl_Titulo;
    private Label Lbl_Ingles;
    private Label Lbl_Info;
    private Label Lbl_Score;
}
