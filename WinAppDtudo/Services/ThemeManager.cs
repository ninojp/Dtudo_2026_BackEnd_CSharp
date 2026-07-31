using Microsoft.Win32;
using WinAppDtudo.Controls;

namespace WinAppDtudo.Services;

/// <summary>
/// Gerenciador centralizado de temas (Dark Mode / Light Mode).
/// Detecta automaticamente as preferências do Windows 11 e aplica o tema aos formulários.
/// </summary>
public static class ThemeManager
{
    private static bool _isDarkModeEnabled = false;

    /// <summary>
    /// Inicializa o gerenciador de temas e detecta as preferências do Windows 11.
    /// Deve ser chamado uma única vez no Program.cs durante a inicialização do aplicativo.
    /// </summary>
    public static void Initialize()
    {
        _isDarkModeEnabled = IsWindowsDarkModeEnabled();
    }

    /// <summary>
    /// Obtém o status atual do Dark Mode.
    /// </summary>
    public static bool IsDarkModeEnabled => _isDarkModeEnabled;

    /// <summary>
    /// Detecta se o Windows 11 está configurado para o tema Dark Mode.
    /// </summary>
    private static bool IsWindowsDarkModeEnabled()
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key != null)
                {
                    object? value = key.GetValue("AppsUseLightTheme");
                    if (value != null && int.TryParse(value.ToString(), out int result))
                    {
                        // AppsUseLightTheme = 0 significa Dark Mode
                        // AppsUseLightTheme = 1 significa Light Mode
                        return result == 0;
                    }
                }
            }
        }
        catch
        {
            // Em caso de erro ao ler o registro, retorna true (assume Dark Mode como padrão)
            return true;
        }

        return true; // Dark Mode como padrão
    }

    /// <summary>
    /// Aplica o tema Dark Mode a um formulário e todos os seus componentes filhos.
    /// </summary>
    /// <param name="form">O formulário a receber o tema.</param>
    public static void ApplyDarkModeToForm(Form form)
    {
        if (!_isDarkModeEnabled)
            return;

        // Aplicar ao formulário
        form.BackColor = DarkModeColors.BackgroundColor;
        form.ForeColor = DarkModeColors.TextColor;

        // Aplicar recursivamente a todos os controles filhos
        ApplyDarkModeToControls(form.Controls);
    }

    /// <summary>
    /// Aplica o tema Dark Mode a uma coleção de controles e seus filhos recursivamente.
    /// </summary>
    private static void ApplyDarkModeToControls(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            ApplyDarkModeToControl(control);

            // Recursivamente aplica aos controles filhos
            if (control.HasChildren)
            {
                ApplyDarkModeToControls(control.Controls);
            }
        }
    }

    /// <summary>
    /// Aplica o tema Dark Mode a um controle específico baseado em seu tipo.
    /// </summary>
    private static void ApplyDarkModeToControl(Control control)
    {
        // MenuStrip
        if (control is MenuStrip menuStrip)
        {
            menuStrip.BackColor = DarkModeColors.BackgroundSecondaryColor;
            menuStrip.ForeColor = DarkModeColors.TextColor;
            return;
        }

        // ToolStrip
        if (control is ToolStrip toolStrip)
        {
            toolStrip.BackColor = DarkModeColors.BackgroundSecondaryColor;
            toolStrip.ForeColor = DarkModeColors.TextColor;
            return;
        }

        // StatusStrip
        if (control is StatusStrip statusStrip)
        {
            statusStrip.BackColor = DarkModeColors.BackgroundSecondaryColor;
            statusStrip.ForeColor = DarkModeColors.TextColor;
            return;
        }

        // Panel / GroupBox
        if (control is Panel or GroupBox)
        {
            control.BackColor = DarkModeColors.BackgroundSecondaryColor;
            control.ForeColor = DarkModeColors.TextColor;
            return;
        }

        // TextBox / RichTextBox
        if (control is TextBox textBox)
        {
            textBox.BackColor = DarkModeColors.BackgroundColor;
            textBox.ForeColor = DarkModeColors.TextColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            return;
        }

        if (control is RichTextBox richTextBox)
        {
            richTextBox.BackColor = DarkModeColors.BackgroundColor;
            richTextBox.ForeColor = DarkModeColors.TextColor;
            richTextBox.BorderStyle = BorderStyle.FixedSingle;
            return;
        }

        // ComboBox
        if (control is ComboBox comboBox)
        {
            comboBox.BackColor = DarkModeColors.BackgroundColor;
            comboBox.ForeColor = DarkModeColors.TextColor;
            return;
        }

        // CheckBox / RadioButton
        if (control is CheckBox or RadioButton)
        {
            control.BackColor = DarkModeColors.BackgroundSecondaryColor;
            control.ForeColor = DarkModeColors.TextColor;
            return;
        }

        // Button
        if (control is Button button)
        {
            button.BackColor = DarkModeColors.AccentColor;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = DarkModeColors.BorderColor;
            button.FlatAppearance.MouseOverBackColor = DarkModeColors.SelectionColor;
            button.FlatAppearance.MouseDownBackColor = DarkModeColors.SelectionColor;
            if (button.Font.Size < 9.5F)
            {
                button.Font = new Font(button.Font.FontFamily, 9.5F, FontStyle.Bold);
            }
            return;
        }

        // Label
        if (control is Label)
        {
            control.BackColor = DarkModeColors.BackgroundSecondaryColor;
            control.ForeColor = DarkModeColors.TextColor;
            return;
        }

        // ListBox
        if (control is ListBox listBox)
        {
            listBox.BackColor = DarkModeColors.BackgroundColor;
            listBox.ForeColor = DarkModeColors.TextColor;
            return;
        }

        // DataGridView
        if (control is DataGridView dataGridView)
        {
            ApplyDarkModeToDataGridView(dataGridView);
            return;
        }

        // TabControl
        if (control is TabControl tabControl)
        {
            tabControl.BackColor = DarkModeColors.BackgroundSecondaryColor;
            tabControl.ForeColor = DarkModeColors.TextColor;
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                tabPage.BackColor = DarkModeColors.BackgroundColor;
                tabPage.ForeColor = DarkModeColors.TextColor;
            }
            return;
        }

        // Para controles genéricos
        control.BackColor = DarkModeColors.BackgroundSecondaryColor;
        control.ForeColor = DarkModeColors.TextColor;
    }

    /// <summary>
    /// Aplica o tema Dark Mode específico para DataGridView.
    /// </summary>
    private static void ApplyDarkModeToDataGridView(DataGridView dgv)
    {
        dgv.BackgroundColor = DarkModeColors.BackgroundColor;
        dgv.ForeColor = DarkModeColors.TextColor;
        dgv.GridColor = DarkModeColors.BorderColor;
        dgv.DefaultCellStyle.BackColor = DarkModeColors.BackgroundColor;
        dgv.DefaultCellStyle.ForeColor = DarkModeColors.TextColor;
        dgv.DefaultCellStyle.SelectionBackColor = DarkModeColors.SelectionColor;
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

        // Cabeçalho das colunas
        dgv.ColumnHeadersDefaultCellStyle.BackColor = DarkModeColors.BackgroundSecondaryColor;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = DarkModeColors.TextColor;
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = DarkModeColors.SelectionColor;

        // Cabeçalho das linhas
        dgv.RowHeadersDefaultCellStyle.BackColor = DarkModeColors.BackgroundSecondaryColor;
        dgv.RowHeadersDefaultCellStyle.ForeColor = DarkModeColors.TextColor;
        dgv.RowHeadersDefaultCellStyle.SelectionBackColor = DarkModeColors.SelectionColor;

        dgv.EnableHeadersVisualStyles = false;
    }

    /// <summary>
    /// Aplica o tema Dark Mode a um UserControl específico.
    /// </summary>
    /// <param name="userControl">O UserControl a receber o tema.</param>
    public static void ApplyDarkModeToUserControl(UserControl userControl)
    {
        if (!_isDarkModeEnabled)
            return;

        userControl.BackColor = DarkModeColors.BackgroundSecondaryColor;
        userControl.ForeColor = DarkModeColors.TextColor;

        if (userControl.HasChildren)
        {
            ApplyDarkModeToControls(userControl.Controls);
        }
    }

    /// <summary>
    /// Obtém a cor apropriada para o tema atual.
    /// </summary>
    /// <param name="colorType">Tipo de cor desejada.</param>
    public static Color GetThemeColor(ThemeColorType colorType)
    {
        return colorType switch
        {
            ThemeColorType.Background => DarkModeColors.BackgroundColor,
            ThemeColorType.BackgroundSecondary => DarkModeColors.BackgroundSecondaryColor,
            ThemeColorType.Text => DarkModeColors.TextColor,
            ThemeColorType.TextSecondary => DarkModeColors.TextSecondaryColor,
            ThemeColorType.Border => DarkModeColors.BorderColor,
            ThemeColorType.Accent => DarkModeColors.AccentColor,
            ThemeColorType.Success => DarkModeColors.SuccessColor,
            ThemeColorType.Error => DarkModeColors.ErrorColor,
            ThemeColorType.Warning => DarkModeColors.WarningColor,
            ThemeColorType.Info => DarkModeColors.InfoColor,
            _ => DarkModeColors.TextColor,
        };
    }
}

/// <summary>
/// Define os tipos de cores disponíveis no tema.
/// </summary>
public enum ThemeColorType
{
    Background,
    BackgroundSecondary,
    Text,
    TextSecondary,
    Border,
    Accent,
    Success,
    Error,
    Warning,
    Info
}
