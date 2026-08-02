namespace WinAppDtudo.Services;

/// <summary>
/// Paleta oficial do tema escuro do WinAppDtudo.
/// </summary>
public static class DarkModeColors
{
    /// <summary>Fundo absoluto da aplicacao.</summary>
    public static Color BackgroundColor { get; } = Color.Black;

    /// <summary>Superficie baixa, usada em areas internas e paginas.</summary>
    public static Color SurfaceColor { get; } = Color.FromArgb(4, 4, 5);

    /// <summary>Superficie secundaria para paineis, menus e controles.</summary>
    public static Color BackgroundSecondaryColor { get; } = Color.FromArgb(10, 10, 12);

    /// <summary>Superficie elevada para controles ativos e cabecalhos.</summary>
    public static Color ElevatedColor { get; } = Color.FromArgb(18, 18, 22);

    /// <summary>Fundo da aba ativa, incluindo cabecalho e area de conteudo.</summary>
    public static Color ActiveTabBackgroundColor { get; } = Color.FromArgb(28, 28, 34);

    /// <summary>Texto principal.</summary>
    public static Color TextColor { get; } = Color.Gold;

    /// <summary>Texto de abas inativas.</summary>
    public static Color InactiveTabTextColor { get; } = Color.FromArgb(155, 155, 155);

    /// <summary>Texto secundario.</summary>
    public static Color TextSecondaryColor { get; } = Color.Goldenrod;

    /// <summary>Texto e icones desabilitados.</summary>
    public static Color DisabledTextColor { get; } = Color.FromArgb(120, 86, 10);

    /// <summary>Borda padrao.</summary>
    public static Color BorderColor { get; } = Color.FromArgb(74, 55, 8);

    /// <summary>Borda mais forte para selecao e foco.</summary>
    public static Color ActiveBorderColor { get; } = Color.FromArgb(218, 165, 32);

    /// <summary>Destaque principal para botoes e elementos acionaveis.</summary>
    public static Color AccentColor { get; } = Color.FromArgb(35, 40, 90);

    /// <summary>Destaque ao passar o mouse.</summary>
    public static Color HoverColor { get; } = Color.FromArgb(30, 30, 36);

    /// <summary>Fundo de selecao.</summary>
    public static Color SelectionColor { get; } = Color.FromArgb(255, 165, 0);

    /// <summary>Fundo para controles desabilitados.</summary>
    public static Color DisabledColor { get; } = Color.FromArgb(28, 24, 15);

    /// <summary>Cor de sucesso.</summary>
    public static Color SuccessColor { get; } = Color.FromArgb(16, 176, 112);

    /// <summary>Cor de erro.</summary>
    public static Color ErrorColor { get; } = Color.FromArgb(240, 76, 76);

    /// <summary>Cor de aviso.</summary>
    public static Color WarningColor { get; } = Color.FromArgb(255, 159, 64);

    /// <summary>Cor de informacao.</summary>
    public static Color InfoColor { get; } = Color.FromArgb(0, 150, 200);
}
