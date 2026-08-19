namespace WinAppDtudo.Services;

public sealed record DarkSelectionOption(string Value, string DisplayText);

public static class DarkSelectionDialog
{
    public static string? Show(
        string prompt,
        string title,
        IReadOnlyCollection<DarkSelectionOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Count == 0)
        {
            return null;
        }

        using var dialog = new GoldBorderForm
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

        var selection = new ComboBox
        {
            Dock = DockStyle.Top,
            Height = 52,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = nameof(DarkSelectionOption.DisplayText),
            ValueMember = nameof(DarkSelectionOption.Value),
            DataSource = options.ToArray(),
            Font = new Font("Segoe UI", 14F)
        };
        ThemeManager.ApplyDarkModeToControl(selection);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 116,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            Padding = new Padding(0, 24, 0, 0)
        };

        var selectButton = new Button
        {
            Text = "Selecionar",
            DialogResult = DialogResult.OK,
            Size = new Size(200, 68),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };
        var cancelButton = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Size = new Size(200, 68),
            Margin = new Padding(16, 0, 0, 0),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };

        ThemeManager.ApplyDarkModeToControl(selectButton);
        ThemeManager.ApplyDarkModeToControl(cancelButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(selectButton);

        dialog.Controls.Add(selection);
        dialog.Controls.Add(label);
        dialog.Controls.Add(buttons);
        dialog.AcceptButton = selectButton;
        dialog.CancelButton = cancelButton;

        var owner = Form.ActiveForm;
        var result = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return result == DialogResult.OK
            ? (selection.SelectedItem as DarkSelectionOption)?.Value
            : null;
    }
}
