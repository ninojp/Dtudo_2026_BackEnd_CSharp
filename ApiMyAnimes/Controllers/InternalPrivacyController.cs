using ApiMyAnimes.Infrastructure;
using ApiMyAnimes.Services;
using LibDtudo.Shared.Dtos;
using LibDtudo.Shared.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiMyAnimes.Controllers;

/// <summary>
/// Rotas INTERNAS (servico-a-servico) de privacidade por conta, chamadas apenas pela ApiIdentity para a exportacao e a exclusao LGPD.
/// Exigem JWT de client credentials com a permissao/escopo <c>service.privacy.myanimes</c> e o certificado mTLS do mesmo cliente;
/// tokens de usuario recebem <c>403</c>. Nao ha rota no gateway nem CORS para elas. A conta vai no corpo (nunca na URL/log de requisicao).
/// </summary>
/// <param name="privacyService">Servico de privacidade por conta.</param>
/// <param name="logger">Logger do controlador (sem dados pessoais).</param>
[ApiController]
[Route("apiLocal/internal/privacy")]
[Authorize(Policy = InternalServiceAuthorization.PrivacyPolicyName)]
public class InternalPrivacyController(AccountPrivacyService privacyService, ILogger<InternalPrivacyController> logger) : ControllerBase
{
    private const long MaxRequestBodyBytes = 1024;

    /// <summary>Exporta os dados pessoais da conta guardados pela ApiMyAnimes (progresso de episodios e execucoes de atualizacao de pasta).</summary>
    /// <param name="request">Corpo com o <c>accountId</c> (1 a 128 caracteres, sem espacos nem controles).</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <returns><c>200</c> com o export (vazio se a conta nao tem dados), <c>400</c> para conta invalida, <c>401</c>/<c>403</c> sem credencial de servico valida.</returns>
    [HttpPost("export")]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    [ProducesResponseType(typeof(MyAnimesPrivacyExportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Exportar([FromBody] MyAnimesPrivacyAccountRequest? request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(request, out var accountId)) return BadRequest(InvalidAccountProblem());

        Response.Headers.CacheControl = "no-store";
        var export = await privacyService.ExportAsync(accountId, Actor(), CorrelationId(), cancellationToken);
        logger.LogInformation("Exportacao de privacidade concluida para o cliente de servico {ClientId}.", Actor());
        return Ok(export);
    }

    /// <summary>
    /// Apaga o progresso da conta e anonimiza o vinculo com execucoes de atualizacao de pasta. Idempotente: sem dado a apagar
    /// responde <c>200</c> com contagens zeradas (nunca <c>404</c>), para que a ApiIdentity possa repetir a chamada com seguranca.
    /// </summary>
    /// <param name="request">Corpo com o <c>accountId</c> (1 a 128 caracteres, sem espacos nem controles).</param>
    /// <param name="cancellationToken">Cancelamento da requisicao.</param>
    /// <returns><c>200</c> com as contagens, <c>400</c> para conta invalida, <c>401</c>/<c>403</c> sem credencial de servico valida.</returns>
    [HttpPost("erase")]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    [ProducesResponseType(typeof(MyAnimesPrivacyEraseResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Apagar([FromBody] MyAnimesPrivacyAccountRequest? request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(request, out var accountId)) return BadRequest(InvalidAccountProblem());

        Response.Headers.CacheControl = "no-store";
        var result = await privacyService.EraseAsync(accountId, Actor(), CorrelationId(), cancellationToken);
        logger.LogInformation("Exclusao de privacidade concluida para o cliente de servico {ClientId}.", Actor());
        return Ok(result);
    }

    private static bool TryGetAccountId(MyAnimesPrivacyAccountRequest? request, out string accountId)
    {
        accountId = request?.AccountId ?? string.Empty;
        return MyAnimesPrivacyContract.IsValidAccountId(accountId);
    }

    private static ProblemDetails InvalidAccountProblem() => new()
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Conta invalida.",
        Detail = $"accountId e obrigatorio, deve ter ate {MyAnimesPrivacyContract.AccountIdMaxLength} caracteres e nao pode conter espacos nem caracteres de controle."
    };

    private string Actor() => User.FindFirst("client_id")?.Value ?? "unknown-service";

    private string CorrelationId() =>
        Response.Headers[CorrelationContext.HeaderName].FirstOrDefault() ?? HttpContext.TraceIdentifier;
}
