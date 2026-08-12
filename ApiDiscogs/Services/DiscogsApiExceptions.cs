namespace ApiDiscogs.Services;

/// <summary>
/// Indica que o payload externo nao atende ao contrato minimo esperado.
/// </summary>
public sealed class DiscogsInvalidResponseException(string message)
    : InvalidOperationException(message);

/// <summary>
/// Indica que uma entrada recebida pelo endpoint nao atende aos limites do contrato.
/// </summary>
public sealed class DiscogsValidationException(string message)
    : ArgumentException(message);
