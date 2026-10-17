using System.ComponentModel;
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

    // Capas do MyAnimeList têm 225 x 318 px; as medidas lógicas (96 DPI) abaixo são escaladas pelo DPI atual.
    private const double ProporcaoPoster = 318D / 225;
    private const int PosterPadding = 6;
    private const int PosterEspacoId = 4;
    private const int PosterAlturaId = 20;

    private int _malId;
    private bool _formatoPoster;

    public UC_MiniAnimeCard()
    {
        InitializeComponent();
        SubscreverCliques();
        ThemeManager.ApplyDarkModeToUserControl(this);
    }

    /// <summary>
    /// Quando verdadeiro, o card usa a moldura proporcional: imagem em formato de pôster seguida do ID e do
    /// título (até duas linhas), com a largura definida por quem organiza os cards (veja <see cref="CalcularGrade"/>).
    /// Falso mantém o formato legado, de largura fixa de 200 px.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool FormatoPoster
    {
        get => _formatoPoster;
        set
        {
            if (_formatoPoster == value)
                return;

            _formatoPoster = value;
            OrganizarConteudo();
            Invalidate();
        }
    }

    /// <summary>
    /// Calcula as colunas e a largura de cada card para preencher a largura disponível: usa o menor número
    /// de colunas em que os cards não passam de <paramref name="larguraMaxima"/>, limitado ao máximo de colunas
    /// que ainda respeita <paramref name="larguraMinima"/>. Cada card ocupa sua largura mais <paramref name="espaco"/>
    /// (as margens somadas), de modo que a linha nunca ultrapassa <paramref name="larguraDisponivel"/>.
    /// </summary>
    public static (int Colunas, int LarguraCard) CalcularGrade(
        int larguraDisponivel, int espaco, int larguraMinima, int larguraMaxima)
    {
        larguraDisponivel = Math.Max(1, larguraDisponivel);
        var colunasParaMaximo = (int)Math.Ceiling((double)larguraDisponivel / (larguraMaxima + espaco));
        var colunasParaMinimo = Math.Max(1, larguraDisponivel / (larguraMinima + espaco));
        var colunas = Math.Max(1, Math.Min(colunasParaMaximo, colunasParaMinimo));
        var largura = Math.Min(larguraMaxima, larguraDisponivel / colunas - espaco);
        return (colunas, Math.Max(1, largura));
    }

    /// <summary>Tamanho do card para a largura informada: no formato pôster a altura acompanha a proporção da capa.</summary>
    public Size CalcularTamanhoParaLargura(int larguraCard)
    {
        if (!_formatoPoster)
            return GetPreferredSize(new Size(larguraCard, 0));

        var padding = LogicalToDeviceUnits(PosterPadding);
        var larguraImagem = Math.Max(1, larguraCard - padding * 2);
        var alturaImagem = (int)Math.Round(larguraImagem * ProporcaoPoster);
        var alturaLinha = TextRenderer.MeasureText("Ag", Lbl_Nome.Font, Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Height;
        var alturaNome = alturaLinha * 2 + LogicalToDeviceUnits(4);
        return new Size(larguraCard, padding + alturaImagem + LogicalToDeviceUnits(PosterEspacoId)
            + LogicalToDeviceUnits(PosterAlturaId) + alturaNome + padding);
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        if (_formatoPoster)
        {
            var larguraMaxima = LogicalToDeviceUnits(200);
            return CalcularTamanhoParaLargura(proposedSize.Width > 0
                ? Math.Min(larguraMaxima, proposedSize.Width)
                : larguraMaxima);
        }

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
        OrganizarConteudo();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        OrganizarConteudo();
    }

    // O conteúdo é posicionado aqui, em pixels do DPI atual; por isso o card não usa o autoescalonamento
    // do WinForms, que escalaria os filhos uma segunda vez (imagem maior que a moldura em DPI alto).
    private void OrganizarConteudo()
    {
        if (Pbx_Capa is null || Lbl_MalId is null || Lbl_Nome is null)
            return;

        if (_formatoPoster)
        {
            var padding = LogicalToDeviceUnits(PosterPadding);
            var larguraConteudo = Math.Max(1, ClientSize.Width - padding * 2);
            Pbx_Capa.SetBounds(padding, padding, larguraConteudo, (int)Math.Round(larguraConteudo * ProporcaoPoster));
            Lbl_MalId.SetBounds(padding, Pbx_Capa.Bottom + LogicalToDeviceUnits(PosterEspacoId),
                larguraConteudo, LogicalToDeviceUnits(PosterAlturaId));
            Lbl_Nome.SetBounds(padding, Lbl_MalId.Bottom, larguraConteudo,
                Math.Max(1, ClientSize.Height - Lbl_MalId.Bottom - padding));
            return;
        }

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
        if (_formatoPoster)
        {
            // Reserva visível da área da capa: aparece enquanto a imagem carrega e nas sobras de capas não verticais.
            using var reserva = new SolidBrush(DarkModeColors.ActiveTabBackgroundColor);
            e.Graphics.FillRectangle(reserva, Pbx_Capa.Bounds);
        }

        ControlPaint.DrawBorder(e.Graphics, ClientRectangle,
            DarkModeColors.ActiveBorderColor, ButtonBorderStyle.Solid);
    }
}
