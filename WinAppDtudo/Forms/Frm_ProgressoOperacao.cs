using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public class Frm_ProgressoOperacao : Form
{
    private readonly Label _lblPercentual;
    private readonly Label _lblDetalhes;
    private readonly ProgressBar _progressBar;

    public Frm_ProgressoOperacao(string titulo)
    {
        Text = titulo;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        ClientSize = new Size(1400, 340);

        _lblPercentual = new Label
        {
            Dock = DockStyle.Top,
            Height = 68,
            Padding = new Padding(24, 20, 24, 0),
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "0%"
        };

        _lblDetalhes = new Label
        {
            Dock = DockStyle.Top,
            Height = 116,
            Padding = new Padding(24, 8, 24, 0),
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 14F),
            TextAlign = ContentAlignment.TopLeft,
            Text = "Iniciando..."
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 56,
            Margin = new Padding(24),
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous
        };

        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 0, 24, 24)
        };

        pnl.Controls.Add(_progressBar);
        Controls.Add(pnl);
        Controls.Add(_lblDetalhes);
        Controls.Add(_lblPercentual);
        ThemeManager.ApplyDarkModeToForm(this);
        BackColor = DarkModeColors.ActiveTabBackgroundColor;
        pnl.BackColor = DarkModeColors.ActiveTabBackgroundColor;
        _lblPercentual.BackColor = DarkModeColors.ActiveTabBackgroundColor;
        _lblDetalhes.BackColor = DarkModeColors.ActiveTabBackgroundColor;
    }

    public void Atualizar(int percentual, string mensagem)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(() => Atualizar(percentual, mensagem));
            return;
        }

        var valor = Math.Clamp(percentual, 0, 100);
        _progressBar.Value = valor;
        _lblPercentual.Text = $"{valor}%";
        _lblDetalhes.Text = mensagem;
        _lblPercentual.Refresh();
        _lblDetalhes.Refresh();
        _progressBar.Refresh();
        Refresh();
    }
}
