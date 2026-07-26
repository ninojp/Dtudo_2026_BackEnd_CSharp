using WinAppDtudo.Services;
using LibDtudo.Shared.Dtos.MyAnimeList;

namespace WinAppDtudo.Controls;

/// <summary>
/// Card clicável que exibe o pôster, título, subtítulo, ano, tipo e pontuação de um anime.
/// Dispara o evento <see cref="CardClicado"/> com o MalId ao ser clicado.
/// </summary>
public partial class UC_AnimeCard : UserControl
{
    /// <summary>Disparado quando o usuário clica no card. O argumento é o MalId do anime.</summary>
    public event EventHandler<int>? CardClicado;

    private int _malId;
    private int _versaoDaCapa;

    public UC_AnimeCard()
    {
        InitializeComponent();
        SubscreverEventosDeClique();
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    // ===================================================================

    /// <summary>Preenche o card com os dados do anime e inicia o carregamento da imagem.</summary>
    public void CarregarDados(AnimeSearchCard anime, bool usarFallbackMyAnimeList = true, int? malIdParaImagem = null)
    {
        _malId = anime.MalId;

        Lbl_Titulo.Text = anime.Title ?? $"Anime #{anime.MalId}";

        var subtitulo = ObterSubtitulo(anime);
        if (!string.IsNullOrWhiteSpace(subtitulo))
        {
            Lbl_Ingles.Text = subtitulo;
            Lbl_Ingles.Visible = true;
        }
        else
        {
            Lbl_Ingles.Text = string.Empty;
            Lbl_Ingles.Visible = false;
        }

        Lbl_Info.Text = $"📅 {anime.Year?.ToString() ?? "—"}   🎞 {anime.Type ?? "?"}   ⭐ {anime.Score?.ToString("0.00") ?? "—"}";

        SubstituirCapa(CriarCapaPadrao(anime.Title, anime.MalId));
        var versaoDaCapa = ++_versaoDaCapa;
        _ = CarregarImagemAsync(anime.ImageUrl, malIdParaImagem ?? anime.MalId, versaoDaCapa, usarFallbackMyAnimeList);
    }

    private static string? ObterSubtitulo(AnimeSearchCard anime)
    {
        if (!string.IsNullOrWhiteSpace(anime.TitleEnglish) && anime.TitleEnglish != anime.Title)
            return anime.TitleEnglish;

        return anime.TitleSynonyms.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s) && s != anime.Title)
            ?? anime.TitleJapanese;
    }

    // ===================================================================

    private async Task CarregarImagemAsync(string? url, int malId, int versaoDaCapa, bool usarFallbackMyAnimeList)
    {
        var img = usarFallbackMyAnimeList
            ? await ImageLoaderService.DownloadAnimeCoverAsync(url, malId)
            : await ImageLoaderService.DownloadAsync(url);
        if (img == null || Pbx_Capa.IsDisposed || versaoDaCapa != _versaoDaCapa)
        {
            img?.Dispose();
            return;
        }

        if (Pbx_Capa.InvokeRequired)
            Pbx_Capa.Invoke(() => SubstituirCapa(img));
        else
            SubstituirCapa(img);
    }

    private void SubstituirCapa(Image image)
    {
        var imagemAnterior = Pbx_Capa.Image;
        Pbx_Capa.Image = image;
        imagemAnterior?.Dispose();
    }

    private Image CriarCapaPadrao(string? titulo, int malId)
    {
        var bitmap = new Bitmap(Pbx_Capa.Width, Pbx_Capa.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(45, 45, 55));
        using var brush = new SolidBrush(Color.Gold);
        using var fonteTitulo = new Font("Segoe UI", 12F, FontStyle.Bold);
        using var fonteRodape = new Font("Segoe UI", 8F, FontStyle.Regular);
        var areaTitulo = new Rectangle(16, 70, bitmap.Width - 32, bitmap.Height - 130);
        TextRenderer.DrawText(graphics, titulo ?? $"Anime #{malId}", fonteTitulo, areaTitulo, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(graphics, "Capa indisponível", fonteRodape,
            new Rectangle(8, bitmap.Height - 46, bitmap.Width - 16, 22), Color.Gold,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        return bitmap;
    }

    private void SubscreverEventosDeClique()
    {
        Click += DispararCardClicado;
        foreach (Control ctrl in Controls)
            ctrl.Click += DispararCardClicado;
    }

    private void DispararCardClicado(object? sender, EventArgs e)
        => CardClicado?.Invoke(this, _malId);

    //protected override void OnMouseEnter(EventArgs e)
    //{
    //    base.OnMouseEnter(e);
    //    BackColor = Color.FromArgb(218, 232, 255);
    //}

    //protected override void OnMouseLeave(EventArgs e)
    //{
    //    base.OnMouseLeave(e);
    //    BackColor = SystemColors.Control;
    //}

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
            Color.FromArgb(180, 200, 230), ButtonBorderStyle.Solid);
    }
}
