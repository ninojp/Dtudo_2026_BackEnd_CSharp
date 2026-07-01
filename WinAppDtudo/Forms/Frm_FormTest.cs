using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinAppDtudo;

public partial class Frm_FormTest : Form
{
    public Frm_FormTest()
    {
        InitializeComponent();
    }

    private void Btn_Voltar_Click(object sender, EventArgs e)
    {
        Frm_WinAppDtudo formPrincipal = new Frm_WinAppDtudo();
        formPrincipal.ShowDialog();
    }
}
