using System.Net;
using System.Net.Http.Json;
using ApiIdentity.Configuration;
using LibDtudo.Shared.Dtos;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Privacy;

/// <summary>
/// A ApiMyAnimes não pôde atender a chamada de privacidade (indisponível, timeout, rejeição ou resposta inválida).
/// <see cref="Code"/> é um código curto e estável, sem dado pessoal, usado em auditoria, log e <c>LastError</c>.
/// </summary>
public sealed class MyAnimesPrivacyUnavailableException(string code, Exception? innerException = null)
    : Exception($"ApiMyAnimes indisponivel para a operacao de privacidade ({code}).", innerException)
{
    public string Code { get; } = code;
}

/// <summary>Cliente das rotas internas de privacidade da ApiMyAnimes (servico-a-servico).</summary>
public interface IMyAnimesPrivacyClient
{
    /// <summary>Exporta os dados pessoais da conta. Falhas viram <see cref="MyAnimesPrivacyUnavailableException"/>.</summary>
    Task<MyAnimesPrivacyExportDto> ExportAsync(string accountId, CancellationToken cancellationToken);

    /// <summary>Apaga (idempotente) os dados pessoais da conta. Falhas viram <see cref="MyAnimesPrivacyUnavailableException"/>.</summary>
    Task<MyAnimesPrivacyEraseResultDto> EraseAsync(string accountId, CancellationToken cancellationToken);
}

/// <summary>
/// Implementação com timeout por tentativa e retry limitado (backoff exponencial) apenas para falhas transitórias
/// (rede, timeout, 408, 429 e 5xx). Respostas 4xx definitivas não são repetidas. O cancelamento do chamador nunca é engolido.
/// </summary>
public sealed class MyAnimesPrivacyClient(
    HttpClient httpClient,
    IOptions<MyAnimesPrivacyClientOptions> options,
    TimeProvider timeProvider) : IMyAnimesPrivacyClient
{
    public Task<MyAnimesPrivacyExportDto> ExportAsync(string accountId, CancellationToken cancellationToken) =>
        PostAsync<MyAnimesPrivacyExportDto>(MyAnimesPrivacyContract.ExportRoute, accountId, cancellationToken);

    public Task<MyAnimesPrivacyEraseResultDto> EraseAsync(string accountId, CancellationToken cancellationToken) =>
        PostAsync<MyAnimesPrivacyEraseResultDto>(MyAnimesPrivacyContract.EraseRoute, accountId, cancellationToken);

    private async Task<TResponse> PostAsync<TResponse>(string route, string accountId, CancellationToken cancellationToken)
        where TResponse : class
    {
        var settings = options.Value;
        if (!settings.TryGetBaseUri(out _) || httpClient.BaseAddress is null)
        {
            throw new MyAnimesPrivacyUnavailableException("not-configured");
        }

        if (!MyAnimesPrivacyContract.IsValidAccountId(accountId))
        {
            throw new MyAnimesPrivacyUnavailableException("invalid-account-id");
        }

        string failureCode = "unknown";
        Exception? failure = null;
        for (var attempt = 1; attempt <= settings.MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var response = await httpClient.PostAsJsonAsync(
                    route,
                    new MyAnimesPrivacyAccountRequest { AccountId = accountId },
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken)
                        ?? throw new MyAnimesPrivacyUnavailableException("invalid-response");
                }

                failureCode = $"http-{(int)response.StatusCode}";
                if (!IsTransient(response.StatusCode))
                {
                    throw new MyAnimesPrivacyUnavailableException(failureCode);
                }
            }
            catch (MyAnimesPrivacyUnavailableException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException exception)
            {
                failureCode = "timeout";
                failure = exception;
            }
            catch (HttpRequestException exception)
            {
                failureCode = exception.StatusCode is { } status ? $"token-http-{(int)status}" : "network";
                failure = exception;
            }
            catch (InvalidOperationException exception)
            {
                // Autenticacao de servico desabilitada/sem certificado: configuracao, nao adianta repetir.
                throw new MyAnimesPrivacyUnavailableException("service-auth-unavailable", exception);
            }
            catch (System.Text.Json.JsonException exception)
            {
                throw new MyAnimesPrivacyUnavailableException("invalid-response", exception);
            }

            if (attempt < settings.MaxAttempts)
            {
                var delay = TimeSpan.FromMilliseconds(settings.RetryBaseDelayMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, timeProvider, cancellationToken);
            }
        }

        throw new MyAnimesPrivacyUnavailableException(failureCode, failure);
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
        || (int)statusCode >= 500;
}
