namespace WinAppDtudo.Controls;

/// <summary>
/// Exibe texto com aparência de rótulo e permite copiar seu conteúdo sem usar um campo de edição.
/// Quebra o texto em linhas, respeita o alinhamento horizontal de <see cref="Label.TextAlign"/>
/// e mantém a seleção com o mouse coerente com o desenho, inclusive em várias linhas.
/// </summary>
public sealed class SelectableTextLabel : Label
{
    private readonly ToolStripMenuItem _copiarMenuItem;
    private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
    private const TextFormatFlags DrawFlags = MeasureFlags | TextFormatFlags.Left | TextFormatFlags.Top;
    private int _selectionAnchor;
    private int _selectionStart;
    private int _selectionLength;
    private bool _isSelecting;

    private sealed record LayoutDoTexto(string Texto, List<TextLine> Linhas, int AlturaLinha, int Topo)
    {
        public int Altura => Linhas.Count * AlturaLinha;
    }

    public SelectableTextLabel()
    {
        Cursor = Cursors.IBeam;
        TabStop = false;
        SetStyle(ControlStyles.Selectable | ControlStyles.OptimizedDoubleBuffer, true);

        _copiarMenuItem = new ToolStripMenuItem("Copiar");
        _copiarMenuItem.Click += (_, _) => CopiarTexto();
        ContextMenuStrip = new ContextMenuStrip();
        ContextMenuStrip.Items.Add(_copiarMenuItem);
    }

    /// <summary>Altura necessária para exibir o texto quebrado na largura informada.</summary>
    public int MedirAltura(int largura) => CriarLayout(largura).Altura;

    public override Size GetPreferredSize(Size proposedSize)
    {
        var texto = Text ?? string.Empty;
        var largura = proposedSize.Width > 0 ? proposedSize.Width : int.MaxValue / 4;
        var layout = CriarLayout(largura);
        var maiorLinha = layout.Linhas.Select(linha => MedirTrecho(texto, linha.Start, linha.Length)).DefaultIfEmpty(0).Max();
        return new Size(proposedSize.Width > 0 ? proposedSize.Width : maiorLinha, layout.Altura);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        _selectionStart = 0;
        _selectionLength = 0;
        _isSelecting = false;
        base.OnTextChanged(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        _isSelecting = e.Button == MouseButtons.Left;
        _selectionAnchor = ObterIndiceDoCursor(e.Location);
        DefinirSelecao(_selectionAnchor, _selectionAnchor);
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isSelecting)
            DefinirSelecao(_selectionAnchor, ObterIndiceDoCursor(e.Location));

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _isSelecting = false;
        base.OnMouseUp(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.A)
        {
            DefinirSelecao(0, Text.Length);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.C)
        {
            CopiarTexto();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);

        var layout = CriarLayout(ClientSize.Width);
        var estadoGrafico = e.Graphics.Save();
        e.Graphics.SetClip(ClientRectangle);
        for (var indice = 0; indice < layout.Linhas.Count; indice++)
            DesenharLinha(e.Graphics, layout, indice);
        e.Graphics.Restore(estadoGrafico);
    }

    private void DesenharLinha(Graphics graphics, LayoutDoTexto layout, int indice)
    {
        var texto = layout.Texto;
        var linha = layout.Linhas[indice];
        var x = ObterDeslocamentoX(MedirTrecho(texto, linha.Start, linha.Length));
        var y = layout.Topo + indice * layout.AlturaLinha;

        var selecaoInicio = Math.Max(_selectionStart, linha.Start);
        var selecaoFim = Math.Min(_selectionStart + _selectionLength, linha.End);
        if (selecaoFim <= selecaoInicio)
        {
            DesenharTrecho(graphics, texto, linha.Start, linha.Length, x, y, ForeColor);
            return;
        }

        var larguraAntes = MedirTrecho(texto, linha.Start, selecaoInicio - linha.Start);
        var larguraSelecionada = MedirTrecho(texto, selecaoInicio, selecaoFim - selecaoInicio);
        graphics.FillRectangle(SystemBrushes.Highlight, x + larguraAntes, y, larguraSelecionada, layout.AlturaLinha);
        DesenharTrecho(graphics, texto, linha.Start, selecaoInicio - linha.Start, x, y, ForeColor);
        DesenharTrecho(graphics, texto, selecaoInicio, selecaoFim - selecaoInicio, x + larguraAntes, y, SystemColors.HighlightText);
        DesenharTrecho(graphics, texto, selecaoFim, linha.End - selecaoFim, x + larguraAntes + larguraSelecionada, y, ForeColor);
    }

