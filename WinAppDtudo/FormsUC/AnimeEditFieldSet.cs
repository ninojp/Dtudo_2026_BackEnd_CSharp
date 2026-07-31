using System.Globalization;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

internal sealed class AnimeEditFieldSet
{
    private readonly Dictionary<string, TextBox> _textFields = [];
    private readonly Dictionary<string, CheckBox> _checkFields = [];

    private Panel? _panel;
    private int _yOffset;
    private int _column;
    private int _rowHeight;
    private Control? _lastLabel;
    private Control? _lastEditor;

    public void Begin(Panel panel)
    {
        _panel = panel;
        _textFields.Clear();
        _checkFields.Clear();
        _yOffset = 10;
        _column = 0;
        _rowHeight = 0;
        _lastLabel = null;
        _lastEditor = null;
    }

    public TextBox AddText(string key, string label, string? value, bool required = false, bool readOnly = false)
    {
        var editor = CreateTextBox(value, multiline: false, readOnly);
        AddPair(label, editor, required);
        _textFields[key] = editor;
        return editor;
    }

    public TextBox AddCombo(string key, string label, string? value, IReadOnlyList<string> options)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Font = new Font("Segoe UI", 10F),
            BackColor = DarkModeColors.BackgroundColor,
            ForeColor = DarkModeColors.TextColor,
            IntegralHeight = false
        };
        combo.Items.AddRange(options.Cast<object>().ToArray());
        combo.Text = value ?? string.Empty;
        AddPair(label, combo);

        var mirror = CreateTextBox(value, multiline: false, readOnly: false);
        combo.TextChanged += (_, _) => mirror.Text = combo.Text;
        _textFields[key] = mirror;
        return mirror;
    }

    public TextBox AddList(string key, string label, IEnumerable<string>? values)
    {
        var editor = CreateTextBox(string.Join(Environment.NewLine, values ?? []), multiline: true, readOnly: false);
        AddWide(label, editor);
        _textFields[key] = editor;
        return editor;
    }

    public TextBox AddLongText(string key, string label, string? value)
    {
        var editor = CreateTextBox(value, multiline: true, readOnly: false);
        editor.Height = 130;
        AddWide(label, editor);
        _textFields[key] = editor;
        return editor;
    }

    public CheckBox AddBool(string key, string label, bool value)
    {
        var editor = new CheckBox
        {
            Checked = value,
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = DarkModeColors.TextColor,
            BackColor = DarkModeColors.BackgroundSecondaryColor
        };
        AddPair(label, editor);
        _checkFields[key] = editor;
        return editor;
    }

    public void Finish()
    {
        FinishRow();
        if (_panel is not null)
            _panel.AutoScrollMinSize = new Size(0, _yOffset + 20);
    }

    public string Text(string key) => _textFields.TryGetValue(key, out var field) ? field.Text.Trim() : string.Empty;

    public string? OptionalText(string key)
    {
        var value = Text(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public bool Bool(string key) => _checkFields.TryGetValue(key, out var field) && field.Checked;

    public List<string> List(string key)
    {
        return Text(key)
            .Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool TryRequiredString(string key, string label, List<string> errors, out string value)
    {
        value = Text(key);
        if (!string.IsNullOrWhiteSpace(value))
            return true;

        errors.Add($"{label} e obrigatorio.");
        return false;
    }

    public bool TryRequiredInt(string key, string label, int min, int max, List<string> errors, out int value)
    {
        var text = Text(key);
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value)
            && value >= min
            && value <= max)
        {
            return true;
        }

        errors.Add($"{label} deve ser um numero inteiro entre {min} e {max}.");
        return false;
    }

    public int? OptionalInt(string key, string label, int? min, int? max, List<string> errors)
    {
        var text = Text(key);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value))
        {
            errors.Add($"{label} deve ser um numero inteiro.");
            return null;
        }

        if (min.HasValue && value < min.Value)
            errors.Add($"{label} deve ser maior ou igual a {min.Value}.");
        if (max.HasValue && value > max.Value)
            errors.Add($"{label} deve ser menor ou igual a {max.Value}.");

        return value;
    }

    public double? OptionalDouble(string key, string label, double? min, double? max, List<string> errors)
    {
        var text = Text(key);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var styles = NumberStyles.Float | NumberStyles.AllowThousands;
        if (!double.TryParse(text, styles, CultureInfo.CurrentCulture, out var value)
            && !double.TryParse(text, styles, CultureInfo.InvariantCulture, out value))
        {
            errors.Add($"{label} deve ser um numero decimal valido.");
            return null;
        }

        if (min.HasValue && value < min.Value)
            errors.Add($"{label} deve ser maior ou igual a {min.Value}.");
        if (max.HasValue && value > max.Value)
            errors.Add($"{label} deve ser menor ou igual a {max.Value}.");

        return value;
    }

    private void AddPair(string label, Control editor, bool required = false)
    {
        var panel = RequirePanel();
        var valueWidth = GetValueWidth(panel);
        var columnWidth = valueWidth + 160;
        var x = 4 + (_column * columnWidth);
        var editorHeight = Math.Max(34, editor.Height);

        var labelControl = CreateLabel(label, required);
        labelControl.Location = new Point(x, _yOffset + 2);
        labelControl.Size = new Size(150, editorHeight);
        editor.Location = new Point(x + 154, _yOffset + 2);
        editor.Size = new Size(valueWidth, editorHeight);

        panel.Controls.Add(labelControl);
        panel.Controls.Add(editor);

        if (_column == 0)
        {
            _lastLabel = labelControl;
            _lastEditor = editor;
            _rowHeight = editorHeight;
            _column = 1;
        }
        else
        {
            _rowHeight = Math.Max(_rowHeight, editorHeight);
            if (_lastLabel is not null) _lastLabel.Height = _rowHeight;
            if (_lastEditor is not null) _lastEditor.Height = _rowHeight;
            labelControl.Height = _rowHeight;
            editor.Height = _rowHeight;
            _yOffset += _rowHeight + 12;
            _column = 0;
            _rowHeight = 0;
            _lastLabel = null;
            _lastEditor = null;
        }
    }

    private void AddWide(string label, TextBox editor)
    {
        var panel = RequirePanel();
        FinishRow();
        _yOffset += 12;

        var valueWidth = GetValueWidth(panel);
        var sectionWidth = Math.Max(2 * valueWidth + 148, 450);

        var labelControl = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = DarkModeColors.TextColor,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            Location = new Point(4, _yOffset),
            Size = new Size(sectionWidth, 32),
            Text = label + ":",
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(labelControl);

        editor.Location = new Point(4, labelControl.Bottom + 8);
        editor.Size = new Size(sectionWidth, editor.Height);
        panel.Controls.Add(editor);

        _yOffset = editor.Bottom + 14;
    }

    private void FinishRow()
    {
        if (_column == 0) return;

        _yOffset += _rowHeight + 12;
        _column = 0;
        _rowHeight = 0;
        _lastLabel = null;
        _lastEditor = null;
    }

    private Panel RequirePanel()
        => _panel ?? throw new InvalidOperationException("AnimeEditFieldSet.Begin deve ser chamado antes de adicionar campos.");

    private static int GetValueWidth(Panel panel)
    {
        var columnWidth = Math.Max((panel.ClientSize.Width - 20) / 2, 350);
        return Math.Max(columnWidth - 160, 180);
    }

    private static Label CreateLabel(string label, bool required)
    {
        return new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = DarkModeColors.TextColor,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            Text = required ? $"{label} *:" : $"{label}:",
            TextAlign = ContentAlignment.MiddleRight
        };
    }

    private static TextBox CreateTextBox(string? value, bool multiline, bool readOnly)
    {
        return new TextBox
        {
            Text = value ?? string.Empty,
            Font = new Font("Segoe UI", 10F),
            AutoSize = !multiline,
            Multiline = multiline,
            Height = multiline ? 85 : 34,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
            ReadOnly = readOnly,
            BackColor = readOnly ? DarkModeColors.BackgroundSecondaryColor : DarkModeColors.BackgroundColor,
            ForeColor = DarkModeColors.TextColor,
            BorderStyle = BorderStyle.FixedSingle
        };
    }
}
