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
        Pnl_Info = new Panel();
        Pnl_Esquerda = new Panel();
        Pnl_Stats = new Panel();
        Lbl_Ano = new Label();
        Lbl_ScoreStat = new Label();
        Lbl_Rank = new Label();
        Lbl_Popularidade = new Label();
        Lbl_Episodios = new Label();
        Lbl_Duracao = new Label();
        Pbx_Capa = new PictureBox();
        Pnl_Header.SuspendLayout();
        Pnl_Conteudo.SuspendLayout();
        Pnl_Esquerda.SuspendLayout();
        Pnl_Stats.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)Pbx_Capa).BeginInit();
        SuspendLayout();
        // 
        // Pnl_Header
        // 
        Pnl_Header.BackColor = Color.FromArgb(25, 30, 80);
        Pnl_Header.Controls.Add(Lbl_TituloAnime);
        Pnl_Header.Controls.Add(Lbl_TipoStatus);
        Pnl_Header.Dock = DockStyle.Top;
        Pnl_Header.Location = new Point(0, 0);
        Pnl_Header.Margin = new Padding(4, 3, 4, 3);
        Pnl_Header.Name = "Pnl_Header";
        Pnl_Header.Padding = new Padding(13, 6, 13, 4);
        Pnl_Header.Size = new Size(1248, 64);
        Pnl_Header.TabIndex = 2;
        // 
        // Lbl_TituloAnime
        // 
        Lbl_TituloAnime.Dock = DockStyle.Top;
        Lbl_TituloAnime.Font = new Font("Segoe UI Black", 13F, FontStyle.Bold);
        Lbl_TituloAnime.ForeColor = Color.White;
        Lbl_TituloAnime.Location = new Point(13, 6);
        Lbl_TituloAnime.Margin = new Padding(4, 0, 4, 0);
        Lbl_TituloAnime.Name = "Lbl_TituloAnime";
        Lbl_TituloAnime.Size = new Size(1222, 35);
        Lbl_TituloAnime.TabIndex = 0;
        Lbl_TituloAnime.Text = "—";
        // 
        // Lbl_TipoStatus
        // 
        Lbl_TipoStatus.Dock = DockStyle.Bottom;
        Lbl_TipoStatus.Font = new Font("Segoe UI", 8.5F);
        Lbl_TipoStatus.ForeColor = Color.LightSteelBlue;
        Lbl_TipoStatus.Location = new Point(13, 41);
        Lbl_TipoStatus.Margin = new Padding(4, 0, 4, 0);
        Lbl_TipoStatus.Name = "Lbl_TipoStatus";
        Lbl_TipoStatus.Size = new Size(1222, 19);
        Lbl_TipoStatus.TabIndex = 1;
        Lbl_TipoStatus.Text = "—";
        // 
        // Lbl_Carregando
        // 
        Lbl_Carregando.BackColor = SystemColors.Desktop;
        Lbl_Carregando.Dock = DockStyle.Fill;
        Lbl_Carregando.Font = new Font("Segoe UI", 14F);
        Lbl_Carregando.ForeColor = Color.Gold;
        Lbl_Carregando.Location = new Point(0, 64);
        Lbl_Carregando.Margin = new Padding(4, 0, 4, 0);
        Lbl_Carregando.Name = "Lbl_Carregando";
        Lbl_Carregando.Size = new Size(1248, 498);
        Lbl_Carregando.TabIndex = 0;
        Lbl_Carregando.Text = "⏳ Carregando detalhes do anime...";
        Lbl_Carregando.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // Pnl_Conteudo
        // 
        Pnl_Conteudo.Controls.Add(Pnl_Info);
        Pnl_Conteudo.Controls.Add(Pnl_Esquerda);
        Pnl_Conteudo.Dock = DockStyle.Fill;
        Pnl_Conteudo.Location = new Point(0, 64);
        Pnl_Conteudo.Margin = new Padding(4, 3, 4, 3);
        Pnl_Conteudo.Name = "Pnl_Conteudo";
        Pnl_Conteudo.Size = new Size(1248, 498);
        Pnl_Conteudo.TabIndex = 1;
        Pnl_Conteudo.Visible = false;
        // 
        // Pnl_Info
        // 
        Pnl_Info.AutoScroll = true;
        Pnl_Info.BackColor = Color.White;
        Pnl_Info.Dock = DockStyle.Fill;
        Pnl_Info.Location = new Point(354, 0);
        Pnl_Info.Margin = new Padding(4, 3, 4, 3);
        Pnl_Info.Name = "Pnl_Info";
        Pnl_Info.Padding = new Padding(5, 4, 5, 4);
        Pnl_Info.Size = new Size(894, 498);
        Pnl_Info.TabIndex = 0;
        // 
        // Pnl_Esquerda
        // 
        Pnl_Esquerda.BackColor = Color.FromArgb(243, 244, 250);
        Pnl_Esquerda.Controls.Add(Pnl_Stats);
        Pnl_Esquerda.Controls.Add(Pbx_Capa);
        Pnl_Esquerda.Dock = DockStyle.Left;
        Pnl_Esquerda.Location = new Point(0, 0);
        Pnl_Esquerda.Margin = new Padding(4, 3, 4, 3);
        Pnl_Esquerda.Name = "Pnl_Esquerda";
        Pnl_Esquerda.Size = new Size(354, 498);
        Pnl_Esquerda.TabIndex = 1;
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
        Pnl_Stats.Location = new Point(0, 376);
        Pnl_Stats.Margin = new Padding(4, 3, 4, 3);
        Pnl_Stats.Name = "Pnl_Stats";
        Pnl_Stats.Padding = new Padding(13, 10, 8, 4);
        Pnl_Stats.Size = new Size(354, 122);
        Pnl_Stats.TabIndex = 0;
        // 
        // Lbl_Ano
        // 
        Lbl_Ano.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        Lbl_Ano.ForeColor = Color.FromArgb(35, 40, 90);
        Lbl_Ano.Location = new Point(13, 10);
        Lbl_Ano.Margin = new Padding(4, 0, 4, 0);
        Lbl_Ano.Name = "Lbl_Ano";
        Lbl_Ano.Size = new Size(328, 25);
        Lbl_Ano.TabIndex = 0;
        // 
        // Lbl_ScoreStat
        // 
        Lbl_ScoreStat.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        Lbl_ScoreStat.ForeColor = Color.DarkOrange;
        Lbl_ScoreStat.Location = new Point(13, 40);
        Lbl_ScoreStat.Margin = new Padding(4, 0, 4, 0);
        Lbl_ScoreStat.Name = "Lbl_ScoreStat";
        Lbl_ScoreStat.Size = new Size(328, 25);
        Lbl_ScoreStat.TabIndex = 1;
        // 
        // Lbl_Rank
        // 
        Lbl_Rank.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        Lbl_Rank.ForeColor = Color.FromArgb(35, 40, 90);
        Lbl_Rank.Location = new Point(13, 69);
        Lbl_Rank.Margin = new Padding(4, 0, 4, 0);
        Lbl_Rank.Name = "Lbl_Rank";
        Lbl_Rank.Size = new Size(328, 25);
        Lbl_Rank.TabIndex = 2;
        // 
        // Lbl_Popularidade
        // 
        Lbl_Popularidade.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        Lbl_Popularidade.ForeColor = Color.FromArgb(35, 40, 90);
        Lbl_Popularidade.Location = new Point(13, 98);
        Lbl_Popularidade.Margin = new Padding(4, 0, 4, 0);
        Lbl_Popularidade.Name = "Lbl_Popularidade";
        Lbl_Popularidade.Size = new Size(328, 25);
        Lbl_Popularidade.TabIndex = 3;
        // 
        // Lbl_Episodios
        // 
        Lbl_Episodios.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        Lbl_Episodios.ForeColor = Color.FromArgb(35, 40, 90);
        Lbl_Episodios.Location = new Point(13, 127);
        Lbl_Episodios.Margin = new Padding(4, 0, 4, 0);
        Lbl_Episodios.Name = "Lbl_Episodios";
        Lbl_Episodios.Size = new Size(328, 25);
        Lbl_Episodios.TabIndex = 4;
        // 
        // Lbl_Duracao
        // 
        Lbl_Duracao.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        Lbl_Duracao.ForeColor = Color.FromArgb(35, 40, 90);
        Lbl_Duracao.Location = new Point(13, 156);
        Lbl_Duracao.Margin = new Padding(4, 0, 4, 0);
        Lbl_Duracao.Name = "Lbl_Duracao";
        Lbl_Duracao.Size = new Size(328, 25);
        Lbl_Duracao.TabIndex = 5;
        // 
        // Pbx_Capa
        // 
        Pbx_Capa.BackColor = Color.FromArgb(50, 50, 60);
        Pbx_Capa.Dock = DockStyle.Top;
        Pbx_Capa.Location = new Point(0, 0);
        Pbx_Capa.Margin = new Padding(4, 3, 4, 3);
        Pbx_Capa.Name = "Pbx_Capa";
        Pbx_Capa.Size = new Size(354, 376);
        Pbx_Capa.SizeMode = PictureBoxSizeMode.Zoom;
        Pbx_Capa.TabIndex = 1;
        Pbx_Capa.TabStop = false;
        // 
        // FUC_DetalhesAnime
        // 
        AutoScaleDimensions = new SizeF(13F, 26F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = SystemColors.Desktop;
        Controls.Add(Lbl_Carregando);
        Controls.Add(Pnl_Conteudo);
        Controls.Add(Pnl_Header);
        ForeColor = Color.Gold;
        Margin = new Padding(4, 3, 4, 3);
        Name = "FUC_DetalhesAnime";
        Size = new Size(1248, 562);
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
