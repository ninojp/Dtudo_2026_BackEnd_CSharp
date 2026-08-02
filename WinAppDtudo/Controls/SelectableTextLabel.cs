namespace WinAppDtudo.Controls;

/// <summary>
/// Exibe texto com aparência de rótulo e permite copiar seu conteúdo sem usar um campo de edição.
/// </summary>
public sealed class SelectableTextLabel : Label
{
    private readonly ToolStripMenuItem _copiarMenuItem;
    private const TextFormatFlags TextFlags = TextFormatFlags.NoPadding | TextFormatFlags.WordBreak;
    private int _selectionAnchor;
    private int _selectionStart;
    private int _selectionLength;
    private bool _isSelecting;

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

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        _isSelecting = e.Button == MouseButtons.Left;
        _selectionAnchor = ObterIndiceDoCursor(e.X);
        DefinirSelecao(_selectionAnchor, _selectionAnchor);
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isSelecting)
            DefinirSelecao(_selectionAnchor, ObterIndiceDoCursor(e.X));

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

        var texto = Text ?? string.Empty;
        var areaTexto = ObterAreaTexto(texto);
        var estadoGrafico = e.Graphics.Save();
        e.Graphics.SetClip(ClientRectangle);

        if (_selectionLength == 0)
        {
            TextRenderer.DrawText(e.Graphics, texto, Font, areaTexto, ForeColor, TextFlags);
        }
        else
        {
            var antes = texto[.._selectionStart];
            var selecionado = texto.Substring(_selectionStart, _selectionLength);
            var depois = texto[(_selectionStart + _selectionLength)..];
            var larguraAntes = MedirTexto(antes);
            var larguraSelecionada = MedirTexto(selecionado);
            var areaSelecionada = new Rectangle(areaTexto.X + larguraAntes, areaTexto.Y, larguraSelecionada, areaTexto.Height);

            TextRenderer.DrawText(e.Graphics, antes, Font, areaTexto, ForeColor, TextFlags);
            e.Graphics.FillRectangle(SystemBrushes.Highlight, areaSelecionada);
            TextRenderer.DrawText(e.Graphics, selecionado, Font, areaSelecionada, SystemColors.HighlightText, TextFlags);
            TextRenderer.DrawText(
                e.Graphics,
                depois,
                Font,
                new Rectangle(areaSelecionada.Right, areaTexto.Y, Math.Max(0, areaTexto.Right - areaSelecionada.Right), areaTexto.Height),
                ForeColor,
                TextFlags);
        }

        e.Graphics.Restore(estadoGrafico);
    }

    private void CopiarTexto()
    {
        var textoParaCopiar = _selectionLength > 0
            ? Text.Substring(_selectionStart, _selectionLength)
            : Text;

        if (!string.IsNullOrWhiteSpace(textoParaCopiar))
            Clipboard.SetText(textoParaCopiar);
    }

    private int ObterIndiceDoCursor(int coordenadaX)
    {
        var texto = Text ?? string.Empty;
        var areaTexto = ObterAreaTexto(texto);
        var larguraDesejada = Math.Max(0, coordenadaX - areaTexto.X);

        for (var indice = 0; indice < texto.Length; indice++)
        {
            var inicioCaractere = MedirTexto(texto[..indice]);
            var fimCaractere = MedirTexto(texto[..(indice + 1)]);
            var meioCaractere = inicioCaractere + ((fimCaractere - inicioCaractere) / 2);

            if (larguraDesejada <= meioCaractere)
                return indice;
        }

        return texto.Length;
    }

    private void DefinirSelecao(int inicio, int fim)
    {
        _selectionStart = Math.Min(inicio, fim);
        _selectionLength = Math.Abs(fim - inicio);
        Invalidate();
    }

    private Rectangle ObterAreaTexto(string texto)
    {
        var alturaTexto = TextRenderer.MeasureText(
            texto,
            Font,
            new Size(Math.Max(1, ClientSize.Width), int.MaxValue),
            TextFlags).Height;
        var y = TextAlign is ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight
            ? Math.Max(0, (ClientSize.Height - alturaTexto) / 2)
            : 0;

        return new Rectangle(0, y, ClientSize.Width, alturaTexto);
    }

    private int MedirTexto(string texto)
        => TextRenderer.MeasureText(texto, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
}
