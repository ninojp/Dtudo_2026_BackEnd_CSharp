using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WinAppDtudo.Services;

namespace WinAppDtudo.FormsUC;

public partial class FUC_CadastrarUsuario : UserControl
{
    public FUC_CadastrarUsuario()
    {
        InitializeComponent();
        ThemeManager.ApplyDarkModeToUserControl(this);
    }
}
