using WinAppDtudo.Services;

namespace WinAppDtudo.Forms;

public partial class Frm_CadastrarUsuario : Form
{
    private readonly AuthApiService _authApiService = new();

    public Frm_CadastrarUsuario()
    {
        InitializeComponent();
        // Aplicar o tema Dark Mode ao formulário e seus componentes
        ThemeManager.ApplyDarkModeToForm(this);
        Txb_Senha.UseSystemPasswordChar = true;
        Btn_Cadastrar.Click += Btn_Cadastrar_Click;
    }

    private void Btn_Cancelar_Click(object sender, EventArgs e)
    {
        Close();
    }

    private async void Btn_Cadastrar_Click(object? sender, EventArgs e)
    {
        var login = Txb_Login.Text.Trim();
        var senha = Txb_Senha.Text;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            WinAppDtudo.Services.DarkMessageBox.Show("Informe login e senha.", "Cadastro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Btn_Cadastrar.Enabled = false;
        try
        {
            var response = await _authApiService.RegisterAsync(login, login, senha);
            if (response.Success)
            {
                WinAppDtudo.Services.DarkMessageBox.Show("Usuario cadastrado com sucesso.", "Cadastro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            else
            {
                WinAppDtudo.Services.DarkMessageBox.Show(response.Message ?? "Nao foi possivel cadastrar o usuario.", "Cadastro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (HttpRequestException ex)
        {
            WinAppDtudo.Services.DarkMessageBox.Show($"Nao foi possivel conectar a ApiMyAnimes em {AppConfigurationService.ApiMyAnimesBaseUrl}.\n\n{ex.Message}", "Erro de conexao", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Btn_Cadastrar.Enabled = true;
        }
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
