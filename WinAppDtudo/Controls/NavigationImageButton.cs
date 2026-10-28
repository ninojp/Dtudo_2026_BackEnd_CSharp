using System.ComponentModel;
using System.Drawing.Imaging;

namespace WinAppDtudo.Controls;

/// <summary>
/// Botao de navegacao com imagem, fundo transparente e borda exibida somente ao passar o mouse.
/// Pode exibir a imagem em escala de cinza (<see cref="Grayscale"/>), usada enquanto o atalho esta bloqueado.
/// </summary>
public class NavigationImageButton : PictureBox
{
    private bool _isPointerOver;
    private bool _grayscale;
    private Image? _colorImage;
    private Image? _grayImage;

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

    /// <summary>Imagem colorida original; a imagem exibida pode ser a versao em escala de cinza.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new Image? Image
    {
        get => _colorImage;
        set
        {
            _colorImage = value;
            _grayImage?.Dispose();
            _grayImage = null;
            UpdateDisplayedImage();
        }
    }

    /// <summary>Exibe a imagem em preto e branco.</summary>
    [DefaultValue(false)]
    public bool Grayscale
    {
        get => _grayscale;
        set
        {
            if (_grayscale == value)
                return;

            _grayscale = value;
            UpdateDisplayedImage();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            base.Image = null;
            _grayImage?.Dispose();
            _grayImage = null;
        }

        base.Dispose(disposing);
    }

    private void UpdateDisplayedImage()
    {
        if (_grayscale && _colorImage is not null)
            _grayImage ??= CreateGrayscale(_colorImage);

        base.Image = _grayscale ? _grayImage ?? _colorImage : _colorImage;
    }

    private static Bitmap CreateGrayscale(Image source)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(result);
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix(
        [
            [0.299f, 0.299f, 0.299f, 0f, 0f],
            [0.587f, 0.587f, 0.587f, 0f, 0f],
            [0.114f, 0.114f, 0.114f, 0f, 0f],
            [0f, 0f, 0f, 1f, 0f],
            [0f, 0f, 0f, 0f, 1f]
        ]));
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, result.Width, result.Height),
            0,
            0,
            source.Width,
            source.Height,
            GraphicsUnit.Pixel,
            attributes);
        return result;
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
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        TabStop = Enabled;

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
