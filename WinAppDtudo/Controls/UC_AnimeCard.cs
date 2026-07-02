using WinAppDtudo.Services;

namespace WinAppDtudo.Controls;

/// <summary>
/// Card clicável que exibe o pôster, título, título em inglês, ano, tipo e pontuação de um anime.
/// Dispara o evento <see cref="CardClicado"/> com o MalId ao ser clicado.
/// </summary>
public partial class UC_AnimeCard : UserControl
{
    /// <summary>Disparado quando o usuário clica no card. O argumento é o MalId do anime.</summary>
    public event EventHandler<int>? CardClicado;

    private int _malId;

    public UC_AnimeCard()
    {
        InitializeComponent();
        SubscreverEventosDeClique();
    }

    // ===================================================================

    /// <summary>Preenche o card com os dados do anime e inicia o carregamento da imagem.</summary>
    public void CarregarDados(JikanAnimeCard anime)
    {
        _malId = anime.MalId;

        Lbl_Titulo.Text = anime.Title ?? $"Anime #{anime.MalId}";

        if (!string.IsNullOrWhiteSpace(anime.TitleEnglish) && anime.TitleEnglish != anime.Title)
        {
            Lbl_Ingles.Text = anime.TitleEnglish;
            Lbl_Ingles.Visible = true;
        }
        else
        {
            Lbl_Ingles.Visible = false;
        }

        Lbl_Info.Text = $"{anime.Year?.ToString() ?? "—"}  •  {anime.Type ?? "?"}";
        Lbl_Score.Text = anime.Score.HasValue ? $"⭐ {anime.Score:0.00}" : "⭐ —";

        Pbx_Capa.Image = null;
        if (!string.IsNullOrWhiteSpace(anime.ImageUrl))
            _ = CarregarImagemAsync(anime.ImageUrl);
    }

    // ===================================================================

    private async Task CarregarImagemAsync(string url)
    {
        var img = await ImageLoaderService.DownloadAsync(url);
        if (img == null || Pbx_Capa.IsDisposed) return;
        if (Pbx_Capa.InvokeRequired)
            Pbx_Capa.Invoke(() => Pbx_Capa.Image = img);
        else
            Pbx_Capa.Image = img;
    }

    private void SubscreverEventosDeClique()
    {
        Click += DispararCardClicado;
        foreach (Control ctrl in Controls)
            ctrl.Click += DispararCardClicado;
    }

    private void DispararCardClicado(object? sender, EventArgs e)
        => CardClicado?.Invoke(this, _malId);

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        BackColor = Color.FromArgb(218, 232, 255);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        BackColor = SystemColors.Control;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
            Color.FromArgb(180, 200, 230), ButtonBorderStyle.Solid);
    }
}
