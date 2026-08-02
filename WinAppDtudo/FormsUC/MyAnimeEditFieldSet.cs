using LibDtudo.Shared.Dtos;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

internal sealed class MyAnimeEditFieldSet
{
    private TextBox? _txtId;
    private TextBox? _txtTitulo;
    private TextBox? _txtMalIds;
    private Label? _lblResumoMalIds;

    public event EventHandler? Changed;

    public void Build(Panel panel, ObterMyAnimeDto myAnime)
    {
        panel.SuspendLayout();
        panel.Controls.Clear();

        var y = 12;
        _txtId = AddText(panel, "ID", myAnime.Id.ToString(), y, readOnly: true);
        y += 54;

        _txtTitulo = AddText(panel, "Titulo", myAnime.Titulo, y, readOnly: false);
        _txtTitulo.MaxLength = 100;
        _txtTitulo.TextChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        y += 64;

        var lblMalIds = CreateLabel("Animes MalId *");
        lblMalIds.Location = new Point(18, y);
        lblMalIds.Size = new Size(220, 32);
        panel.Controls.Add(lblMalIds);

        _txtMalIds = new TextBox
        {
            Location = new Point(18, lblMalIds.Bottom + 8),
            Size = new Size(Math.Max(420, panel.ClientSize.Width - 54), 260),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 10F),
            BackColor = DarkModeColors.BackgroundColor,
            ForeColor = DarkModeColors.TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            Text = string.Join(Environment.NewLine, myAnime.AnimesMalId)
        };
        _txtMalIds.TextChanged += (_, _) =>
        {
            UpdateMalIdsSummary();
            Changed?.Invoke(this, EventArgs.Empty);
        };
        panel.Controls.Add(_txtMalIds);

        _lblResumoMalIds = new Label
        {
            Location = new Point(18, _txtMalIds.Bottom + 10),
            Size = new Size(Math.Max(420, panel.ClientSize.Width - 54), 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.Gold,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(_lblResumoMalIds);

        var help = new Label
        {
            Location = new Point(18, _lblResumoMalIds.Bottom + 8),
            Size = new Size(Math.Max(420, panel.ClientSize.Width - 54), 76),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = DarkModeColors.TextSecondaryColor,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            Text = "Informe um MalId por linha, ou separe por virgula, ponto e virgula ou espaco. Valores repetidos sao removidos ao salvar.",
            TextAlign = ContentAlignment.TopLeft
        };
        panel.Controls.Add(help);

        panel.AutoScrollMinSize = new Size(0, help.Bottom + 20);
        panel.ResumeLayout(true);
        UpdateMalIdsSummary();
    }

    public bool TryCreateDto(List<string> errors, out AtualizaMyAnimeDto dto, out MyAnimeMalIdsParseResult parseResult)
    {
        dto = new AtualizaMyAnimeDto();
        parseResult = ParseMalIds();

        var titulo = _txtTitulo?.Text.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(titulo))
            errors.Add("Titulo e obrigatorio.");
        if (titulo.Length > 100)
            errors.Add("Titulo deve ter no maximo 100 caracteres.");

        errors.AddRange(parseResult.Errors);
        if (parseResult.ValidMalIds.Count == 0)
            errors.Add("Informe pelo menos um MalId valido para a colecao.");

        if (errors.Count > 0)
            return false;

        dto = new AtualizaMyAnimeDto
        {
            Titulo = titulo,
            AnimesMalId = parseResult.ValidMalIds
        };
        return true;
    }

    private TextBox AddText(Panel panel, string label, string value, int y, bool readOnly)
    {
        var lbl = CreateLabel(label + (readOnly ? string.Empty : " *"));
        lbl.Location = new Point(18, y + 2);
        lbl.Size = new Size(160, 34);
        panel.Controls.Add(lbl);

        var txt = new TextBox
        {
            Location = new Point(188, y),
            Size = new Size(Math.Max(320, panel.ClientSize.Width - 224), 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 10F),
            ReadOnly = readOnly,
            BackColor = readOnly ? DarkModeColors.BackgroundSecondaryColor : DarkModeColors.BackgroundColor,
            ForeColor = DarkModeColors.TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            Text = value
        };
        panel.Controls.Add(txt);
        return txt;
    }

    private void UpdateMalIdsSummary()
    {
        if (_lblResumoMalIds is null)
            return;

        var result = ParseMalIds();
        var invalidos = result.InvalidTokens.Count == 0 ? string.Empty : $" | invalidos: {result.InvalidTokens.Count}";
        var duplicados = result.DuplicateMalIds.Count == 0 ? string.Empty : $" | duplicados removidos: {result.DuplicateMalIds.Count}";
        _lblResumoMalIds.Text = $"MalIds validos: {result.ValidMalIds.Count}{duplicados}{invalidos}";
    }

    private MyAnimeMalIdsParseResult ParseMalIds()
    {
        var text = _txtMalIds?.Text ?? string.Empty;
        var tokens = text
            .Split(['\r', '\n', ',', ';', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToList();

        var validos = new List<int>();
        var vistos = new HashSet<int>();
        var duplicados = new HashSet<int>();
        var invalidos = new List<string>();
        var errors = new List<string>();

        foreach (var token in tokens)
        {
            if (!int.TryParse(token, out var malId) || malId <= 0)
            {
                invalidos.Add(token);
                continue;
            }

            if (!vistos.Add(malId))
            {
                duplicados.Add(malId);
                continue;
            }

            validos.Add(malId);
        }

        if (invalidos.Count > 0)
        {
            var amostra = string.Join(", ", invalidos.Take(8));
            errors.Add($"Remova ou corrija os MalIds invalidos: {amostra}.");
        }

        return new MyAnimeMalIdsParseResult(validos, duplicados.OrderBy(id => id).ToList(), invalidos, errors);
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = DarkModeColors.TextColor,
            BackColor = DarkModeColors.BackgroundSecondaryColor,
            Text = text + ":",
            TextAlign = ContentAlignment.MiddleLeft
        };
    }
}

internal sealed record MyAnimeMalIdsParseResult(
    List<int> ValidMalIds,
    List<int> DuplicateMalIds,
    List<string> InvalidTokens,
    List<string> Errors);
