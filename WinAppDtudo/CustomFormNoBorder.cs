using System.Runtime.InteropServices;

namespace WinAppDtudo;

/// <summary>
/// CustomFormNoBorder é uma classe base customizada que remove a barra de título do formulário
/// e adiciona botões de controle (minimizar, maximizar, fechar) no MenuStrip.
/// Herdar dessa classe evita duplicação de código em múltiplos formulários.
/// </summary>
public class CustomFormNoBorder : Form
{
    // Constantes da Windows API para remover a barra de título e permitir arrastar
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

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
        // Remove a barra de título e os controles padrão
        this.ControlBox = false;
        this.Text = string.Empty;
        this.FormBorderStyle = FormBorderStyle.None;
        this.Icon = null;

        // Implementa o suporte a arrastar a janela pelo MenuStrip, se fornecido
        if (menuStrip != null)
        {
            menuStrip.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    menuStrip.Cursor = Cursors.SizeAll;
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
        }
    }

    /// <summary>
    /// Adiciona os botões de controle (minimizar, maximizar, fechar) ao MenuStrip.
    /// Deve ser chamado após AddRange() dos demais ToolStripItems no Designer.
    /// </summary>
    /// <param name="menuStrip">O MenuStrip do formulário.</param>
    public void AddControlButtonsToMenuStrip(MenuStrip menuStrip)
    {
        // Criar os itens do menu para minimizar, maximizar e fechar
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

        // Adicionar os botões ao MenuStrip
        menuStrip.Items.Add(btnMinimizar);
        menuStrip.Items.Add(btnMaximizar);
        menuStrip.Items.Add(btnFechar);
    }
}
