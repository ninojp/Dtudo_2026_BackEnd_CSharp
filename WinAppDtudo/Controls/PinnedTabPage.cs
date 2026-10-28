using System.ComponentModel;

namespace WinAppDtudo.Controls;

/// <summary>
/// Aba fixa do <see cref="DarkTabControl"/>: nao exibe botao de fechar, nao pode ser
/// reordenada e permanece sempre a primeira aba (por exemplo, a aba Inicio).
/// </summary>
public sealed class PinnedTabPage : TabPage
{
    private Color _surfaceColor;

    /// <summary>
    /// Cor de fundo propria da pagina (por exemplo, preto na aba Inicio). Quando vazia, usa a cor padrao das abas.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SurfaceColor
    {
        get => _surfaceColor;
        set
        {
            _surfaceColor = value;
            BackColor = DarkTabControl.ResolvePageSurfaceColor(this);
        }
    }
}
