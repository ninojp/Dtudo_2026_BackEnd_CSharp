using System.Net;
using System.Text.Json;
using ApiMyAnimes.Configuration;
using LibDtudo.Shared.Dtos.FileMonitoring;
using Microsoft.Extensions.Options;

namespace ApiMyAnimes.Services;

/// <summary>Falha ao consultar a ApiFileStorage; a mensagem nunca contem caminho fisico.</summary>
public sealed class AnimeFolderClientException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>Cliente servico-a-servico das leituras de pasta de anime da ApiFileStorage.</summary>
public interface IAnimeFolderScanClient
{
    /// <summary>Ha URL HTTPS configurada para a ApiFileStorage.</summary>
    bool IsConfigured { get; }

    /// <exception cref="AnimeFolderClientException">Falha de rede, autenticacao, tempo limite ou resposta invalida.</exception>
    Task<AnimeFolderDiscoveryResponse> DiscoverAsync(AnimeFolderDiscoveryRequest request, CancellationToken cancellationToken);

    /// <exception cref="AnimeFolderClientException">Falha de rede, autenticacao, tempo limite ou resposta invalida.</exception>
    Task<AnimeFolderScanResponse> ScanAsync(AnimeFolderScanRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Implementacao HTTP: <c>POST monitoring/anime-folders/discovery</c> e <c>POST monitoring/anime-folders/scan</c> com token de
/// servico (client credentials + mTLS), configurados em <c>AddAnimeFolderScanClient</c>.
/// </summary>
public sealed class AnimeFolderScanClient(HttpClient httpClient, IOptions<ApiFileStorageOptions> options) : IAnimeFolderScanClient
{
    private const string DiscoveryPath = "api/file-storage/monitoring/anime-folders/discovery";
    private const string ScanPath = "api/file-storage/monitoring/anime-folders/scan";

    public bool IsConfigured => options.Value.IsConfigured;

    public Task<AnimeFolderDiscoveryResponse> DiscoverAsync(AnimeFolderDiscoveryRequest request, CancellationToken cancellationToken) =>
        PostAsync<AnimeFolderDiscoveryRequest, AnimeFolderDiscoveryResponse>(DiscoveryPath, request, cancellationToken);

    public Task<AnimeFolderScanResponse> ScanAsync(AnimeFolderScanRequest request, CancellationToken cancellationToken) =>
        PostAsync<AnimeFolderScanRequest, AnimeFolderScanResponse>(ScanPath, request, cancellationToken);

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken)
        where TResponse : class
    {
        if (!IsConfigured) throw new AnimeFolderClientException("ApiFileStorage nao configurada.");
        try
        {
            using var response = await httpClient.PostAsJsonAsync(path, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new AnimeFolderClientException($"A ApiFileStorage respondeu HTTP {(int)response.StatusCode}.");
            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken)
                ?? throw new AnimeFolderClientException("A ApiFileStorage devolveu uma resposta vazia.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AnimeFolderClientException("Tempo limite excedido ao consultar a ApiFileStorage.");
        }
        catch (HttpRequestException exception)
        {
            throw new AnimeFolderClientException(
                exception.StatusCode is HttpStatusCode status
                    ? $"A ApiFileStorage respondeu HTTP {(int)status}."
                    : "Falha de comunicacao com a ApiFileStorage.", exception);
        }
        catch (InvalidOperationException exception)
        {
            // Autenticacao de servico desabilitada ou sem certificado: mensagens proprias, sem dados sensiveis.
            throw new AnimeFolderClientException(exception.Message, exception);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new AnimeFolderClientException("Resposta invalida da ApiFileStorage.", exception);
        }
    }
}
