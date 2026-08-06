namespace WinAppDtudo.Controls;

/// <summary>
/// Botao de navegacao com imagem, fundo transparente e borda exibida somente ao passar o mouse.
/// </summary>
public class NavigationImageButton : PictureBox
{
    private bool _isPointerOver;

    public NavigationImageButton()
    {
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.Selectable,
            true);

        BackColor = Color.Transparent;
        BorderStyle = BorderStyle.None;
        Cursor = Cursors.Hand;
        SizeMode = PictureBoxSizeMode.StretchImage;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isPointerOver = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isPointerOver = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);

        if (!Enabled)
        {
            _isPointerOver = false;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left)
            Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode is not (Keys.Enter or Keys.Space))
            return;

        OnClick(EventArgs.Empty);
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (!_isPointerOver || ClientSize.Width < 2 || ClientSize.Height < 2)
            return;

        using var borderPen = new Pen(Color.Gold);
        e.Graphics.DrawRectangle(borderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }
}
