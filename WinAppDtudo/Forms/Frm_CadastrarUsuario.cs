using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinAppDtudo.Forms;

public partial class Frm_CadastrarUsuario : Form
{
    public Frm_CadastrarUsuario()
    {
        InitializeComponent();
    }

    private void Btn_Cancelar_Click(object sender, EventArgs e)
    {

    }

    private void Btn_ImagemPerfil_Click(object sender, EventArgs e)
    {
        OpenFileDialog imgTemp = new OpenFileDialog();
        imgTemp.InitialDirectory = "C:\\Users\\comer\\OneDrive\\Imagens";
        imgTemp.Filter = "Arquivos de Imagem (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Todos os Arquivos (*.*)|*.*";
        imgTemp.Title = "Selecione uma Imagem de Perfil";
        imgTemp.FilterIndex = 0;

        if (imgTemp.ShowDialog() == DialogResult.OK)
        {
            Pic_PictureBoxImgTemp.Image = Image.FromFile(imgTemp.FileName);
            Lbl_EnderecoImagem.Text = imgTemp.FileName;
        }
    }

    private void Btn_CorDialogBox_Click(object sender, EventArgs e)
    {
        ColorDialog tempColor = new();
        if (tempColor.ShowDialog() == DialogResult.OK)
        {
            this.BackColor = tempColor.Color;
            this.ForeColor = Color.Gold;
        }
    }

    private void Btn_FontDialogBox_Click(object sender, EventArgs e)
    {
        FontDialog tempFont = new();
        if (tempFont.ShowDialog() == DialogResult.OK)
        {
            this.Font = tempFont.Font;
        }
    }
}
