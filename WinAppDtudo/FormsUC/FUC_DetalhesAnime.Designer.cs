using WinAppDtudo.Controls;

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
        Lbl_TituloAnime = new SelectableTextLabel();
        Lbl_TituloIngles = new SelectableTextLabel();
        Lbl_Sinonimo = new SelectableTextLabel();
        Lbl_TituloJapones = new SelectableTextLabel();
        Lbl_Carregando = new Label();
        Pnl_Conteudo = new Panel();
        Pnl_Info = new Panel();
        Pnl_Esquerda = new Panel();
        Pnl_Acoes = new Panel();
        Btn_SalvarComoAnime = new Button();
        Btn_SalvarComoMyAnime = new Button();
        Btn_ExibirMyAnime = new Button();
        Btn_EditarAnime = new Button();
        Pnl_Stats = new Panel();
        Lbl_EstatisticasRapidas = new Label();
        Lbl_Generos = new Label();
        Lbl_Episodios = new Label();
        Lbl_TempoPorEpisodio = new Label();
        Pbx_Capa = new PictureBox();
        Lbl_Rank = new Label();
        Lbl_Popularidade = new Label();
        Pnl_Header.SuspendLayout();
        Pnl_Conteudo.SuspendLayout();
        Pnl_Esquerda.SuspendLayout();
        Pnl_Acoes.SuspendLayout();
        Pnl_Stats.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)Pbx_Capa).BeginInit();
        SuspendLayout();
        // 
        // Pnl_Header
        // 
        Pnl_Header.BackColor = Color.FromArgb(25, 30, 80);
        Pnl_Header.Controls.Add(Lbl_TituloAnime);
        Pnl_Header.Controls.Add(Lbl_TituloIngles);
        Pnl_Header.Controls.Add(Lbl_Sinonimo);
        Pnl_Header.Controls.Add(Lbl_TituloJapones);
        Pnl_Header.Dock = DockStyle.Top;
        Pnl_Header.Location = new Point(0, 0);
        Pnl_Header.Margin = new Padding(4, 3, 4, 3);
        Pnl_Header.Name = "Pnl_Header";
        Pnl_Header.Padding = new Padding(50, 6, 13, 4);
        Pnl_Header.Size = new Size(1920, 200);
        Pnl_Header.TabIndex = 2;
        // 
        // Lbl_TituloAnime
        // 
        Lbl_TituloAnime.AutoEllipsis = true;
        Lbl_TituloAnime.Font = new Font("Segoe UI Black", 15F, FontStyle.Bold);
        Lbl_TituloAnime.ForeColor = Color.White;
        Lbl_TituloAnime.Location = new Point(50, 10);
        Lbl_TituloAnime.Margin = new Padding(40, 0, 40, 0);
        Lbl_TituloAnime.Name = "Lbl_TituloAnime";
        Lbl_TituloAnime.Size = new Size(970, 50);
        Lbl_TituloAnime.TabIndex = 0;
        Lbl_TituloAnime.Text = "—";
        Lbl_TituloAnime.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // Lbl_TituloIngles
        // 
        Lbl_TituloIngles.AutoEllipsis = true;
        Lbl_TituloIngles.AutoSize = false;
        Lbl_TituloIngles.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        Lbl_TituloIngles.ForeColor = Color.Gold;
        Lbl_TituloIngles.Location = new Point(50, 10);
        Lbl_TituloIngles.MaximumSize = new Size(1800, 0);
        Lbl_TituloIngles.Name = "Lbl_TituloIngles";
        Lbl_TituloIngles.Size = new Size(930, 50);
        Lbl_TituloIngles.TabIndex = 2;
        Lbl_TituloIngles.TextAlign = ContentAlignment.MiddleLeft;
        Lbl_TituloIngles.Visible = false;
        // 
        // Lbl_Sinonimo
        // 
        Lbl_Sinonimo.AutoEllipsis = true;
        Lbl_Sinonimo.AutoSize = false;
        Lbl_Sinonimo.Font = new Font("Segoe UI", 12F);
        Lbl_Sinonimo.ForeColor = Color.LightGray;
        Lbl_Sinonimo.Location = new Point(50, 6);
        Lbl_Sinonimo.MaximumSize = new Size(1800, 0);
        Lbl_Sinonimo.Name = "Lbl_Sinonimo";
        Lbl_Sinonimo.Location = new Point(50, 60);
        Lbl_Sinonimo.Size = new Size(950, 60);
        Lbl_Sinonimo.TabIndex = 1;
        Lbl_Sinonimo.TextAlign = ContentAlignment.MiddleLeft;
        Lbl_Sinonimo.Visible = false;
        // 
        // Lbl_TituloJapones
        // 
        Lbl_TituloJapones.AutoEllipsis = true;
        Lbl_TituloJapones.AutoSize = false;
        Lbl_TituloJapones.Font = new Font("Segoe UI", 11F);
        Lbl_TituloJapones.ForeColor = Color.LightSteelBlue;
        Lbl_TituloJapones.Location = new Point(50, 6);
        Lbl_TituloJapones.MaximumSize = new Size(1800, 0);
        Lbl_TituloJapones.Name = "Lbl_TituloJapones";
        Lbl_TituloJapones.Location = new Point(970, 60);
        Lbl_TituloJapones.Size = new Size(950, 50);
        Lbl_TituloJapones.TabIndex = 0;
        Lbl_TituloJapones.TextAlign = ContentAlignment.MiddleLeft;
        Lbl_TituloJapones.Visible = false;
        // 
        // Lbl_Carregando
        // 
        Lbl_Carregando.BackColor = SystemColors.Desktop;
        Lbl_Carregando.Dock = DockStyle.Fill;
        Lbl_Carregando.Font = new Font("Segoe UI", 14F);
        Lbl_Carregando.ForeColor = Color.Gold;
        Lbl_Carregando.Location = new Point(0, 175);
        Lbl_Carregando.Margin = new Padding(4, 0, 4, 0);
        Lbl_Carregando.Name = "Lbl_Carregando";
        Lbl_Carregando.Size = new Size(1920, 905);
        Lbl_Carregando.TabIndex = 0;
        Lbl_Carregando.Text = "⏳ Carregando detalhes do anime...";
        Lbl_Carregando.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // Pnl_Conteudo
        // 
        Pnl_Conteudo.Controls.Add(Pnl_Info);
        Pnl_Conteudo.Controls.Add(Pnl_Esquerda);
        Pnl_Conteudo.Dock = DockStyle.Fill;
        Pnl_Conteudo.Location = new Point(0, 200);
        Pnl_Conteudo.Margin = new Padding(4, 3, 4, 3);
        Pnl_Conteudo.Name = "Pnl_Conteudo";
        Pnl_Conteudo.Size = new Size(1920, 905);
        Pnl_Conteudo.TabIndex = 1;
        Pnl_Conteudo.Visible = false;
        // 
        // Pnl_Info
        // 
        Pnl_Info.AutoScroll = true;
        Pnl_Info.BackColor = Color.Silver;
        Pnl_Info.Dock = DockStyle.Fill;
        Pnl_Info.Location = new Point(600, 250);
        Pnl_Info.Margin = new Padding(4, 3, 4, 3);
        Pnl_Info.Name = "Pnl_Info";
        Pnl_Info.Padding = new Padding(5, 4, 5, 4);
        Pnl_Info.Size = new Size(1370, 905);
        Pnl_Info.TabIndex = 0;
        // 
        // Pnl_Esquerda
        // 
        Pnl_Esquerda.BackColor = Color.FromArgb(243, 244, 250);
        Pnl_Esquerda.Controls.Add(Pnl_Acoes);
        Pnl_Esquerda.Controls.Add(Pnl_Stats);
        Pnl_Esquerda.Controls.Add(Pbx_Capa);
        Pnl_Esquerda.Dock = DockStyle.Left;
        Pnl_Esquerda.Location = new Point(0, 0);
        Pnl_Esquerda.Margin = new Padding(50, 10, 20, 10);
        Pnl_Esquerda.Name = "Pnl_Esquerda";
        Pnl_Esquerda.Size = new Size(550, 905);
        Pnl_Esquerda.TabIndex = 1;
        // 
        // Pnl_Acoes
        // 
        Pnl_Acoes.Controls.Add(Btn_SalvarComoAnime);
        Pnl_Acoes.Controls.Add(Btn_SalvarComoMyAnime);
        Pnl_Acoes.Location = new Point(90, 810);
        Pnl_Acoes.Margin = new Padding(40, 30, 40, 30);
        Pnl_Acoes.Name = "Pnl_Acoes";
        Pnl_Acoes.Padding = new Padding(13, 6, 13, 6);
        Pnl_Acoes.Size = new Size(400, 200);
        Pnl_Acoes.TabIndex = 2;
        // 
        // Btn_SalvarComoAnime
        // 
        Btn_SalvarComoAnime.BackColor = Color.FromArgb(35, 40, 90);
        Btn_SalvarComoAnime.Dock = DockStyle.Top;
        Btn_SalvarComoAnime.FlatStyle = FlatStyle.Flat;
        Btn_SalvarComoAnime.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        Btn_SalvarComoAnime.ForeColor = Color.Gold;
        Btn_SalvarComoAnime.Location = new Point(13, 66);
        Btn_SalvarComoAnime.Margin = new Padding(4, 3, 4, 3);
        Btn_SalvarComoAnime.Name = "Btn_SalvarComoAnime";
        Btn_SalvarComoAnime.Size = new Size(350, 60);
        Btn_SalvarComoAnime.TabIndex = 1;
        Btn_SalvarComoAnime.Text = "Salvar Anime";
        Btn_SalvarComoAnime.UseVisualStyleBackColor = false;
        // 
        // Btn_SalvarComoMyAnime
        // 
        Btn_SalvarComoMyAnime.BackColor = Color.FromArgb(35, 40, 90);
        Btn_SalvarComoMyAnime.Dock = DockStyle.Top;
        Btn_SalvarComoMyAnime.FlatStyle = FlatStyle.Flat;
        Btn_SalvarComoMyAnime.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        Btn_SalvarComoMyAnime.ForeColor = Color.Gold;
        Btn_SalvarComoMyAnime.Location = new Point(13, 6);
        Btn_SalvarComoMyAnime.Margin = new Padding(4, 3, 4, 3);
        Btn_SalvarComoMyAnime.Name = "Btn_SalvarComoMyAnime";
        Btn_SalvarComoMyAnime.Size = new Size(350, 60);
        Btn_SalvarComoMyAnime.TabIndex = 0;
        Btn_SalvarComoMyAnime.Text = "Salvar Como MyAnime";
        Btn_SalvarComoMyAnime.UseVisualStyleBackColor = false;
        // 
        // Pnl_Stats
        // 
        Pnl_Stats.AutoSize = true;
        Pnl_Stats.Controls.Add(Lbl_EstatisticasRapidas);
        Pnl_Stats.Controls.Add(Lbl_Generos);
        Pnl_Stats.Controls.Add(Btn_ExibirMyAnime);
        Pnl_Stats.Controls.Add(Btn_EditarAnime);
        Pnl_Stats.Controls.Add(Lbl_Episodios);
        Pnl_Stats.Controls.Add(Lbl_TempoPorEpisodio);
        Pnl_Stats.Dock = DockStyle.Fill;
        Pnl_Stats.Location = new Point(0, 570);
        Pnl_Stats.Margin = new Padding(4, 3, 4, 3);
        Pnl_Stats.Name = "Pnl_Stats";
        Pnl_Stats.Padding = new Padding(13, 10, 8, 4);
        Pnl_Stats.Size = new Size(550, 329);
        Pnl_Stats.TabIndex = 0;
        // 
        // Lbl_EstatisticasRapidas
        // 
        Lbl_EstatisticasRapidas.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        Lbl_EstatisticasRapidas.ForeColor = Color.Gold;
        Lbl_EstatisticasRapidas.Location = new Point(70, 10);
        Lbl_EstatisticasRapidas.Margin = new Padding(4, 0, 4, 0);
        Lbl_EstatisticasRapidas.Name = "Lbl_EstatisticasRapidas";
        Lbl_EstatisticasRapidas.Size = new Size(500, 40);
        Lbl_EstatisticasRapidas.TabIndex = 0;
        Lbl_EstatisticasRapidas.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // Lbl_Generos
        // 
        Lbl_Generos.Font = new Font("Segoe UI", 9.5F);
        Lbl_Generos.ForeColor = Color.DarkOrange;
        Lbl_Generos.Location = new Point(70, 65);
        Lbl_Generos.Margin = new Padding(4, 0, 4, 0);
        Lbl_Generos.Name = "Lbl_Generos";
        Lbl_Generos.Size = new Size(500, 40);
        Lbl_Generos.TabIndex = 1;
        // 
        // Btn_ExibirMyAnime
        // 
        Btn_ExibirMyAnime.BackColor = Color.FromArgb(35, 40, 90);
        Btn_ExibirMyAnime.FlatStyle = FlatStyle.Flat;
        Btn_ExibirMyAnime.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        Btn_ExibirMyAnime.ForeColor = Color.Gold;
        Btn_ExibirMyAnime.Location = new Point(70, 115);
        Btn_ExibirMyAnime.Name = "Btn_ExibirMyAnime";
        Btn_ExibirMyAnime.Size = new Size(500, 45);
        Btn_ExibirMyAnime.TabIndex = 6;
        Btn_ExibirMyAnime.Text = "Exibir MyAnime";
        Btn_ExibirMyAnime.UseVisualStyleBackColor = false;
        Btn_ExibirMyAnime.Visible = false;
        // 
        // Btn_EditarAnime
        // 
        Btn_EditarAnime.BackColor = Color.FromArgb(35, 40, 90);
        Btn_EditarAnime.FlatStyle = FlatStyle.Flat;
        Btn_EditarAnime.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        Btn_EditarAnime.ForeColor = Color.Gold;
        Btn_EditarAnime.Location = new Point(70, 166);
        Btn_EditarAnime.Name = "Btn_EditarAnime";
        Btn_EditarAnime.Size = new Size(500, 45);
        Btn_EditarAnime.TabIndex = 7;
        Btn_EditarAnime.Text = "Editar Anime";
        Btn_EditarAnime.UseVisualStyleBackColor = false;
        Btn_EditarAnime.Visible = false;
        // 
        // Lbl_Episodios
        // 
        Lbl_Episodios.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        Lbl_Episodios.ForeColor = Color.FromArgb(35, 40, 90);
        Lbl_Episodios.Location = new Point(70, 75);
        Lbl_Episodios.Margin = new Padding(4, 0, 4, 0);
        Lbl_Episodios.Name = "Lbl_Episodios";
        Lbl_Episodios.Size = new Size(500, 50);
        Lbl_Episodios.TabIndex = 4;
        // 
        // Lbl_TempoPorEpisodio
        // 
        Lbl_TempoPorEpisodio.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        Lbl_TempoPorEpisodio.ForeColor = Color.FromArgb(35, 40, 90);
        Lbl_TempoPorEpisodio.Location = new Point(70, 120);
        Lbl_TempoPorEpisodio.Margin = new Padding(4, 0, 4, 0);
        Lbl_TempoPorEpisodio.Name = "Lbl_TempoPorEpisodio";
        Lbl_TempoPorEpisodio.Size = new Size(328, 50);
        Lbl_TempoPorEpisodio.TabIndex = 5;
        Lbl_TempoPorEpisodio.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // Pbx_Capa
        // 
        Pbx_Capa.BackColor = Color.FromArgb(50, 50, 60);
        Pbx_Capa.Dock = DockStyle.Top;
        Pbx_Capa.Location = new Point(0, 0);
        Pbx_Capa.Margin = new Padding(4, 3, 4, 3);
        Pbx_Capa.Name = "Pbx_Capa";
        Pbx_Capa.Size = new Size(550, 576);
        Pbx_Capa.SizeMode = PictureBoxSizeMode.Zoom;
        Pbx_Capa.TabIndex = 1;
        Pbx_Capa.TabStop = false;
        // 
        // Lbl_Rank
        // 
        Lbl_Rank.Location = new Point(0, 0);
        Lbl_Rank.Name = "Lbl_Rank";
        Lbl_Rank.Size = new Size(100, 23);
        Lbl_Rank.TabIndex = 0;
        // 
        // Lbl_Popularidade
        // 
        Lbl_Popularidade.Location = new Point(0, 0);
        Lbl_Popularidade.Name = "Lbl_Popularidade";
        Lbl_Popularidade.Size = new Size(100, 23);
        Lbl_Popularidade.TabIndex = 0;
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
        Size = new Size(1920, 1080);
        Pnl_Header.ResumeLayout(false);
        Pnl_Header.PerformLayout();
        Pnl_Conteudo.ResumeLayout(false);
        Pnl_Esquerda.ResumeLayout(false);
        Pnl_Esquerda.PerformLayout();
        Pnl_Acoes.ResumeLayout(false);
        Pnl_Stats.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)Pbx_Capa).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Panel Pnl_Header;
    private SelectableTextLabel Lbl_TituloAnime;
    private SelectableTextLabel Lbl_TituloIngles;
    private SelectableTextLabel Lbl_Sinonimo;
    private SelectableTextLabel Lbl_TituloJapones;
    private Label Lbl_Carregando;
    private Panel Pnl_Conteudo;
    private Panel Pnl_Esquerda;
    private Panel Pnl_Acoes;
    private Button Btn_SalvarComoAnime;
    private Button Btn_SalvarComoMyAnime;
    private Button Btn_ExibirMyAnime;
    private Button Btn_EditarAnime;
    private PictureBox Pbx_Capa;
    private Panel Pnl_Stats;
    private Label Lbl_EstatisticasRapidas;
    private Label Lbl_Generos;
    private Label Lbl_Rank;
    private Label Lbl_Popularidade;
    private Label Lbl_Episodios;
    private Label Lbl_TempoPorEpisodio;
    private Panel Pnl_Info;
}
