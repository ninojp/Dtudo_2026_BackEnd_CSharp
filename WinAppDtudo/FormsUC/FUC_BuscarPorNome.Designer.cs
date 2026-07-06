namespace WinAppDtudo.FormsUC;

partial class FUC_BuscarPorNome
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
        Tlp_Main = new TableLayoutPanel();
        Pnl_Topo = new Panel();
        Lbl_Titulo = new Label();
        Lbl_InputLabel = new Label();
        Txb_InputBuscarPorNome = new TextBox();
        Btn_BuscarPorNome = new Button();
        Lbl_Status = new Label();
        Flp_Cards = new FlowLayoutPanel();
        Pnl_Paginacao = new Panel();
        Tlp_Paginacao = new TableLayoutPanel();
        Btn_PaginaAnterior = new Button();
        Lbl_Pagina = new Label();
        Btn_ProximaPagina = new Button();
        Tlp_Main.SuspendLayout();
        Pnl_Topo.SuspendLayout();
        Pnl_Paginacao.SuspendLayout();
        Tlp_Paginacao.SuspendLayout();
        SuspendLayout();
        // 
        // Tlp_Main
        // 
        Tlp_Main.BackColor = Color.Gold;
        Tlp_Main.BackgroundImageLayout = ImageLayout.Stretch;
        Tlp_Main.ColumnCount = 1;
        Tlp_Main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        Tlp_Main.Controls.Add(Pnl_Topo, 0, 0);
        Tlp_Main.Controls.Add(Flp_Cards, 0, 1);
        Tlp_Main.Controls.Add(Pnl_Paginacao, 0, 2);
        Tlp_Main.Dock = DockStyle.Fill;
        Tlp_Main.ForeColor = Color.Gold;
        Tlp_Main.Location = new Point(0, 0);
        Tlp_Main.Margin = new Padding(4, 3, 4, 3);
        Tlp_Main.Name = "Tlp_Main";
        Tlp_Main.RowCount = 3;
        Tlp_Main.RowStyles.Add(new RowStyle(SizeType.Absolute, 211F));
        Tlp_Main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Tlp_Main.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        Tlp_Main.Size = new Size(1920, 1027);
        Tlp_Main.TabIndex = 0;
        // 
        // Pnl_Topo
        // 
        Pnl_Topo.AutoSize = true;
        Pnl_Topo.BackColor = Color.Black;
        Pnl_Topo.Controls.Add(Lbl_Titulo);
        Pnl_Topo.Controls.Add(Lbl_InputLabel);
        Pnl_Topo.Controls.Add(Txb_InputBuscarPorNome);
        Pnl_Topo.Controls.Add(Btn_BuscarPorNome);
        Pnl_Topo.Controls.Add(Lbl_Status);
        Pnl_Topo.Dock = DockStyle.Fill;
        Pnl_Topo.Font = new Font("Microsoft Sans Serif", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Pnl_Topo.ForeColor = Color.Gold;
        Pnl_Topo.Location = new Point(4, 3);
        Pnl_Topo.Margin = new Padding(4, 3, 4, 3);
        Pnl_Topo.Name = "Pnl_Topo";
        Pnl_Topo.Size = new Size(1912, 205);
        Pnl_Topo.TabIndex = 0;
        // 
        // Lbl_Titulo
        // 
        Lbl_Titulo.Anchor = AnchorStyles.Top;
        Lbl_Titulo.AutoSize = true;
        Lbl_Titulo.BackColor = Color.Black;
        Lbl_Titulo.Font = new Font("Segoe UI Black", 12F, FontStyle.Bold);
        Lbl_Titulo.ForeColor = Color.Gold;
        Lbl_Titulo.Location = new Point(1243, 34);
        Lbl_Titulo.Margin = new Padding(4, 0, 4, 0);
        Lbl_Titulo.Name = "Lbl_Titulo";
        Lbl_Titulo.Size = new Size(504, 45);
        Lbl_Titulo.TabIndex = 0;
        Lbl_Titulo.Text = "🔍 Procurar Animes por Nome";
        // 
        // Lbl_InputLabel
        // 
        Lbl_InputLabel.Anchor = AnchorStyles.Top;
        Lbl_InputLabel.AutoSize = true;
        Lbl_InputLabel.BackColor = Color.Black;
        Lbl_InputLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        Lbl_InputLabel.ForeColor = Color.Gold;
        Lbl_InputLabel.Location = new Point(695, 37);
        Lbl_InputLabel.Margin = new Padding(4, 0, 4, 0);
        Lbl_InputLabel.Name = "Lbl_InputLabel";
        Lbl_InputLabel.Size = new Size(392, 45);
        Lbl_InputLabel.TabIndex = 1;
        Lbl_InputLabel.Text = "Digite o nome do anime:";
        // 
        // Txb_InputBuscarPorNome
        // 
        Txb_InputBuscarPorNome.Anchor = AnchorStyles.Top;
        Txb_InputBuscarPorNome.BackColor = SystemColors.ControlDarkDark;
        Txb_InputBuscarPorNome.Font = new Font("Segoe UI", 10F);
        Txb_InputBuscarPorNome.ForeColor = Color.Gold;
        Txb_InputBuscarPorNome.Location = new Point(639, 85);
        Txb_InputBuscarPorNome.Margin = new Padding(4, 3, 4, 3);
        Txb_InputBuscarPorNome.Multiline = true;
        Txb_InputBuscarPorNome.Name = "Txb_InputBuscarPorNome";
        Txb_InputBuscarPorNome.Size = new Size(504, 63);
        Txb_InputBuscarPorNome.TabIndex = 0;
        // 
        // Btn_BuscarPorNome
        // 
        Btn_BuscarPorNome.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Btn_BuscarPorNome.BackColor = Color.DimGray;
        Btn_BuscarPorNome.FlatStyle = FlatStyle.Standard;
        Btn_BuscarPorNome.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        Btn_BuscarPorNome.ForeColor = Color.White;
        Btn_BuscarPorNome.Location = new Point(1406, 85);
        Btn_BuscarPorNome.Margin = new Padding(4, 3, 4, 3);
        Btn_BuscarPorNome.Name = "Btn_BuscarPorNome";
        Btn_BuscarPorNome.Size = new Size(213, 63);
        Btn_BuscarPorNome.TabIndex = 1;
        Btn_BuscarPorNome.Text = "🔍 Buscar";
        Btn_BuscarPorNome.UseVisualStyleBackColor = false;
        // 
        // Lbl_Status
        // 
        Lbl_Status.AutoSize = true;
        Lbl_Status.Font = new Font("Segoe UI", 8.5F);
        Lbl_Status.ForeColor = Color.DimGray;
        Lbl_Status.Location = new Point(731, 60);
        Lbl_Status.Margin = new Padding(4, 0, 4, 0);
        Lbl_Status.Name = "Lbl_Status";
        Lbl_Status.Size = new Size(0, 31);
        Lbl_Status.TabIndex = 2;
        // 
        // Flp_Cards
        // 
        Flp_Cards.AutoScroll = true;
        Flp_Cards.BackColor = Color.Black;
        Flp_Cards.Dock = DockStyle.Fill;
        Flp_Cards.Location = new Point(4, 214);
        Flp_Cards.Margin = new Padding(4, 3, 4, 3);
        Flp_Cards.Name = "Flp_Cards";
        Flp_Cards.Padding = new Padding(8, 6, 8, 6);
        Flp_Cards.Size = new Size(1912, 754);
        Flp_Cards.TabIndex = 1;
        // 
        // Pnl_Paginacao
        // 
        Pnl_Paginacao.BackColor = Color.Black;
        Pnl_Paginacao.Controls.Add(Tlp_Paginacao);
        Pnl_Paginacao.Dock = DockStyle.Fill;
        Pnl_Paginacao.Location = new Point(4, 974);
        Pnl_Paginacao.Margin = new Padding(4, 3, 4, 3);
        Pnl_Paginacao.Name = "Pnl_Paginacao";
        Pnl_Paginacao.Size = new Size(1912, 50);
        Pnl_Paginacao.TabIndex = 2;
        // 
        // Tlp_Paginacao
        // 
        Tlp_Paginacao.ColumnCount = 3;
        Tlp_Paginacao.ColumnStyles.Add(new ColumnStyle());
        Tlp_Paginacao.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        Tlp_Paginacao.ColumnStyles.Add(new ColumnStyle());
        Tlp_Paginacao.Controls.Add(Btn_PaginaAnterior, 0, 0);
        Tlp_Paginacao.Controls.Add(Lbl_Pagina, 1, 0);
        Tlp_Paginacao.Controls.Add(Btn_ProximaPagina, 2, 0);
        Tlp_Paginacao.Dock = DockStyle.Fill;
        Tlp_Paginacao.Location = new Point(0, 0);
        Tlp_Paginacao.Margin = new Padding(4, 3, 4, 3);
        Tlp_Paginacao.Name = "Tlp_Paginacao";
        Tlp_Paginacao.RowCount = 1;
        Tlp_Paginacao.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Tlp_Paginacao.Size = new Size(1912, 50);
        Tlp_Paginacao.TabIndex = 0;
        // 
        // Btn_PaginaAnterior
        // 
        Btn_PaginaAnterior.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        Btn_PaginaAnterior.BackColor = Color.DimGray;
        Btn_PaginaAnterior.Enabled = false;
        Btn_PaginaAnterior.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        Btn_PaginaAnterior.Location = new Point(10, 8);
        Btn_PaginaAnterior.Margin = new Padding(10, 8, 5, 4);
        Btn_PaginaAnterior.Name = "Btn_PaginaAnterior";
        Btn_PaginaAnterior.Size = new Size(169, 38);
        Btn_PaginaAnterior.TabIndex = 0;
        Btn_PaginaAnterior.Text = "◄  Anterior";
        Btn_PaginaAnterior.UseVisualStyleBackColor = false;
        // 
        // Lbl_Pagina
        // 
        Lbl_Pagina.BackColor = Color.Transparent;
        Lbl_Pagina.Dock = DockStyle.Fill;
        Lbl_Pagina.Font = new Font("Segoe UI", 9F);
        Lbl_Pagina.Location = new Point(188, 0);
        Lbl_Pagina.Margin = new Padding(4, 0, 4, 0);
        Lbl_Pagina.Name = "Lbl_Pagina";
        Lbl_Pagina.Size = new Size(1536, 50);
        Lbl_Pagina.TabIndex = 1;
        Lbl_Pagina.Text = "—";
        Lbl_Pagina.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // Btn_ProximaPagina
        // 
        Btn_ProximaPagina.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        Btn_ProximaPagina.BackColor = Color.DimGray;
        Btn_ProximaPagina.Enabled = false;
        Btn_ProximaPagina.FlatStyle = FlatStyle.Flat;
        Btn_ProximaPagina.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        Btn_ProximaPagina.Location = new Point(1733, 8);
        Btn_ProximaPagina.Margin = new Padding(5, 8, 10, 4);
        Btn_ProximaPagina.Name = "Btn_ProximaPagina";
        Btn_ProximaPagina.Size = new Size(169, 38);
        Btn_ProximaPagina.TabIndex = 1;
        Btn_ProximaPagina.Text = "Próxima  ►";
        Btn_ProximaPagina.UseVisualStyleBackColor = false;
        // 
        // FUC_BuscarPorNome
        // 
        AutoScaleDimensions = new SizeF(13F, 26F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.Black;
        BackgroundImage = Properties.Resources.AnimesElas;
        Controls.Add(Tlp_Main);
        Margin = new Padding(4, 3, 4, 3);
        Name = "FUC_BuscarPorNome";
        Size = new Size(1920, 1027);
        Tlp_Main.ResumeLayout(false);
        Tlp_Main.PerformLayout();
        Pnl_Topo.ResumeLayout(false);
        Pnl_Topo.PerformLayout();
        Pnl_Paginacao.ResumeLayout(false);
        Tlp_Paginacao.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel Tlp_Main;
    private Panel Pnl_Topo;
    private Label Lbl_Titulo;
    private Label Lbl_InputLabel;
    private TextBox Txb_InputBuscarPorNome;
    private Button Btn_BuscarPorNome;
    private Label Lbl_Status;
    private FlowLayoutPanel Flp_Cards;
    private Panel Pnl_Paginacao;
    private TableLayoutPanel Tlp_Paginacao;
    private Button Btn_PaginaAnterior;
    private Label Lbl_Pagina;
    private Button Btn_ProximaPagina;
}