    private void DesenharTrecho(Graphics graphics, string texto, int inicio, int tamanho, int x, int y, Color cor)
    {
        if (tamanho > 0)
            TextRenderer.DrawText(graphics, texto.Substring(inicio, tamanho), Font, new Point(x, y), cor, DrawFlags);
    }

    private void CopiarTexto()
    {
        var textoParaCopiar = _selectionLength > 0
            ? Text.Substring(_selectionStart, _selectionLength)
            : Text;

        if (!string.IsNullOrWhiteSpace(textoParaCopiar))
            Clipboard.SetText(textoParaCopiar);
    }

    private int ObterIndiceDoCursor(Point ponto)
    {
        var layout = CriarLayout(ClientSize.Width);
        if (layout.Linhas.Count == 0)
            return 0;

        var texto = layout.Texto;
        var indiceLinha = Math.Clamp((ponto.Y - layout.Topo) / Math.Max(1, layout.AlturaLinha), 0, layout.Linhas.Count - 1);
        var linha = layout.Linhas[indiceLinha];
        var larguraDesejada = ponto.X - ObterDeslocamentoX(MedirTrecho(texto, linha.Start, linha.Length));

        for (var deslocamento = 0; deslocamento < linha.Length; deslocamento++)
        {
            var passo = char.IsHighSurrogate(texto[linha.Start + deslocamento]) && deslocamento + 1 < linha.Length ? 2 : 1;
            var inicioCaractere = MedirTrecho(texto, linha.Start, deslocamento);
            var fimCaractere = MedirTrecho(texto, linha.Start, deslocamento + passo);
            if (larguraDesejada <= inicioCaractere + ((fimCaractere - inicioCaractere) / 2))
                return linha.Start + deslocamento;

            deslocamento += passo - 1;
        }

        return linha.End;
    }

    private void DefinirSelecao(int inicio, int fim)
    {
        _selectionStart = Math.Min(inicio, fim);
        _selectionLength = Math.Abs(fim - inicio);
        Invalidate();
    }

    private LayoutDoTexto CriarLayout(int largura)
    {
        var texto = Text ?? string.Empty;
        var linhas = TextLineBreaker.Break(texto, largura, (inicio, tamanho) => MedirTrecho(texto, inicio, tamanho));
        var alturaLinha = TextRenderer.MeasureText("Ag", Font, Size.Empty, MeasureFlags).Height;
        var altura = linhas.Count * alturaLinha;
        var topo = TextAlign switch
        {
            ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight
                => Math.Max(0, (ClientSize.Height - altura) / 2),
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight
                => Math.Max(0, ClientSize.Height - altura),
            _ => 0
        };

        return new LayoutDoTexto(texto, linhas, alturaLinha, topo);
    }

    private int ObterDeslocamentoX(int larguraLinha) => TextAlign switch
    {
        ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter
            => Math.Max(0, (ClientSize.Width - larguraLinha) / 2),
        ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight
            => Math.Max(0, ClientSize.Width - larguraLinha),
        _ => 0
    };

    private int MedirTrecho(string texto, int inicio, int tamanho)
        => tamanho <= 0
            ? 0
            : TextRenderer.MeasureText(texto.Substring(inicio, tamanho), Font, Size.Empty, MeasureFlags).Width;
}
