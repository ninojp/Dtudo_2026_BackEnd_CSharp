using System.ComponentModel;
using WinAppDtudo.Controls;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public partial class FUC_DetalhesAnime
{
    private bool _layoutPronto;
    private bool _organizandoPagina;
    private bool _carregando = true;
    private bool _carregamentoIniciado;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor => DarkModeColors.GetAnimeDetailAccentColor(_consultaLocal);

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

    private int OrganizarPagina(int width)
    {
        var horizontalPadding = Math.Min(LogicalToDeviceUnits(24), width / 8);
        var verticalPadding = LogicalToDeviceUnits(12);
        var gap = LogicalToDeviceUnits(24);
        var scrollPosition = AutoScrollPosition;

        Pnl_Header.Padding = new Padding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
        Pnl_Header.SetBounds(scrollPosition.X, scrollPosition.Y, width, Pnl_Header.Height);
        OrganizarTitulosDoCabecalho();

        Lbl_Carregando.SetBounds(0, Pnl_Header.Height, width, Math.Max(1, ClientSize.Height - Pnl_Header.Height));
        if (_carregando)
            return Pnl_Header.Height;

        var availableWidth = Math.Max(1, width - horizontalPadding * 2);
        var sideBySide = availableWidth >= LogicalToDeviceUnits(860);
        var leftWidth = sideBySide
            ? Math.Clamp(availableWidth / 3, LogicalToDeviceUnits(280), LogicalToDeviceUnits(420))
            : availableWidth;
        var leftHeight = OrganizarColunaEsquerda(leftWidth);
        Pnl_Esquerda.SetBounds(horizontalPadding, verticalPadding, leftWidth, leftHeight);

        var rightWidth = sideBySide ? availableWidth - leftWidth - gap : availableWidth;
        var rightX = sideBySide ? Pnl_Esquerda.Right + gap : horizontalPadding;
        var rightY = sideBySide ? verticalPadding : Pnl_Esquerda.Bottom + gap;
        Pnl_Info.SetBounds(rightX, rightY, rightWidth, Pnl_Info.Height);
        Pnl_Info.ArrangeContent(rightWidth);

        var bodyHeight = Math.Max(Pnl_Esquerda.Bottom, Pnl_Info.Bottom) + verticalPadding;
        Pnl_Conteudo.SetBounds(scrollPosition.X, scrollPosition.Y + Pnl_Header.Height, width, bodyHeight);
        return Pnl_Header.Height + bodyHeight;
    }

    private int OrganizarColunaEsquerda(int width)
    {
        var gap = LogicalToDeviceUnits(12);
        var coverHeight = Math.Clamp((int)Math.Round(width * 1.35), LogicalToDeviceUnits(240), LogicalToDeviceUnits(560));
        Pbx_Capa.SetBounds(0, 0, width, coverHeight);

        var innerPadding = Math.Min(LogicalToDeviceUnits(12), width / 8);
        var availableWidth = Math.Max(1, width - innerPadding * 2);
        var rowGap = LogicalToDeviceUnits(5);
        var y = LogicalToDeviceUnits(10);

        foreach (var label in new[] { Lbl_EstatisticasRapidas, Lbl_Episodios, Lbl_TempoPorEpisodio })
        {
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.SetBounds(innerPadding, y, availableWidth, Math.Max(LogicalToDeviceUnits(40), label.Font.Height + LogicalToDeviceUnits(8)));
            y = label.Bottom + rowGap;
        }

        var hasGenres = !string.IsNullOrWhiteSpace(Lbl_Generos.Text);
        Lbl_Generos.Visible = hasGenres;
        Lbl_Generos.TextAlign = ContentAlignment.MiddleCenter;
        Lbl_Generos.SetBounds(innerPadding, y, availableWidth, hasGenres
            ? Math.Max(LogicalToDeviceUnits(35), TextRenderer.MeasureText(
                Lbl_Generos.Text, Lbl_Generos.Font, new Size(availableWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + LogicalToDeviceUnits(8))
            : 0);
        if (hasGenres)
            y = Lbl_Generos.Bottom;

        var showMyAnime = _consultaLocal && _animeAtual?.MyAnimeID > 0;
        Btn_ExibirMyAnime.Visible = showMyAnime;
        Btn_EditarAnime.Visible = _consultaLocal;
        var buttonWidth = Math.Min(LogicalToDeviceUnits(300), availableWidth);
        var buttonX = (width - buttonWidth) / 2;
        if (showMyAnime || _consultaLocal)
            y += LogicalToDeviceUnits(24);
        if (showMyAnime)
        {
            Btn_ExibirMyAnime.SetBounds(buttonX, y, buttonWidth, MedirAlturaBotao(Btn_ExibirMyAnime, buttonWidth));
            y = Btn_ExibirMyAnime.Bottom + gap;
        }
        if (_consultaLocal)
        {
            Btn_EditarAnime.SetBounds(buttonX, y, buttonWidth, MedirAlturaBotao(Btn_EditarAnime, buttonWidth));
            y = Btn_EditarAnime.Bottom;
        }

        Pnl_Stats.SetBounds(0, Pbx_Capa.Bottom + gap, width, y + LogicalToDeviceUnits(8));
        Pnl_Acoes.Visible = !_consultaLocal;
        var actionHeight = 0;
        if (!_consultaLocal)
        {
            var actionGap = LogicalToDeviceUnits(8);
            Btn_SalvarComoMyAnime.SetBounds(innerPadding, 0, availableWidth,
                MedirAlturaBotao(Btn_SalvarComoMyAnime, availableWidth));
            Btn_SalvarComoAnime.SetBounds(innerPadding, Btn_SalvarComoMyAnime.Bottom + actionGap, availableWidth,
                MedirAlturaBotao(Btn_SalvarComoAnime, availableWidth));
            actionHeight = Btn_SalvarComoAnime.Bottom;
        }
        Pnl_Acoes.SetBounds(0, Pnl_Stats.Bottom + gap, width, actionHeight);
        return (!_consultaLocal ? Pnl_Acoes.Bottom : Pnl_Stats.Bottom) + gap;
    }

    private int MedirAlturaBotao(Button button, int width) =>
        Math.Max(LogicalToDeviceUnits(45), TextRenderer.MeasureText(
            button.Text, button.Font,
            new Size(Math.Max(1, width - button.Padding.Horizontal), int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + button.Padding.Vertical);
}
