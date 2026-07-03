using WinAppDtudo.Services;

namespace WinAppDtudo.Helpers;

/// <summary>
/// Classe helper para aplicar facilmente o tema Dark Mode aos formulários.
/// Use esta classe como base para todos os formulários da aplicação.
/// </summary>
public abstract class BaseFormDarkMode : Form
{
    /// <summary>
    /// Construtor que aplica automaticamente o tema Dark Mode ao formulário.
    /// </summary>
    protected BaseFormDarkMode()
    {
        InitializeTheming();
    }

    /// <summary>
    /// Inicializa e aplica o tema Dark Mode ao formulário.
    /// Chamado automaticamente no construtor.
    /// </summary>
    protected void InitializeTheming()
    {
        ThemeManager.ApplyDarkModeToForm(this);
    }
}

/// <summary>
/// Classe helper para aplicar facilmente o tema Dark Mode aos UserControls.
/// Use esta classe como base para todos os UserControls da aplicação.
/// </summary>
public abstract class BaseUserControlDarkMode : UserControl
{
    /// <summary>
    /// Construtor que aplica automaticamente o tema Dark Mode ao UserControl.
    /// </summary>
    protected BaseUserControlDarkMode()
    {
        InitializeTheming();
    }

    /// <summary>
    /// Inicializa e aplica o tema Dark Mode ao UserControl.
    /// Chamado automaticamente no construtor.
    /// </summary>
    protected void InitializeTheming()
    {
        ThemeManager.ApplyDarkModeToUserControl(this);
    }
}
