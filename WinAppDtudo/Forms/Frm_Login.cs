using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public partial class Frm_Login : Form
{
    public string Login
    {
        get { return Txb_Login.Text; }
    }
    public string Senha
    {
        get { return Txb_Senha.Text; }
    }
    public Frm_Login()
    {
        InitializeComponent();

        // Aplicar o tema Dark Mode
        ThemeManager.ApplyDarkModeToForm(this);

        //Carregar os textos dos controles (componentes), após a inicialização do formulário.
        Lbl_NomeLabel.Text = "Nome do Usuário:";
        Lbl_SenhaLabel.Text = "Senha do Usuário:";
        Btn_Login.Text = "Login";
        Btn_Cancelar.Text = "Cancelar";
    }

    private void Btn_Login_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void Btn_Cancelar_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }
}
