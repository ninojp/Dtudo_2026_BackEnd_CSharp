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
            Color.FromArgb(180, 200, 230), ButtonBorderStyle.Solid);
    }
}
