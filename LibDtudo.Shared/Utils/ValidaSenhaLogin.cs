namespace LibDtudo.Shared.Utils;

/// <summary>
/// Utilitario legado para validar senha a partir de um hash armazenado.
/// </summary>
public class ValidaSenhaLogin
{
    /// <summary>Valida a senha informada contra um hash PBKDF2.</summary>
    public static bool ValidarSenhaDoLogin(string senha, string senhaHash)
        => PasswordHasher.VerifyPassword(senha, senhaHash);
}
