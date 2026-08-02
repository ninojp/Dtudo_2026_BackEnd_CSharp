using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public partial class Frm_Questao : Form
{
    public Frm_Questao(string nomeImagem="RadioAtivo", string mensagem="Texto padrão")
    {
        InitializeComponent();
        //Image imagemTemp = (Image)global::WinAppControlStore.Properties.Resources.ResourceManager.GetObject(nomeImagem);
        Image? imagemTemp = (Image?)Properties.Resources.ResourceManager.GetObject(nomeImagem);
        Pic_PictureBox.Image = imagemTemp;
        Lbl_TextoDaCaixa.Text = mensagem;
        ThemeManager.ApplyDarkModeToForm(this);
        Scale(new SizeF(2F, 2F));
        Font = new Font(Font.FontFamily, Font.Size * 1.4F, Font.Style);
        StartPosition = FormStartPosition.CenterParent;
    }

    private void Btn_Continue_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void Btn_Pare_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}
