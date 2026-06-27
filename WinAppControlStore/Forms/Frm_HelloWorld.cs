namespace WinAppControlStore.Forms;

public partial class Frm_HelloWorld : Form
{
    public Frm_HelloWorld()
    {
        InitializeComponent();
    }

    private void Btn_Modifica_Label_Click(object sender, EventArgs e)
    {
        Lbl_Titulo.Text = Txb_Texto_Temp.Text;
    }
}
