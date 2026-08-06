namespace ApiMyAnimes.Configuration;

/// <summary>Configuracao da autenticacao local.</summary>
public sealed class AuthOptions
{
    /// <summary>Nome da secao de configuracao.</summary>
    public const string SectionName = "Auth";

    /// <summary>Caminho relativo ou absoluto do arquivo local de usuarios.</summary>
    public string UsersFilePath { get; set; } = string.Empty;
}
