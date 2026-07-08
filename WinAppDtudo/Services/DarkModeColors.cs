namespace WinAppDtudo.Services;

/// <summary>
/// Define a paleta de cores para o tema Dark Mode, seguindo o padrão do Windows 11.
/// </summary>
public static class DarkModeColors
{
    // Cores principais do tema Dark
    /// <summary>Cor de fundo principal dos formulários e painéis</summary>
    public static Color BackgroundColor { get; } = Color.FromArgb(32, 32, 32);

    /// <summary>Cor de fundo secundária para controles e panels</summary>
    public static Color BackgroundSecondaryColor { get; } = Color.FromArgb(45, 45, 45);

    /// <summary>Cor do texto principal</summary>
    //(era, 229, 229, 229) agora gold, RGB:(255, 215, 0)
    public static Color TextColor { get; } = Color.FromArgb(255, 215, 0);

    /// <summary>Cor do texto secundário ou desabilitado</summary>
    // (era, (155, 155, 155)) agora goldenrod, RGB:(218, 165, 32)
    public static Color TextSecondaryColor { get; } = Color.FromArgb(218, 165, 32);

    /// <summary>Cor de borda dos controles</summary>
    // (era, (70, 70, 70)) agora agora goldenrod, RGB:(218, 165, 32)
    public static Color BorderColor { get; } = Color.FromArgb(218, 165, 32);

    /// <summary>Cor de destaque/hover</summary>
    //(era, (0, 120, 215)) atualizado para evitar transparência em botões
    public static Color AccentColor { get; } = Color.FromArgb(35, 40, 90);

    /// <summary>Cor de fundo para itens selecionados</summary>
    // (era, (0, 120, 215)) agora transparente, RGB:(0, 0, 0, 0)
    // (era, (0, 120, 215)) agora Orange1, RGB:(255, 165, 0)
    public static Color SelectionColor { get; } = Color.FromArgb(255, 165, 0);

    /// <summary>Cor para elementos desabilitados</summary>
    // (era, (80, 80, 80)) agora DarkGoldenrod, RGB:(184, 134, 11)
    public static Color DisabledColor { get; } = Color.FromArgb(184, 134, 11);

    /// <summary>Cor de sucesso (verde)</summary>
    public static Color SuccessColor { get; } = Color.FromArgb(16, 176, 112);

    /// <summary>Cor de erro (vermelho)</summary>
    public static Color ErrorColor { get; } = Color.FromArgb(240, 76, 76);

    /// <summary>Cor de aviso (laranja)</summary>
    public static Color WarningColor { get; } = Color.FromArgb(255, 159, 64);

    /// <summary>Cor de informação (azul)</summary>
    public static Color InfoColor { get; } = Color.FromArgb(0, 150, 200);
}
