namespace WinAppDtudo.FormsUC;

partial class FUC_DetalhesAnime
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
        Pnl_Header = new Panel();
        Lbl_TituloAnime = new Label();
        Lbl_TipoStatus = new Label();
        Lbl_Carregando = new Label();
        Pnl_Conteudo = new Panel();
        Pnl_Esquerda = new Panel();
        Pbx_Capa = new PictureBox();
        Pnl_Stats = new Panel();
        Lbl_Ano = new Label();
        Lbl_ScoreStat = new Label();
        Lbl_Rank = new Label();
        Lbl_Popularidade = new Label();
        Lbl_Episodios = new Label();
        Lbl_Duracao = new Label();
        Pnl_Info = new Panel();
        Pnl_Header.SuspendLayout();
        Pnl_Conteudo.SuspendLayout();
        Pnl_Esquerda.SuspendLayout();
        Pnl_Stats.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)Pbx_Capa).BeginInit();
        SuspendLayout();
        //
        // Pnl_Header
        //
        Pnl_Header.Controls.Add(Lbl_TituloAnime);
        Pnl_Header.Controls.Add(Lbl_TipoStatus);
        Pnl_Header.BackColor = Color.FromArgb(25, 30, 80);
        Pnl_Header.Dock = DockStyle.Top;
        Pnl_Header.Height = 62;
        Pnl_Header.Name = "Pnl_Header";
        Pnl_Header.Padding = new Padding(10, 6, 10, 4);
        //
        // Lbl_TituloAnime
        //
        Lbl_TituloAnime.AutoSize = false;
        Lbl_TituloAnime.Dock = DockStyle.Top;
        Lbl_TituloAnime.Font = new Font("Segoe UI Black", 13F, FontStyle.Bold);
        Lbl_TituloAnime.ForeColor = Color.White;
        Lbl_TituloAnime.Height = 34;
        Lbl_TituloAnime.Name = "Lbl_TituloAnime";
        Lbl_TituloAnime.Text = "—";
        //
        // Lbl_TipoStatus
        //
        Lbl_TipoStatus.AutoSize = false;
        Lbl_TipoStatus.Dock = DockStyle.Bottom;
        Lbl_TipoStatus.Font = new Font("Segoe UI", 8.5F);
        Lbl_TipoStatus.ForeColor = Color.LightSteelBlue;
        Lbl_TipoStatus.Height = 18;
        Lbl_TipoStatus.Name = "Lbl_TipoStatus";
        Lbl_TipoStatus.Text = "—";
        //
        // Lbl_Carregando
        //
        Lbl_Carregando.AutoSize = false;
        Lbl_Carregando.Dock = DockStyle.Fill;
        Lbl_Carregando.Font = new Font("Segoe UI", 14F);
        Lbl_Carregando.ForeColor = Color.Gray;
        Lbl_Carregando.Name = "Lbl_Carregando";
        Lbl_Carregando.Text = "⏳ Carregando detalhes do anime...";
        Lbl_Carregando.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Carregando.Visible = true;
        //
        // Pnl_Conteudo
        //
        Pnl_Conteudo.Controls.Add(Pnl_Info);
        Pnl_Conteudo.Controls.Add(Pnl_Esquerda);
        Pnl_Conteudo.Dock = DockStyle.Fill;
        Pnl_Conteudo.Name = "Pnl_Conteudo";
        Pnl_Conteudo.Visible = false;
        //
        // Pnl_Esquerda
        //
        Pnl_Esquerda.Controls.Add(Pnl_Stats);
        Pnl_Esquerda.Controls.Add(Pbx_Capa);
        Pnl_Esquerda.BackColor = Color.FromArgb(243, 244, 250);
        Pnl_Esquerda.Dock = DockStyle.Left;
        Pnl_Esquerda.Name = "Pnl_Esquerda";
        Pnl_Esquerda.Width = 272;
        //
        // Pbx_Capa
        //
        Pbx_Capa.BackColor = Color.FromArgb(50, 50, 60);
        Pbx_Capa.Dock = DockStyle.Top;
        Pbx_Capa.Height = 362;
        Pbx_Capa.Name = "Pbx_Capa";
        Pbx_Capa.SizeMode = PictureBoxSizeMode.Zoom;
        Pbx_Capa.TabStop = false;
        //
        // Pnl_Stats
        //
        Pnl_Stats.Controls.Add(Lbl_Ano);
        Pnl_Stats.Controls.Add(Lbl_ScoreStat);
        Pnl_Stats.Controls.Add(Lbl_Rank);
        Pnl_Stats.Controls.Add(Lbl_Popularidade);
        Pnl_Stats.Controls.Add(Lbl_Episodios);
        Pnl_Stats.Controls.Add(Lbl_Duracao);
        Pnl_Stats.Dock = DockStyle.Fill;
        Pnl_Stats.Name = "Pnl_Stats";
        Pnl_Stats.Padding = new Padding(10, 10, 6, 4);
        //
        // Labels de estatísticas
        //
        var statFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        var statColor = Color.FromArgb(35, 40, 90);

        Lbl_Ano.AutoSize = false;
        Lbl_Ano.Font = statFont;
        Lbl_Ano.ForeColor = statColor;
        Lbl_Ano.Location = new Point(10, 10);
        Lbl_Ano.Name = "Lbl_Ano";
        Lbl_Ano.Size = new Size(252, 24);

        Lbl_ScoreStat.AutoSize = false;
        Lbl_ScoreStat.Font = statFont;
        Lbl_ScoreStat.ForeColor = Color.DarkOrange;
        Lbl_ScoreStat.Location = new Point(10, 38);
        Lbl_ScoreStat.Name = "Lbl_ScoreStat";
        Lbl_ScoreStat.Size = new Size(252, 24);

        Lbl_Rank.AutoSize = false;
        Lbl_Rank.Font = statFont;
        Lbl_Rank.ForeColor = statColor;
        Lbl_Rank.Location = new Point(10, 66);
        Lbl_Rank.Name = "Lbl_Rank";
        Lbl_Rank.Size = new Size(252, 24);

        Lbl_Popularidade.AutoSize = false;
        Lbl_Popularidade.Font = statFont;
        Lbl_Popularidade.ForeColor = statColor;
        Lbl_Popularidade.Location = new Point(10, 94);
        Lbl_Popularidade.Name = "Lbl_Popularidade";
        Lbl_Popularidade.Size = new Size(252, 24);

        Lbl_Episodios.AutoSize = false;
        Lbl_Episodios.Font = statFont;
        Lbl_Episodios.ForeColor = statColor;
        Lbl_Episodios.Location = new Point(10, 122);
        Lbl_Episodios.Name = "Lbl_Episodios";
        Lbl_Episodios.Size = new Size(252, 24);

        Lbl_Duracao.AutoSize = false;
        Lbl_Duracao.Font = statFont;
        Lbl_Duracao.ForeColor = statColor;
        Lbl_Duracao.Location = new Point(10, 150);
        Lbl_Duracao.Name = "Lbl_Duracao";
        Lbl_Duracao.Size = new Size(252, 24);
        //
        // Pnl_Info
        //
        Pnl_Info.AutoScroll = true;
        Pnl_Info.BackColor = Color.White;
        Pnl_Info.Dock = DockStyle.Fill;
        Pnl_Info.Name = "Pnl_Info";
        Pnl_Info.Padding = new Padding(4);
        //
        // FUC_DetalhesAnime
        //
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(Lbl_Carregando);
        Controls.Add(Pnl_Conteudo);
        Controls.Add(Pnl_Header);
        Name = "FUC_DetalhesAnime";
        Size = new Size(960, 540);
        Pnl_Header.ResumeLayout(false);
        Pnl_Conteudo.ResumeLayout(false);
        Pnl_Esquerda.ResumeLayout(false);
        Pnl_Stats.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)Pbx_Capa).EndInit();
        ResumeLayout(false);
    }

    #endregion

    private Panel Pnl_Header;
    private Label Lbl_TituloAnime;
    private Label Lbl_TipoStatus;
    private Label Lbl_Carregando;
    private Panel Pnl_Conteudo;
    private Panel Pnl_Esquerda;
    private PictureBox Pbx_Capa;
    private Panel Pnl_Stats;
    private Label Lbl_Ano;
    private Label Lbl_ScoreStat;
    private Label Lbl_Rank;
    private Label Lbl_Popularidade;
    private Label Lbl_Episodios;
    private Label Lbl_Duracao;
    private Panel Pnl_Info;
}
