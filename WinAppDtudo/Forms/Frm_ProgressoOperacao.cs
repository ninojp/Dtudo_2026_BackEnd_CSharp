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
        ClientSize = new Size(700, 170);

        _lblPercentual = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(12, 10, 12, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "0%"
        };

        _lblDetalhes = new Label
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(12, 4, 12, 0),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.TopLeft,
            Text = "Iniciando..."
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 28,
            Margin = new Padding(12),
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Style = ProgressBarStyle.Continuous
        };

        var pnl = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 0, 12, 12)
        };

        pnl.Controls.Add(_progressBar);
        Controls.Add(pnl);
        Controls.Add(_lblDetalhes);
        Controls.Add(_lblPercentual);
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
