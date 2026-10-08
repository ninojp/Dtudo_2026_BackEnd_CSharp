using WinAppDtudo.Services;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Dtos.MyAnimeList;

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
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var width = proposedSize.Width > 0
            ? Math.Min(LogicalToDeviceUnits(200), proposedSize.Width)
            : LogicalToDeviceUnits(200);
        var imageWidth = Math.Max(1, Math.Min(LogicalToDeviceUnits(190), width - LogicalToDeviceUnits(2)));
        var imageHeight = (int)Math.Round(imageWidth * 250D / 190);
        return new Size(width, imageHeight + LogicalToDeviceUnits(100));
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Pbx_Capa is null || Lbl_MalId is null || Lbl_Nome is null)
            return;

        var margin = LogicalToDeviceUnits(1);
        var textWidth = Math.Max(1, ClientSize.Width - margin * 2);
        var imageWidth = Math.Min(LogicalToDeviceUnits(190), textWidth);
        Pbx_Capa.SetBounds((ClientSize.Width - imageWidth) / 2, LogicalToDeviceUnits(5),
            imageWidth, (int)Math.Round(imageWidth * 250D / 190));
        Lbl_MalId.SetBounds(margin, Pbx_Capa.Bottom + LogicalToDeviceUnits(5),
            textWidth, LogicalToDeviceUnits(25));
        Lbl_Nome.SetBounds(margin, Lbl_MalId.Bottom, textWidth,
            Math.Max(1, ClientSize.Height - Lbl_MalId.Bottom - LogicalToDeviceUnits(5)));
    }

    public void CarregarDadosLocal(ObterAnimeDto anime)
    {
        _malId = anime.MalId;
        Lbl_MalId.Text = $"DB #{anime.MalId}";
        Lbl_Nome.Text = !string.IsNullOrWhiteSpace(anime.Titulo)
            ? anime.Titulo
            : anime.Title ?? $"#{anime.MalId}";

        Pbx_Capa.Image?.Dispose();
        Pbx_Capa.Image = null;
        _ = CarregarImagemLocalAsync(anime.ImagensUrlMal?.FirstOrDefault());
    }

    // ===================================================================

    /// <summary>Preenche o mini card com os dados do anime relacionado e inicia o carregamento da imagem.</summary>
    public void CarregarDados(AnimeRelationEntry entry)
    {
        _malId = entry.MalId;
        Lbl_MalId.Text = $"ID: {entry.MalId}";
        Lbl_Nome.Text = entry.Name ?? $"#{entry.MalId}";
        //Lbl_Tipo.Text = entry.Type ?? "—";

        Pbx_Capa.Image?.Dispose();
        Pbx_Capa.Image = null;
        if (entry.MalId > 0)
            _ = CarregarImagemAsync(entry.ImageUrl, entry.MalId);
    }

    private async Task CarregarImagemLocalAsync(string? url)
    {
        var imagem = await ImageLoaderService.DownloadAsync(url);
        if (imagem is null || Pbx_Capa.IsDisposed)
        {
            imagem?.Dispose();
            return;
        }

        void AplicarImagem()
        {
            if (Pbx_Capa.IsDisposed)
            {
                imagem.Dispose();
                return;
            }

            var anterior = Pbx_Capa.Image;
            Pbx_Capa.Image = imagem;
            anterior?.Dispose();
        }

        if (Pbx_Capa.InvokeRequired)
            Pbx_Capa.BeginInvoke(AplicarImagem);
        else
            AplicarImagem();
    }

    // ===================================================================

    private async Task CarregarImagemAsync(string? url, int malId)
    {
        var imagem = await ImageLoaderService.DownloadAnimeCoverAsync(url, malId);
        if (imagem is null || Pbx_Capa.IsDisposed)
        {
            imagem?.Dispose();
            return;
        }

        void AplicarImagem()
        {
            if (Pbx_Capa.IsDisposed)
            {
                imagem.Dispose();
                return;
            }

            var anterior = Pbx_Capa.Image;
            Pbx_Capa.Image = imagem;
            anterior?.Dispose();
        }

        if (Pbx_Capa.InvokeRequired)
            Pbx_Capa.BeginInvoke(AplicarImagem);
        else
            AplicarImagem();
    }

    private void SubscreverCliques()
    {
        Click += DispararClique;
        foreach (Control ctrl in Controls)
            ctrl.Click += DispararClique;
    }

    private void DispararClique(object? sender, EventArgs e)
        => CardClicado?.Invoke(this, _malId);

    //protected override void OnMouseEnter(EventArgs e)
    //{
    //    base.OnMouseEnter(e);
    //    BackColor = Color.FromArgb(218, 232, 255);
    //}

    //protected override void OnMouseLeave(EventArgs e)
    //{
    //    base.OnMouseLeave(e);
    //    BackColor = Color.FromArgb(247, 248, 252);
    //}

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
            DarkModeColors.ActiveBorderColor, ButtonBorderStyle.Solid);
    }
}
