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
        Tlp_Main.ColumnCount = 1;
        Tlp_Main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        Tlp_Main.RowCount = 3;
        Tlp_Main.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
        Tlp_Main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Tlp_Main.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        Tlp_Main.Controls.Add(Pnl_Topo, 0, 0);
        Tlp_Main.Controls.Add(Flp_Cards, 0, 1);
        Tlp_Main.Controls.Add(Pnl_Paginacao, 0, 2);
        Tlp_Main.Dock = DockStyle.Fill;
        Tlp_Main.Name = "Tlp_Main";
        //
        // Pnl_Topo
        //
        Pnl_Topo.Controls.Add(Lbl_Titulo);
        Pnl_Topo.Controls.Add(Lbl_InputLabel);
        Pnl_Topo.Controls.Add(Txb_InputBuscarPorNome);
        Pnl_Topo.Controls.Add(Btn_BuscarPorNome);
        Pnl_Topo.Controls.Add(Lbl_Status);
        Pnl_Topo.BackColor = Color.FromArgb(238, 240, 248);
        Pnl_Topo.Dock = DockStyle.Fill;
        Pnl_Topo.Name = "Pnl_Topo";
        //
        // Lbl_Titulo
        //
        Lbl_Titulo.AutoSize = true;
        Lbl_Titulo.Font = new Font("Segoe UI Black", 12F, FontStyle.Bold);
        Lbl_Titulo.ForeColor = Color.FromArgb(30, 30, 110);
        Lbl_Titulo.Location = new Point(12, 8);
        Lbl_Titulo.Name = "Lbl_Titulo";
        Lbl_Titulo.Text = "🔍 Procurar Animes por Nome";
        //
        // Lbl_InputLabel
        //
        Lbl_InputLabel.AutoSize = true;
        Lbl_InputLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        Lbl_InputLabel.Location = new Point(12, 56);
        Lbl_InputLabel.Name = "Lbl_InputLabel";
        Lbl_InputLabel.Text = "Nome do anime:";
        //
        // Txb_InputBuscarPorNome
        //
        Txb_InputBuscarPorNome.Font = new Font("Segoe UI", 10F);
        Txb_InputBuscarPorNome.Location = new Point(120, 52);
        Txb_InputBuscarPorNome.Name = "Txb_InputBuscarPorNome";
        Txb_InputBuscarPorNome.Size = new Size(300, 30);
        Txb_InputBuscarPorNome.TabIndex = 0;
        //
        // Btn_BuscarPorNome
        //
        Btn_BuscarPorNome.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        Btn_BuscarPorNome.Location = new Point(430, 50);
        Btn_BuscarPorNome.Name = "Btn_BuscarPorNome";
        Btn_BuscarPorNome.Size = new Size(120, 34);
        Btn_BuscarPorNome.TabIndex = 1;
        Btn_BuscarPorNome.Text = "🔍 Buscar";
        Btn_BuscarPorNome.UseVisualStyleBackColor = true;
        //
        // Lbl_Status
        //
        Lbl_Status.AutoSize = true;
        Lbl_Status.Font = new Font("Segoe UI", 8.5F);
        Lbl_Status.ForeColor = Color.DimGray;
        Lbl_Status.Location = new Point(562, 58);
        Lbl_Status.Name = "Lbl_Status";
        Lbl_Status.Text = "";
        //
        // Flp_Cards
        //
        Flp_Cards.AutoScroll = true;
        Flp_Cards.BackColor = Color.WhiteSmoke;
        Flp_Cards.Dock = DockStyle.Fill;
        Flp_Cards.FlowDirection = FlowDirection.LeftToRight;
        Flp_Cards.Name = "Flp_Cards";
        Flp_Cards.Padding = new Padding(6);
        Flp_Cards.WrapContents = true;
        //
        // Pnl_Paginacao
        //
        Pnl_Paginacao.Controls.Add(Tlp_Paginacao);
        Pnl_Paginacao.BackColor = Color.FromArgb(238, 240, 248);
        Pnl_Paginacao.Dock = DockStyle.Fill;
        Pnl_Paginacao.Name = "Pnl_Paginacao";
        //
        // Tlp_Paginacao
        //
        Tlp_Paginacao.ColumnCount = 3;
        Tlp_Paginacao.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Tlp_Paginacao.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        Tlp_Paginacao.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Tlp_Paginacao.RowCount = 1;
        Tlp_Paginacao.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Tlp_Paginacao.Controls.Add(Btn_PaginaAnterior, 0, 0);
        Tlp_Paginacao.Controls.Add(Lbl_Pagina, 1, 0);
        Tlp_Paginacao.Controls.Add(Btn_ProximaPagina, 2, 0);
        Tlp_Paginacao.Dock = DockStyle.Fill;
        Tlp_Paginacao.Name = "Tlp_Paginacao";
        //
        // Btn_PaginaAnterior
        //
        Btn_PaginaAnterior.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
        Btn_PaginaAnterior.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        Btn_PaginaAnterior.Margin = new Padding(8, 8, 4, 4);
        Btn_PaginaAnterior.Name = "Btn_PaginaAnterior";
        Btn_PaginaAnterior.Size = new Size(130, 34);
        Btn_PaginaAnterior.TabIndex = 0;
        Btn_PaginaAnterior.Text = "◄  Anterior";
        Btn_PaginaAnterior.UseVisualStyleBackColor = true;
        Btn_PaginaAnterior.Enabled = false;
        //
        // Lbl_Pagina
        //
        Lbl_Pagina.AutoSize = false;
        Lbl_Pagina.Dock = DockStyle.Fill;
        Lbl_Pagina.Font = new Font("Segoe UI", 9F);
        Lbl_Pagina.Name = "Lbl_Pagina";
        Lbl_Pagina.Text = "—";
        Lbl_Pagina.TextAlign = ContentAlignment.MiddleCenter;
        //
        // Btn_ProximaPagina
        //
        Btn_ProximaPagina.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
        Btn_ProximaPagina.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        Btn_ProximaPagina.Margin = new Padding(4, 8, 8, 4);
        Btn_ProximaPagina.Name = "Btn_ProximaPagina";
        Btn_ProximaPagina.Size = new Size(130, 34);
        Btn_ProximaPagina.TabIndex = 1;
        Btn_ProximaPagina.Text = "Próxima  ►";
        Btn_ProximaPagina.UseVisualStyleBackColor = true;
        Btn_ProximaPagina.Enabled = false;
        //
        // FUC_BuscarPorNome
        //
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(Tlp_Main);
        Name = "FUC_BuscarPorNome";
        Size = new Size(960, 530);
        Tlp_Main.ResumeLayout(false);
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
