using System.ComponentModel;
using System.Globalization;

namespace WinAppDtudo.Controls;

/// <summary>
/// Rótulo que desenha emojis com sua fonte de cor nativa e permite substituir essa cor via
/// <see cref="IconColor"/>; o restante do texto usa <see cref="Control.ForeColor"/>.
/// </summary>
public class IconTextLabel : Label
{
    private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
    private const TextFormatFlags DrawFlags = MeasureFlags | TextFormatFlags.Left | TextFormatFlags.Top;
    private Color _iconColor = Color.Empty;
    private Font? _fonteEmoji;
    private Font? _fonteIconeComCor;
    private float _iconFontSize;

    private sealed record LayoutDoTexto(string Texto, bool[] Icones, List<TextLine> Linhas, int AlturaLinha, int Topo)
    {
        public int Altura => Linhas.Count * AlturaLinha;
    }

    /// <summary>Cor opcional dos ícones. <see cref="Color.Empty"/> preserva a paleta original dos emojis.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color IconColor
    {
        get => _iconColor;
        set
        {
            if (_iconColor == value)
                return;

            _iconColor = value;
            Invalidate();
        }
    }

    /// <summary>Tamanho em pontos dos ícones; por padrão acompanha <see cref="Control.Font"/>.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float IconFontSize
    {
        get => _iconFontSize > 0 ? _iconFontSize : Font.Size;
        set
        {
            if (!float.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "O tamanho do ícone deve ser positivo e finito.");

            if (_iconFontSize == value)
                return;

            _iconFontSize = value;
            _fonteEmoji?.Dispose();
            _fonteEmoji = null;
            _fonteIconeComCor?.Dispose();
            _fonteIconeComCor = null;
            Invalidate();
        }
    }

    private bool PossuiIcones => ContemIcone(MarcarIcones(Text ?? string.Empty));

