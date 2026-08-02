namespace WinAppDtudo.Services;

public static class DarkInputDialog
{
    public static string Show(string prompt, string title, string defaultValue = "")
    {
        using var dialog = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(920, 360),
            Padding = new Padding(32),
            Font = new Font("Segoe UI", 14F)
        };

        ThemeManager.ApplyDarkModeToForm(dialog);
        dialog.BackColor = DarkModeColors.ActiveTabBackgroundColor;

        var label = new Label
        {
            Dock = DockStyle.Top,
            Height = 116,
            Text = prompt,
            ForeColor = DarkModeColors.TextColor,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
            Font = new Font("Segoe UI", 14F)
        };

        var input = new TextBox
        {
            Dock = DockStyle.Top,
            Text = defaultValue,
            Height = 52,
            Font = new Font("Segoe UI", 14F)
        };
        ThemeManager.ApplyDarkModeToControl(input);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 116,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            Padding = new Padding(0, 24, 0, 0)
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(200, 68),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };
        var cancelar = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Size = new Size(200, 68),
            Margin = new Padding(16, 0, 0, 0),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };

        ThemeManager.ApplyDarkModeToControl(ok);
        ThemeManager.ApplyDarkModeToControl(cancelar);
        buttons.Controls.Add(cancelar);
        buttons.Controls.Add(ok);

        dialog.Controls.Add(input);
        dialog.Controls.Add(label);
        dialog.Controls.Add(buttons);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancelar;

        input.SelectAll();
        var owner = Form.ActiveForm;
        var result = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return result == DialogResult.OK ? input.Text : string.Empty;
    }
}
