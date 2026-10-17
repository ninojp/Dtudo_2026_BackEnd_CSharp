using System.ComponentModel;
using WinAppDtudo.Services;

namespace WinAppDtudo.Controls;

/// <summary>
/// Rótulo com destaque próprio. O tema usa o <see cref="IThemeAccentProvider"/> mais próximo do controle,
/// então este rótulo mantém <see cref="AccentColor"/> em vez do destaque da página onde está.
/// Serve para elementos fixos (legendas, títulos de seção) que não seguem a cor da fonte dos dados.
/// </summary>
public sealed class AccentLabel : IconTextLabel, IThemeAccentProvider
{
    private Color _accentColor = DarkModeColors.TextColor;

    public AccentLabel()
    {
        ForeColor = _accentColor;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor == value)
                return;

            _accentColor = value;
            ForeColor = value;
        }
    }
}
