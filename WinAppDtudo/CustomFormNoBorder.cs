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
    private const int WM_GETMINMAXINFO = 0x0024;
    private const int WM_WINDOWPOSCHANGED = 0x0047;
    private const int ParkedWindowCoordinate = -30000;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_SYSMENU = 0x00080000;
    private const int MONITOR_DEFAULTTONEAREST = 2;
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
    private IntPtr _lastVisibleMonitor;
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

    protected override CreateParams CreateParams
    {
        get
        {
            var createParams = base.CreateParams;
            // Sem borda e sem ControlBox o WinForms remove estes estilos, e o botao da barra de tarefas deixa de
            // minimizar/restaurar a janela ao ser clicado.
            createParams.Style |= WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU;
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
        if (m.Msg == WM_GETMINMAXINFO)
        {
            base.WndProc(ref m);
            LimitMaximizedBoundsToWorkingArea(m.HWnd, m.LParam);
            return;
        }

        if (m.Msg == WM_WINDOWPOSCHANGED)
        {
            base.WndProc(ref m);
            RememberVisibleMonitor(m.HWnd);
            return;
        }

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

    // Janelas sem borda maximizam sobre a tela inteira, cobrindo a barra de tarefas; aqui o tamanho e a posicao
    // maximizados passam a ser a area de trabalho do monitor onde a janela esta.
    private void LimitMaximizedBoundsToWorkingArea(IntPtr windowHandle, IntPtr minMaxInfoPointer)
    {
        var monitor = ResolveMonitor(windowHandle);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
            return;

        var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(minMaxInfoPointer);
        var work = monitorInfo.WorkArea;
        var bounds = monitorInfo.MonitorArea;
        minMaxInfo.MaxPosition = new NativePoint { X = work.Left - bounds.Left, Y = work.Top - bounds.Top };
        minMaxInfo.MaxSize = new NativePoint { X = work.Right - work.Left, Y = work.Bottom - work.Top };
        minMaxInfo.MaxTrackSize = minMaxInfo.MaxSize;
        Marshal.StructureToPtr(minMaxInfo, minMaxInfoPointer, false);
    }

    // Minimizada, a janela fica estacionada em (-32000,-32000) (tambem durante a restauracao, quando ela ja nao e
    // "iconic" mas ainda nao foi reposicionada) e o monitor "mais proximo" seria qualquer um; ao restaurar a partir
    // da barra de tarefas isso fazia a janela maximizada voltar com a area de trabalho de outro monitor.
    // Nesse caso vale o ultimo monitor em que a janela esteve visivel.
    private IntPtr ResolveMonitor(IntPtr windowHandle)
    {
        if (IsParked(windowHandle))
        {
            if (_lastVisibleMonitor != IntPtr.Zero)
                return _lastVisibleMonitor;

            var placement = new WindowPlacement { Length = Marshal.SizeOf<WindowPlacement>() };
            if (GetWindowPlacement(windowHandle, ref placement))
            {
                var normalPosition = placement.NormalPosition;
                var monitorFromPlacement = MonitorFromRect(ref normalPosition, MONITOR_DEFAULTTONEAREST);
                if (monitorFromPlacement != IntPtr.Zero)
                    return monitorFromPlacement;
            }
        }

        return MonitorFromWindow(windowHandle, MONITOR_DEFAULTTONEAREST);
    }

    private void RememberVisibleMonitor(IntPtr windowHandle)
    {
        if (IsParked(windowHandle))
            return;

        var monitor = MonitorFromWindow(windowHandle, MONITOR_DEFAULTTONEAREST);
        if (monitor != IntPtr.Zero)
            _lastVisibleMonitor = monitor;
    }

    private bool IsParked(IntPtr windowHandle) =>
        WindowState == FormWindowState.Minimized
        || IsIconic(windowHandle)
        || (GetWindowRect(windowHandle, out var rect) && rect.Left <= ParkedWindowCoordinate);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, int flags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref NativeRect rect, int flags);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement placement);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPlacement
    {
        public int Length;
        public int Flags;
        public int ShowCommand;
        public NativePoint MinPosition;
        public NativePoint MaxPosition;
        public NativeRect NormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public int Flags;
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