    /// <summary>Altura necessária para exibir o texto quebrado na largura informada.</summary>
    public int MedirAltura(int largura) => PossuiIcones
        ? CriarLayout(largura).Altura
        : TextRenderer.MeasureText(
            Text ?? string.Empty, Font, new Size(Math.Max(1, largura), int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;

    public override Size GetPreferredSize(Size proposedSize) => PossuiIcones && proposedSize.Width > 0
        ? new Size(proposedSize.Width, CriarLayout(proposedSize.Width).Altura)
        : base.GetPreferredSize(proposedSize);

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        _fonteEmoji?.Dispose();
        _fonteEmoji = null;
        _fonteIconeComCor?.Dispose();
        _fonteIconeComCor = null;
        base.OnFontChanged(e);
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fonteEmoji?.Dispose();
            _fonteEmoji = null;
            _fonteIconeComCor?.Dispose();
            _fonteIconeComCor = null;
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (!PossuiIcones)
        {
            base.OnPaint(e);
            return;
        }

        var layout = CriarLayout(ClientSize.Width);
        var estadoGrafico = e.Graphics.Save();
        e.Graphics.SetClip(ClientRectangle);
        for (var indice = 0; indice < layout.Linhas.Count; indice++)
            DesenharLinha(e.Graphics, layout, layout.Linhas[indice], layout.Topo + indice * layout.AlturaLinha);
        e.Graphics.Restore(estadoGrafico);
    }

    private void DesenharLinha(Graphics graphics, LayoutDoTexto layout, TextLine linha, int y)
    {
        var x = ObterDeslocamentoX(MedirTrecho(layout.Texto, layout.Icones, linha.Start, linha.Length));
        var inicio = linha.Start;
        while (inicio < linha.End)
        {
            var fim = FimDoTrecho(layout.Icones, inicio, linha.End);
            var trecho = layout.Texto.Substring(inicio, fim - inicio);
            var ehIcone = layout.Icones[inicio];
            var fonte = ObterFonte(ehIcone);
            var cor = ehIcone && !_iconColor.IsEmpty ? _iconColor : ForeColor;
            TextRenderer.DrawText(graphics, trecho, fonte, new Point(x, y), cor, DrawFlags);
            x += TextRenderer.MeasureText(trecho, fonte, Size.Empty, MeasureFlags).Width;
            inicio = fim;
        }
    }

    private LayoutDoTexto CriarLayout(int largura)
    {
        var texto = Text ?? string.Empty;
        var icones = MarcarIcones(texto);
        // O espaço logo após um ícone nunca inicia uma nova linha, para o ícone não ficar separado do texto.
        var linhas = TextLineBreaker.Break(
            texto, largura, (inicio, tamanho) => MedirTrecho(texto, icones, inicio, tamanho),
            indice => indice > 0 && icones[indice - 1]);
        var alturaLinha = TextRenderer.MeasureText("Ag", Font, Size.Empty, MeasureFlags).Height;
        var inicioIcone = Array.IndexOf(icones, true);
        if (inicioIcone >= 0)
        {
            var fimIcone = FimDoTrecho(icones, inicioIcone, icones.Length);
            var trechoIcone = texto.Substring(inicioIcone, fimIcone - inicioIcone);
            alturaLinha = Math.Max(alturaLinha, TextRenderer.MeasureText(
                trechoIcone, ObterFonte(true), Size.Empty, MeasureFlags).Height);
        }

        var altura = linhas.Count * alturaLinha;
        var topo = TextAlign switch
        {
            ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight
                => Math.Max(0, (ClientSize.Height - altura) / 2),
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight
                => Math.Max(0, ClientSize.Height - altura),
            _ => 0
        };

        return new LayoutDoTexto(texto, icones, linhas, alturaLinha, topo);
    }

    private int ObterDeslocamentoX(int larguraLinha) => TextAlign switch
    {
        ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter
            => Math.Max(0, (ClientSize.Width - larguraLinha) / 2),
        ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight
            => Math.Max(0, ClientSize.Width - larguraLinha),
        _ => 0
    };

    private int MedirTrecho(string texto, bool[] icones, int inicio, int tamanho)
    {
        var largura = 0;
        var fimDoTrecho = inicio + tamanho;
        for (var atual = inicio; atual < fimDoTrecho;)
        {
            var fim = FimDoTrecho(icones, atual, fimDoTrecho);
            largura += TextRenderer.MeasureText(
                texto.Substring(atual, fim - atual),
                ObterFonte(icones[atual]),
                Size.Empty,
                MeasureFlags).Width;
            atual = fim;
        }

        return largura;
    }

    private Font ObterFonte(bool icone)
    {
        if (!icone)
            return Font;

        if (_iconColor.IsEmpty)
        {
            return _fonteEmoji ??= new Font(
                "Segoe UI Emoji",
                IconFontSize,
                Font.Style,
                Font.Unit,
                Font.GdiCharSet,
                Font.GdiVerticalFont);
        }

        if (IconFontSize == Font.Size)
            return Font;

        return _fonteIconeComCor ??= new Font(
            Font.FontFamily,
            IconFontSize,
            Font.Style,
            Font.Unit,
            Font.GdiCharSet,
            Font.GdiVerticalFont);
    }

    /// <summary>Fim do trecho contíguo, a partir de <paramref name="inicio"/>, que mantém a mesma condição de ícone.</summary>
    private static int FimDoTrecho(bool[] icones, int inicio, int limite)
    {
        var fim = inicio + 1;
        while (fim < limite && icones[fim] == icones[inicio])
            fim++;
        return fim;
    }

    private static bool ContemIcone(bool[] icones) => Array.IndexOf(icones, true) >= 0;

    /// <summary>
    /// Marca os caracteres que são ícones: símbolos Unicode (categoria "outro símbolo", que inclui os emojis)
    /// e os modificadores que os acompanham (seletores de variação, junções e tons de pele).
    /// </summary>
    private static bool[] MarcarIcones(string texto)
    {
        var icones = new bool[texto.Length];
        var anteriorEraIcone = false;
        for (var indice = 0; indice < texto.Length;)
        {
            var tamanho = char.IsSurrogatePair(texto, indice) ? 2 : 1;
            var categoria = CharUnicodeInfo.GetUnicodeCategory(texto, indice);
            var ehIcone = categoria == UnicodeCategory.OtherSymbol
                || (anteriorEraIcone && categoria is UnicodeCategory.NonSpacingMark
                    or UnicodeCategory.Format or UnicodeCategory.ModifierSymbol);
            for (var deslocamento = 0; deslocamento < tamanho; deslocamento++)
                icones[indice + deslocamento] = ehIcone;

            anteriorEraIcone = ehIcone;
            indice += tamanho;
        }

        return icones;
    }
}
