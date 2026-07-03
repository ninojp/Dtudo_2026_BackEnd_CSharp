using WinAppDtudo.Services;

namespace WinAppDtudo.Controls;

/// <summary>
/// Mini card clicável que exibe imagem, MAL ID, título e tipo de um anime relacionado.
/// Dispara o evento <see cref="CardClicado"/> com o MalId ao ser clicado.
/// </summary>
public partial class UC_MiniAnimeCard : UserControl
{
    /// <summary>Disparado quando o usuário clica no mini card. O argumento é o MalId do anime.</summary>
    public event EventHandler<int>? CardClicado;

    private int _malId;

    public UC_MiniAnimeCard()
    {
        InitializeComponent();
        SubscreverCliques();
    }

    // ===================================================================

    /// <summary>Preenche o mini card com os dados do anime relacionado e inicia o carregamento da imagem.</summary>
    public void CarregarDados(JikanRelacaoEntry entry)
    {
        _malId = entry.MalId;
        Lbl_MalId.Text = $"ID: {entry.MalId}";
        Lbl_Nome.Text = entry.Name ?? $"#{entry.MalId}";
        Lbl_Tipo.Text = entry.Type ?? "—";

        Pbx_Capa.Image = null;
        if (!string.IsNullOrWhiteSpace(entry.ImageUrl))
            _ = ImageLoaderService.CarregarEmPictureBoxAsync(Pbx_Capa, entry.ImageUrl);
    }

    // ===================================================================

    private void SubscreverCliques()
    {
        Click += DispararClique;
        foreach (Control ctrl in Controls)
            ctrl.Click += DispararClique;
    }

    private void DispararClique(object? sender, EventArgs e)
        => CardClicado?.Invoke(this, _malId);

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        BackColor = Color.FromArgb(218, 232, 255);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        BackColor = Color.FromArgb(247, 248, 252);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
            Color.FromArgb(180, 200, 230), ButtonBorderStyle.Solid);
    }
}
