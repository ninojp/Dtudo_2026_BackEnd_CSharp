using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinAppDtudo.Forms;

public partial class Frm_MyMusicX : Form
{
    public Frm_MyMusicX()
    {
        InitializeComponent();
    }
    //Menu Windows - Exibir janelas em cascata.
    private void CascataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.LayoutMdi(MdiLayout.Cascade);
    }
    //Menu Windows - Exibir janelas na horizontal.
    private void HorizontalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.LayoutMdi(MdiLayout.TileHorizontal);
    }
    //Menu Windows - Exibir janelas em vertical.
    private void VerticalToolStripMenuItem_Click(object sender, EventArgs e)
    {
        this.LayoutMdi(MdiLayout.TileVertical);
    }

    private void FormTestToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Frm_FormTest formTest = new();
        formTest.MdiParent = this;
        formTest.Show();
    }

    private void FormHelloWorldToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Frm_HelloWorld formHelloWorld = new();
        formHelloWorld.MdiParent = this;
        formHelloWorld.Show();
    }
}
