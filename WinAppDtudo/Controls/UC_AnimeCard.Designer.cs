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
        Pbx_Capa.BackColor = Color.FromArgb(45, 45, 55);
        Pbx_Capa.Location = new Point(10, 8);
        Pbx_Capa.Name = "Pbx_Capa";
        Pbx_Capa.Size = new Size(172, 222);
        Pbx_Capa.SizeMode = PictureBoxSizeMode.Zoom;
        Pbx_Capa.TabStop = false;
        Pbx_Capa.Cursor = Cursors.Hand;
        //
        // Lbl_Titulo
        //
        Lbl_Titulo.AutoSize = false;
        Lbl_Titulo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        Lbl_Titulo.Location = new Point(6, 234);
        Lbl_Titulo.Name = "Lbl_Titulo";
        Lbl_Titulo.Size = new Size(180, 36);
        Lbl_Titulo.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Titulo.Cursor = Cursors.Hand;
        //
        // Lbl_Ingles
        //
        Lbl_Ingles.AutoSize = false;
        Lbl_Ingles.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
        Lbl_Ingles.ForeColor = Color.DimGray;
        Lbl_Ingles.Location = new Point(6, 272);
        Lbl_Ingles.Name = "Lbl_Ingles";
        Lbl_Ingles.Size = new Size(180, 22);
        Lbl_Ingles.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Ingles.Cursor = Cursors.Hand;
        Lbl_Ingles.Visible = false;
        //
        // Lbl_Info
        //
        Lbl_Info.AutoSize = false;
        Lbl_Info.Font = new Font("Segoe UI", 8F);
        Lbl_Info.ForeColor = Color.DarkSlateGray;
        Lbl_Info.Location = new Point(6, 297);
        Lbl_Info.Name = "Lbl_Info";
        Lbl_Info.Size = new Size(180, 22);
        Lbl_Info.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Info.Cursor = Cursors.Hand;
        //
        // Lbl_Score
        //
        Lbl_Score.AutoSize = false;
        Lbl_Score.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        Lbl_Score.ForeColor = Color.DarkOrange;
        Lbl_Score.Location = new Point(6, 321);
        Lbl_Score.Name = "Lbl_Score";
        Lbl_Score.Size = new Size(180, 20);
        Lbl_Score.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Score.Cursor = Cursors.Hand;
        //
        // UC_AnimeCard
        //
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Control;
        Controls.Add(Pbx_Capa);
        Controls.Add(Lbl_Titulo);
        Controls.Add(Lbl_Ingles);
        Controls.Add(Lbl_Info);
        Controls.Add(Lbl_Score);
        Cursor = Cursors.Hand;
        Name = "UC_AnimeCard";
        Size = new Size(192, 345);
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
