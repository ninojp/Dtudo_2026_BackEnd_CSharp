namespace ApiCSharp.Shared.Utils;

/// <summary>
/// Classe responsável por validar a senha do login.
/// </summary>
public class ValidaSenhaLogin
{
    public static bool ValidarSenhaDoLogin(string login, string senha)
    {
        if (senha == "senha123" && login == "NinoJP")
        {
            return true;
        }
        return false;
    }
}
