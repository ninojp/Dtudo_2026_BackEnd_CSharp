namespace WinAppDtudo.Services;

/// <summary>
/// MessageBox customizado para manter dialogs do app no tema escuro.
/// </summary>
public static class DarkMessageBox
{
    public static DialogResult Show(string text)
        => ShowCore(null, text, "Mensagem", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);

    public static DialogResult Show(string text, string caption)
        => ShowCore(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);

    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        => ShowCore(null, text, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);

    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        => ShowCore(null, text, caption, buttons, icon, MessageBoxDefaultButton.Button1);

    public static DialogResult Show(
        string text,
        string caption,
        MessageBoxButtons buttons,
        MessageBoxIcon icon,
        MessageBoxDefaultButton defaultButton)
        => ShowCore(null, text, caption, buttons, icon, defaultButton);

    public static DialogResult Show(
        IWin32Window? owner,
        string text,
        string caption,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
        => ShowCore(owner, text, caption, buttons, icon, defaultButton);

    private static DialogResult ShowCore(
        IWin32Window? owner,
        string text,
        string caption,
        MessageBoxButtons buttons,
        MessageBoxIcon icon,
        MessageBoxDefaultButton defaultButton)
    {
        using var dialog = new Form
        {
            Text = string.IsNullOrWhiteSpace(caption) ? "Mensagem" : caption,
            StartPosition = owner is null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = owner is null,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            ForeColor = DarkModeColors.TextColor,
            AutoScaleMode = AutoScaleMode.Font,
            Font = new Font("Segoe UI", 14F),
            Padding = new Padding(36)
        };

        ThemeManager.ApplyDarkModeToForm(dialog);
        dialog.BackColor = DarkModeColors.ActiveTabBackgroundColor;
        dialog.ForeColor = DarkModeColors.TextColor;

        var iconBox = CriarIcone(icon);
        var message = new Label
        {
            AutoSize = false,
            ForeColor = DarkModeColors.TextColor,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
            Font = new Font("Segoe UI", 15F)
        };

        var measured = TextRenderer.MeasureText(
            text,
            message.Font,
            new Size(1200, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

        var messageWidth = Math.Clamp(measured.Width + 24, 520, 1200);
        var messageHeight = Math.Clamp(measured.Height + 48, 160, 720);
        var iconWidth = iconBox is null ? 0 : 108;
        var contentWidth = messageWidth + iconWidth;
        var buttonsPanelHeight = 116;

        dialog.ClientSize = new Size(
            Math.Clamp(contentWidth + dialog.Padding.Horizontal, 720, 1500),
            Math.Clamp(messageHeight + buttonsPanelHeight + dialog.Padding.Vertical, 320, 900));

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            Padding = new Padding(0, 0, 0, 24)
        };

        if (iconBox is not null)
        {
            iconBox.Location = new Point(0, 16);
            contentPanel.Controls.Add(iconBox);
        }

        message.Location = new Point(iconWidth, 0);
        message.Size = new Size(dialog.ClientSize.Width - dialog.Padding.Horizontal - iconWidth, dialog.ClientSize.Height - dialog.Padding.Vertical - buttonsPanelHeight);
        contentPanel.Controls.Add(message);

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = buttonsPanelHeight,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            Padding = new Padding(0, 24, 0, 0)
        };

        var buttonInfos = ObterBotoes(buttons);
        Button? defaultControl = null;
        Button? cancelControl = null;

        foreach (var (label, result) in buttonInfos)
        {
            var button = new Button
            {
                Text = label,
                DialogResult = result,
                Size = new Size(224, 68),
                Margin = new Padding(16, 0, 0, 0),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };
            ThemeManager.ApplyDarkModeToControl(button);
            buttonsPanel.Controls.Add(button);

            if (defaultControl is null || IsDefaultButton(defaultButton, buttonInfos.IndexOf((label, result))))
                defaultControl = button;

            if (result is DialogResult.Cancel or DialogResult.No)
                cancelControl ??= button;
        }

        dialog.Controls.Add(contentPanel);
        dialog.Controls.Add(buttonsPanel);
        dialog.AcceptButton = defaultControl;
        dialog.CancelButton = cancelControl ?? buttonInfos.Select((_, index) => buttonsPanel.Controls[index]).OfType<Button>().FirstOrDefault();

        var activeOwner = owner ?? Form.ActiveForm;
        return activeOwner is null ? dialog.ShowDialog() : dialog.ShowDialog(activeOwner);
    }

    private static PictureBox? CriarIcone(MessageBoxIcon icon)
    {
        var systemIcon = icon switch
        {
            MessageBoxIcon.Error or MessageBoxIcon.Hand or MessageBoxIcon.Stop => SystemIcons.Error,
            MessageBoxIcon.Warning or MessageBoxIcon.Exclamation => SystemIcons.Warning,
            MessageBoxIcon.Information or MessageBoxIcon.Asterisk => SystemIcons.Information,
            MessageBoxIcon.Question => SystemIcons.Question,
            _ => null
        };

        return systemIcon is null
            ? null
            : new PictureBox
            {
                Image = systemIcon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(84, 84),
                BackColor = DarkModeColors.ActiveTabBackgroundColor
            };
    }

    private static List<(string Label, DialogResult Result)> ObterBotoes(MessageBoxButtons buttons)
    {
        return buttons switch
        {
            MessageBoxButtons.OKCancel =>
            [
                ("OK", DialogResult.OK),
                ("Cancelar", DialogResult.Cancel)
            ],
            MessageBoxButtons.YesNo =>
            [
                ("Sim", DialogResult.Yes),
                ("Nao", DialogResult.No)
            ],
            MessageBoxButtons.YesNoCancel =>
            [
                ("Sim", DialogResult.Yes),
                ("Nao", DialogResult.No),
                ("Cancelar", DialogResult.Cancel)
            ],
            MessageBoxButtons.RetryCancel =>
            [
                ("Tentar novamente", DialogResult.Retry),
                ("Cancelar", DialogResult.Cancel)
            ],
            MessageBoxButtons.AbortRetryIgnore =>
            [
                ("Abortar", DialogResult.Abort),
                ("Tentar novamente", DialogResult.Retry),
                ("Ignorar", DialogResult.Ignore)
            ],
            _ =>
            [
                ("OK", DialogResult.OK)
            ]
        };
    }

    private static bool IsDefaultButton(MessageBoxDefaultButton defaultButton, int index)
    {
        return defaultButton switch
        {
            MessageBoxDefaultButton.Button2 => index == 1,
            MessageBoxDefaultButton.Button3 => index == 2,
            _ => index == 0
        };
    }
}
