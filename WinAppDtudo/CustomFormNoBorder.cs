using System.Runtime.InteropServices;

namespace WinAppDtudo;

/// <summary>
/// CustomFormNoBorder é uma classe base customizada que remove a barra de título do formulário
/// e adiciona botões de controle (minimizar, maximizar, fechar) no MenuStrip.
/// Herdar dessa classe evita duplicação de código em múltiplos formulários.
/// </summary>
public class CustomFormNoBorder : Form
{
    private const int WM_NCHITTEST = 0x84;
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int WM_EXITSIZEMOVE = 0x0232;
    private const int HT_CAPTION = 0x2;
    private const int HTCLIENT = 0x1;
    private const int HTLEFT = 0xA;
    private const int HTRIGHT = 0xB;
    private const int HTTOP = 0xC;
    private const int HTTOPLEFT = 0xD;
    private const int HTTOPRIGHT = 0xE;
    private const int HTBOTTOM = 0xF;
    private const int HTBOTTOMLEFT = 0x10;
    private const int HTBOTTOMRIGHT = 0x11;
    private const int ResizeBorderAt96Dpi = 8;
    private const int BorderThickness = 1;

    private MenuStrip? _dragMenuStrip;
    private bool _formDragHandlerAttached;

    public CustomFormNoBorder()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.AllPaintingInWmPaint,
            true);
    }

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    /// <summary>
    /// Inicializa o formulário customizado sem barra de título.
    /// Deve ser chamado no construtor do formulário derivado, após InitializeComponent().
    /// </summary>
    /// <param name="menuStrip">O MenuStrip do formulário. Se nulo, apenas remove a barra de título.</param>
    public void InitializeCustomFormNoBorder(MenuStrip? menuStrip = null)
    {
        ControlBox = false;
        Text = string.Empty;
        FormBorderStyle = FormBorderStyle.None;
        ReserveBorderSpace();

        if (menuStrip != null)
        {
            menuStrip.Dock = DockStyle.Top;

            if (_dragMenuStrip != null)
                _dragMenuStrip.MouseDown -= DragWindow;

            _dragMenuStrip = menuStrip;
            _dragMenuStrip.MouseDown += DragWindow;
        }
        else if (!_formDragHandlerAttached)
        {
            MouseDown += DragWindowFromForm;
            _formDragHandlerAttached = true;
        }

        Invalidate(true);
    }

    /// <summary>
    /// Adiciona os botões de controle (minimizar, maximizar, fechar) ao MenuStrip.
    /// Deve ser chamado após AddRange() dos demais ToolStripItems no Designer.
    /// </summary>
    /// <param name="menuStrip">O MenuStrip do formulário.</param>
    public void AddControlButtonsToMenuStrip(MenuStrip menuStrip)
    {
        ToolStripMenuItem btnMinimizar = new()
        {
            Name = "MnI_Minimizar",
            Text = "−",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Alignment = ToolStripItemAlignment.Right
        };
        btnMinimizar.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

        ToolStripMenuItem btnMaximizar = new()
        {
            Name = "MnI_Maximizar",
            Text = "□",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Alignment = ToolStripItemAlignment.Right
        };
        btnMaximizar.Click += (s, e) =>
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else
                this.WindowState = FormWindowState.Maximized;
        };

        ToolStripMenuItem btnFechar = new()
        {
            Name = "MnI_Fechar",
            Text = "✕",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.Gold,
            Alignment = ToolStripItemAlignment.Right
        };
        btnFechar.Click += (s, e) => this.Close();

        menuStrip.Items.Add(btnFechar);
        menuStrip.Items.Add(btnMaximizar);
        menuStrip.Items.Add(btnMinimizar);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Invalidate(true);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        Invalidate();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        RedrawVisualTree();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        const int borderThickness = 1;
        var topAndLeftColor = Color.FromArgb(224, Color.Gold);

        using var topAndLeftBrush = new SolidBrush(topAndLeftColor);
        using var bottomAndRightBrush = new SolidBrush(Color.Gold);

        e.Graphics.FillRectangle(topAndLeftBrush, 0, 0, ClientSize.Width, borderThickness);
        e.Graphics.FillRectangle(topAndLeftBrush, 0, 0, borderThickness, ClientSize.Height);
        e.Graphics.FillRectangle(bottomAndRightBrush, 0, ClientSize.Height - borderThickness, ClientSize.Width, borderThickness);
        e.Graphics.FillRectangle(bottomAndRightBrush, ClientSize.Width - borderThickness, 0, borderThickness, ClientSize.Height);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_EXITSIZEMOVE)
        {
            base.WndProc(ref m);
            RedrawVisualTree();
            return;
        }

        if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
        {
            base.WndProc(ref m);

            if (m.Result == (IntPtr)HTCLIENT)
            {
                var clientPoint = PointToClient(Cursor.Position);
                var hitTest = GetResizeHitTest(clientPoint);
                if (hitTest != HTCLIENT)
                {
                    m.Result = (IntPtr)hitTest;
                    return;
                }
            }

            return;
        }

        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_dragMenuStrip != null)
                _dragMenuStrip.MouseDown -= DragWindow;

            if (_formDragHandlerAttached)
                MouseDown -= DragWindowFromForm;

        }

        base.Dispose(disposing);
    }

    private void DragWindow(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
    }

    private void DragWindowFromForm(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || e.Y > GetResizeBorderWidth() * 2)
            return;

        DragWindow(sender, e);
    }

    private int GetResizeHitTest(Point point)
    {
        var resizeBorder = GetResizeBorderWidth();
        var left = point.X >= 0 && point.X <= resizeBorder;
        var right = point.X >= ClientSize.Width - resizeBorder && point.X < ClientSize.Width;
        var top = point.Y >= 0 && point.Y <= resizeBorder;
        var bottom = point.Y >= ClientSize.Height - resizeBorder && point.Y < ClientSize.Height;

        if (top && left)
            return HTTOPLEFT;
        if (top && right)
            return HTTOPRIGHT;
        if (bottom && left)
            return HTBOTTOMLEFT;
        if (bottom && right)
            return HTBOTTOMRIGHT;
        if (left)
            return HTLEFT;
        if (right)
            return HTRIGHT;
        if (top)
            return HTTOP;
        if (bottom)
            return HTBOTTOM;

        return HTCLIENT;
    }

    private int GetResizeBorderWidth()
    {
        return Math.Max(BorderThickness, (int)Math.Round(ResizeBorderAt96Dpi * DeviceDpi / 96f));
    }

    private void ReserveBorderSpace()
    {
        Padding = new Padding(
            Math.Max(Padding.Left, BorderThickness),
            Math.Max(Padding.Top, BorderThickness),
            Math.Max(Padding.Right, BorderThickness),
            Math.Max(Padding.Bottom, BorderThickness));
    }

    private void RedrawVisualTree()
    {
        if (!IsHandleCreated || IsDisposed)
            return;

        RedrawWindow(
            Handle,
            IntPtr.Zero,
            IntPtr.Zero,
            RedrawWindowFlags.Invalidate
            | RedrawWindowFlags.Erase
            | RedrawWindowFlags.AllChildren
            | RedrawWindowFlags.UpdateNow
            | RedrawWindowFlags.Frame);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RedrawWindow(
        IntPtr hWnd,
        IntPtr lprcUpdate,
        IntPtr hrgnUpdate,
        RedrawWindowFlags flags);

    [Flags]
    private enum RedrawWindowFlags : uint
    {
        Invalidate = 0x0001,
        Erase = 0x0004,
        AllChildren = 0x0080,
        UpdateNow = 0x0100,
        Frame = 0x0400
    }
}
