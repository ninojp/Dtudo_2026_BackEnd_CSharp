namespace ApiMusicX.Services;

/// <summary>
/// Excecao de dominio convertida em uma resposta HTTP controlada.
/// </summary>
public class MusicApiException : Exception
{
    /// <summary>
    /// Cria uma excecao associada ao status HTTP informado.
    /// </summary>
    public MusicApiException(int statusCode, string title, string detail, string? code = null)
        : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
        Code = code;
    }

    /// <summary>
    /// Status HTTP que deve ser devolvido ao consumidor.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Titulo publico do problema.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Codigo estavel opcional para tratamento pelo cliente.
    /// </summary>
    public string? Code { get; }
}

/// <summary>
/// Indica que os dados enviados nao podem ser processados.
/// </summary>
public sealed class MusicValidationException(string detail)
    : MusicApiException(StatusCodes.Status400BadRequest, "Dados invalidos", detail, "music.validation");

/// <summary>
/// Indica que o recurso solicitado nao existe.
/// </summary>
public sealed class MusicNotFoundException(string detail)
    : MusicApiException(StatusCodes.Status404NotFound, "Recurso nao encontrado", detail, "music.not_found");

/// <summary>
/// Indica uma divergencia que exige decisao explicita do operador.
/// </summary>
public sealed class MusicConflictException(string detail)
    : MusicApiException(StatusCodes.Status409Conflict, "Conflito na Colecao local", detail, "music.conflict");
