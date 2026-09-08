namespace WinAppDtudo.Services;

/// <summary>Decisão do usuário quando a estrutura do MyAnime já existe no destino escolhido.</summary>
public enum DecisaoEstruturaExistente
{
    Cancelar,
    AdicionarNovos
}

/// <summary>
/// Diálogo dark exibido quando a estrutura do MyAnime já existe na pasta de destino.
/// Deixa explícito que nenhum conteúdo existente será apagado ou substituído e oferece
/// apenas duas saídas: adicionar exclusivamente o que ainda não existe ou cancelar tudo.
/// </summary>
public static class EstruturaExistenteDialog
{
    public static DecisaoEstruturaExistente Show(string caminhoEstrutura)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caminhoEstrutura);

        var decisao = DecisaoEstruturaExistente.Cancelar;

        using var dialog = new GoldBorderForm
        {
            Text = "Estrutura já existente",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(1020, 460),
            Padding = new Padding(32),
            Font = new Font("Segoe UI", 14F)
        };

        ThemeManager.ApplyDarkModeToForm(dialog);
        dialog.BackColor = DarkModeColors.ActiveTabBackgroundColor;

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text =
                "Já existe uma estrutura com este nome no local escolhido:\n\n" +
                caminhoEstrutura + "\n\n" +
                "Nenhuma subpasta ou capa existente será apagada ou substituída.\n" +
                "Clique em \"Adicionar novos\" para criar apenas as subpastas e capas que ainda não existem, " +
                "ou em \"Cancelar\" para não fazer nenhuma modificação na estrutura existente.",
            ForeColor = DarkModeColors.TextColor,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false,
            Font = new Font("Segoe UI", 14F)
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 116,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = DarkModeColors.ActiveTabBackgroundColor,
            Padding = new Padding(0, 24, 0, 0)
        };

        var addButton = new Button
        {
            Text = "Adicionar novos",
            Size = new Size(280, 68),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };
        addButton.Click += (_, _) =>
        {
            decisao = DecisaoEstruturaExistente.AdicionarNovos;
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        var cancelButton = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Size = new Size(200, 68),
            Margin = new Padding(16, 0, 0, 0),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold)
        };

        ThemeManager.ApplyDarkModeToControl(addButton);
        ThemeManager.ApplyDarkModeToControl(cancelButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(addButton);

        dialog.Controls.Add(label);
        dialog.Controls.Add(buttons);
        dialog.AcceptButton = addButton;
        dialog.CancelButton = cancelButton;

        var owner = Form.ActiveForm;
        if (owner is null)
            dialog.ShowDialog();
        else
            dialog.ShowDialog(owner);
        return decisao;
    }
}
