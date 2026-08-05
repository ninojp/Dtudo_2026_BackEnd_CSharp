using System.Drawing.Drawing2D;
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
    private const int WM_SIZING = 0x0214;
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
    private const int CornerRadius = 10;
    private const int ResizeBorder = 8;
    private const int WS_EX_COMPOSITED = 0x02000000;

    private MenuStrip? _dragMenuStrip;
    private BorderOverlay? _borderOverlay;
    private bool _formDragHandlerAttached;
    private bool _isInteractiveResize;

    public CustomFormNoBorder()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        SetStyle(
            ControlStyles.ResizeRedraw
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.AllPaintingInWmPaint,
            true);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WS_EX_COMPOSITED;
            return createParams;
        }
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

        UpdateWindowRegion();
        EnsureBorderOverlay();
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
        UpdateWindowRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (!_isInteractiveResize)
            UpdateWindowRegion();

        Invalidate();
        _borderOverlay?.RefreshBorder();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        UpdateWindowRegion();
        _borderOverlay?.RefreshBorder();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_SIZING)
        {
            if (!_isInteractiveResize)
            {
                _isInteractiveResize = true;
                ReplaceRegion(null);
                _borderOverlay?.RefreshBorder();
            }

            base.WndProc(ref m);
            return;
        }

        if (m.Msg == WM_EXITSIZEMOVE)
        {
            base.WndProc(ref m);

            if (_isInteractiveResize)
            {
                _isInteractiveResize = false;
                UpdateWindowRegion();
                Invalidate();
                _borderOverlay?.RefreshBorder();
            }

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
        if (e.Button != MouseButtons.Left || e.Y > ResizeBorder * 2)
            return;

        DragWindow(sender, e);
    }

    private int GetResizeHitTest(Point point)
    {
        var left = point.X >= 0 && point.X <= ResizeBorder;
        var right = point.X >= ClientSize.Width - ResizeBorder && point.X < ClientSize.Width;
        var top = point.Y >= 0 && point.Y <= ResizeBorder;
        var bottom = point.Y >= ClientSize.Height - ResizeBorder && point.Y < ClientSize.Height;

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

    private void UpdateWindowRegion()
    {
        if (!IsHandleCreated || ClientSize.Width <= 1 || ClientSize.Height <= 1)
            return;

        if (WindowState == FormWindowState.Maximized)
        {
            ReplaceRegion(null);
            return;
        }

        var radius = Math.Max(1, (int)Math.Round(CornerRadius * DeviceDpi / 96f));
        using var path = CreateRoundedRectanglePath(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), radius);
        ReplaceRegion(new Region(path));
    }

    private void EnsureBorderOverlay()
    {
        if (_borderOverlay != null)
            return;

        _borderOverlay = new BorderOverlay(this)
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Bounds = ClientRectangle,
            TabStop = false
        };

        Controls.Add(_borderOverlay);
        _borderOverlay.BringToFront();
        _borderOverlay.BackColor = Color.Transparent;
        _borderOverlay.RefreshBorder();
    }

    private void ReplaceRegion(Region? region)
    {
        var previousRegion = Region;
        Region = region;
        previousRegion?.Dispose();
    }

    private sealed class BorderOverlay : Control
    {
        private const int WM_NCHITTEST = 0x84;
        private const int WM_MOUSEACTIVATE = 0x21;
        private const int HTTRANSPARENT = -1;
        private const int MA_NOACTIVATE = 3;
        private const int WS_EX_TRANSPARENT = 0x20;

        private readonly CustomFormNoBorder _owner;

        public BorderOverlay(CustomFormNoBorder owner)
        {
            _owner = owner;
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TRANSPARENT;
                return createParams;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RefreshBorder();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RefreshBorder();
        }

        public void RefreshBorder()
        {
            Visible = _owner.WindowState != FormWindowState.Maximized;
            if (!Visible || ClientSize.Width <= 1 || ClientSize.Height <= 1)
                return;

            var borderWidth = Math.Max(0.5f, 96f / _owner.DeviceDpi);
            var radius = Math.Max(1f, CornerRadius * _owner.DeviceDpi / 96f);
            var inset = Math.Max(1f, borderWidth * 1.5f);
            var innerBounds = new RectangleF(
                inset,
                inset,
                Math.Max(1f, ClientSize.Width - inset * 2),
                Math.Max(1f, ClientSize.Height - inset * 2));

            using var outerPath = CreateRoundedRectanglePath(
                new RectangleF(0, 0, ClientSize.Width, ClientSize.Height),
                radius);
            using var innerPath = CreateRoundedRectanglePath(
                innerBounds,
                Math.Max(1f, radius - inset));
            var borderRegion = new Region(outerPath);
            borderRegion.Exclude(innerPath);

            var previousRegion = Region;
            Region = borderRegion;
            previousRegion?.Dispose();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_owner.WindowState == FormWindowState.Maximized || ClientSize.Width <= 1 || ClientSize.Height <= 1)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var borderWidth = Math.Max(0.5f, 96f / _owner.DeviceDpi);
            var bounds = new RectangleF(
                borderWidth / 2,
                borderWidth / 2,
                ClientSize.Width - borderWidth,
                ClientSize.Height - borderWidth);
            var radius = Math.Max(1f, CornerRadius * _owner.DeviceDpi / 96f);

            using var path = CreateRoundedRectanglePath(bounds, radius);
            using var pen = new Pen(Color.Gold, borderWidth);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }

            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }

            base.WndProc(ref m);
        }
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
    {
        return CreateRoundedRectanglePath(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), radius);
    }

    private static GraphicsPath CreateRoundedRectanglePath(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
