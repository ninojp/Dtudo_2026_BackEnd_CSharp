using System.ComponentModel;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public partial class FUC_DetalhesAnime
{
    // Medidas lógicas (96 DPI) compartilhadas pelas duas fontes.
    private const int LarguraConteudoColuna = 240;
    private const int AlturaMaximaCapa = 340;
    private const int LarguraColunaEsquerda = 300;
    private const float TamanhoIconeInformacao = 9F;

    private bool _layoutPronto;
    private bool _organizandoPagina;
    private bool _carregando = true;
    private bool _carregamentoIniciado;
    private bool _capaIndisponivel;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor => DarkModeColors.GetAnimeDetailAccentColor(_consultaLocal);

    /// <summary>
    /// Cor dos elementos fixos (legendas, títulos de seção e separadores). Os dados e seus ícones usam
    /// a cor de destaque da fonte selecionada.
    /// </summary>
    private Color CorDosElementosFixos => DarkModeColors.TextColor;

    private readonly record struct ColunasDaPagina(
        int PaddingHorizontal, int Espaco, bool LadoALado, int LarguraEsquerda, int LarguraDireita)
    {
        public int XDireita => PaddingHorizontal + (LadoALado ? LarguraEsquerda + Espaco : 0);
    }

    private void ConfigurarLayoutDaPagina()
    {
        AutoScroll = true;
        AutoScrollMargin = Size.Empty;
        Pnl_Header.Dock = DockStyle.None;
        Pnl_Conteudo.Dock = DockStyle.None;
        Pnl_Esquerda.Dock = DockStyle.None;
        Pnl_Info.Dock = DockStyle.None;
        Pnl_Info.AutoScroll = false;
        Pnl_Info.Padding = Padding.Empty;
        Lbl_Carregando.Dock = DockStyle.None;

        foreach (var titulo in new[] { Lbl_TituloAnime, Lbl_TituloIngles, Lbl_Sinonimo, Lbl_TituloJapones })
        {
            titulo.MaximumSize = Size.Empty;
            titulo.UseMnemonic = false;
        }

        Pbx_Capa.Dock = DockStyle.None;
        Pbx_Capa.MinimumSize = Size.Empty;
        Pnl_Stats.Dock = DockStyle.None;
        Pnl_Stats.AutoSize = false;
        Pnl_Stats.AutoScroll = false;
        if (!_consultaLocal)
            Pnl_Stats.Controls.AddRange([Btn_SalvarComoMyAnime, Btn_SalvarComoAnime]);

        Pnl_Acoes.Dock = DockStyle.None;
        Pnl_Acoes.AutoSize = false;

        foreach (var button in new[] { Btn_SalvarComoMyAnime, Btn_SalvarComoAnime, Btn_ExibirMyAnime, Btn_EditarAnime })
        {
            button.Dock = DockStyle.None;
            button.AutoSize = false;
            button.MinimumSize = Size.Empty;
            button.Padding = new Padding(LogicalToDeviceUnits(12), LogicalToDeviceUnits(8),
                LogicalToDeviceUnits(12), LogicalToDeviceUnits(8));
        }

        _layoutPronto = true;
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        if (!_layoutPronto)
        {
            base.OnLayout(e);
            return;
        }
        if (_organizandoPagina)
            return;

        _organizandoPagina = true;
        try
        {
            // First measure without a scrollbar so a previously narrow layout cannot keep it alive.
            var fullWidth = Math.Max(1, ClientSize.Width +
                (VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
            var contentHeight = OrganizarPagina(fullWidth);
            if (contentHeight > ClientSize.Height)
                contentHeight = OrganizarPagina(Math.Max(1, fullWidth - SystemInformation.VerticalScrollBarWidth));

            AutoScrollMinSize = new Size(0, contentHeight);
            base.OnLayout(e);
        }
        finally
        {
            _organizandoPagina = false;
        }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (_layoutPronto && Visible)
            PerformLayout();
    }

    private ColunasDaPagina CalcularColunas(int width)
    {
        var horizontalPadding = Math.Min(LogicalToDeviceUnits(24), width / 8);
        var gap = LogicalToDeviceUnits(24);
        var availableWidth = Math.Max(1, width - horizontalPadding * 2);
        var sideBySide = availableWidth >= LogicalToDeviceUnits(860);
        var leftWidth = !sideBySide
            ? availableWidth
            : Math.Min(LogicalToDeviceUnits(LarguraColunaEsquerda), availableWidth / 2);
        var rightWidth = sideBySide ? availableWidth - leftWidth - gap : availableWidth;
        return new ColunasDaPagina(horizontalPadding, gap, sideBySide, leftWidth, rightWidth);
    }

    private int OrganizarPagina(int width)
    {
        var verticalPadding = LogicalToDeviceUnits(12);
        var colunas = CalcularColunas(width);
        var scrollPosition = AutoScrollPosition;

        Pnl_Header.Padding = new Padding(colunas.PaddingHorizontal, verticalPadding, colunas.PaddingHorizontal, verticalPadding);
        Pnl_Header.SetBounds(scrollPosition.X, scrollPosition.Y, width, Pnl_Header.Height);
        OrganizarTitulosDoCabecalho(colunas);

        Lbl_Carregando.SetBounds(0, Pnl_Header.Height, width, Math.Max(1, ClientSize.Height - Pnl_Header.Height));
        if (_carregando)
            return Pnl_Header.Height;

        var leftHeight = OrganizarColunaEsquerda(colunas.LarguraEsquerda);
        Pnl_Esquerda.SetBounds(colunas.PaddingHorizontal, verticalPadding, colunas.LarguraEsquerda, leftHeight);

        var rightY = colunas.LadoALado ? verticalPadding : Pnl_Esquerda.Bottom + colunas.Espaco;
        Pnl_Info.SetBounds(colunas.XDireita, rightY, colunas.LarguraDireita, Pnl_Info.Height);
        Pnl_Info.ArrangeContent(colunas.LarguraDireita);

        var bodyHeight = Math.Max(Pnl_Esquerda.Bottom, Pnl_Info.Bottom) + verticalPadding;
        Pnl_Conteudo.SetBounds(scrollPosition.X, scrollPosition.Y + Pnl_Header.Height, width, bodyHeight);
        return Pnl_Header.Height + bodyHeight;
    }

    private int OrganizarColunaEsquerda(int width)
    {
        var gap = LogicalToDeviceUnits(12);
        var innerPadding = Math.Min(LogicalToDeviceUnits(12), width / 8);
        var availableWidth = Math.Max(1, width - innerPadding * 2);
        var contentWidth = Math.Min(LogicalToDeviceUnits(LarguraConteudoColuna), width);

        var coverSize = CalcularTamanhoCapa(contentWidth);
        Pbx_Capa.SetBounds((width - coverSize.Width) / 2, 0, coverSize.Width, coverSize.Height);

        Btn_ExibirMyAnime.Visible = _consultaLocal && _animeAtual?.MyAnimeID > 0;
        Btn_EditarAnime.Visible = _consultaLocal;
        Btn_SalvarComoMyAnime.Visible = !_consultaLocal;
        Btn_SalvarComoAnime.Visible = !_consultaLocal;

        var botaoAcimaDasEstatisticas = _consultaLocal ? Btn_ExibirMyAnime : Btn_SalvarComoMyAnime;
        var botaoAbaixoDasEstatisticas = _consultaLocal ? Btn_EditarAnime : Btn_SalvarComoAnime;
        var buttonX = (width - contentWidth) / 2;
        var y = 0;
        if (botaoAcimaDasEstatisticas.Visible)
        {
            botaoAcimaDasEstatisticas.SetBounds(
                buttonX, y, contentWidth, MedirAlturaBotao(botaoAcimaDasEstatisticas, contentWidth));
            y = botaoAcimaDasEstatisticas.Bottom + gap;
        }

        var rowGap = LogicalToDeviceUnits(2);
        var blockGap = LogicalToDeviceUnits(16);
        var hasRows = false;
        foreach (var label in new[] { Lbl_EstatisticasRapidas, Lbl_Episodios, Lbl_TempoPorEpisodio, Lbl_Generos })
        {
            var hasText = !string.IsNullOrWhiteSpace(label.Text);
            label.Visible = hasText;
            if (!hasText)
                continue;

            if (hasRows)
                y += rowGap;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.SetBounds(innerPadding, y, availableWidth, MedirAlturaTexto(label, availableWidth));
            y = label.Bottom;
            hasRows = true;
        }
        if (hasRows)
            y += blockGap;

        botaoAbaixoDasEstatisticas.SetBounds(
            buttonX, y, contentWidth, MedirAlturaBotao(botaoAbaixoDasEstatisticas, contentWidth));

        Pnl_Stats.SetBounds(
            0, coverSize.IsEmpty ? 0 : Pbx_Capa.Bottom + gap, width, botaoAbaixoDasEstatisticas.Bottom);
        Pnl_Acoes.Visible = false;
        Pnl_Acoes.SetBounds(0, Pnl_Stats.Bottom + gap, width, 0);
        return Pnl_Stats.Bottom + gap;
    }

    /// <summary>
    /// Ajusta a capa ao limite compartilhado pelas fontes e preserva sua proporção. Enquanto carrega,
    /// reserva o tamanho máximo; se não existir ou falhar, recolhe a capa e aproxima os botões.
    /// </summary>
    private Size CalcularTamanhoCapa(int maxWidth)
    {
        var image = Pbx_Capa.Image;
        if (image is null && _capaIndisponivel)
            return Size.Empty;

        var source = image is { Width: > 0, Height: > 0 }
            ? image.Size
            : new Size(LarguraConteudoColuna, AlturaMaximaCapa);
        var maxHeight = LogicalToDeviceUnits(AlturaMaximaCapa);
        var scale = Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height);
        return new Size(
            Math.Max(1, (int)Math.Round(source.Width * scale)),
            Math.Max(1, (int)Math.Round(source.Height * scale)));
    }

    private void MarcarCapaIndisponivel()
    {
        _capaIndisponivel = true;
        if (IsDisposed)
            return;

        if (InvokeRequired)
            BeginInvoke(() =>
            {
                if (!IsDisposed)
                    PerformLayout();
            });
        else
            PerformLayout();
    }

    private int MedirAlturaTexto(IconTextLabel label, int width) =>
        Math.Max(label.Font.Height, label.MedirAltura(width)) + LogicalToDeviceUnits(4);

    private int MedirAlturaBotao(Button button, int width) =>
        Math.Max(LogicalToDeviceUnits(45), TextRenderer.MeasureText(
            button.Text, button.Font,
            new Size(Math.Max(1, width - button.Padding.Horizontal), int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + button.Padding.Vertical);
}
