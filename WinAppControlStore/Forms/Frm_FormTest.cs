using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinAppControlStore
{
    public partial class Frm_FormTest : Form
    {
        public Frm_FormTest()
        {
            InitializeComponent();
        }

        private void Btn_Voltar_Click(object sender, EventArgs e)
        {
            Frm_WinAppControlStore formPrincipal = new Frm_WinAppControlStore();
            formPrincipal.ShowDialog();
        }
    }
}
