using System.Security.Claims;
using ApiIdentity.Configuration;
using ApiIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Mfa;

public sealed class TotpMfaService
{
    private readonly UserManager<IdentityAccount> _userManager;
    private readonly StepUpService _stepUpService;
    private readonly SecuritySessionService _sessionService;
    private readonly IdentitySecurityAuditWriter _auditWriter;
    private readonly IdentityMfaOptions _options;

    public TotpMfaService(
        UserManager<IdentityAccount> userManager,
        StepUpService stepUpService,
        SecuritySessionService sessionService,
        IdentitySecurityAuditWriter auditWriter,
        IOptions<IdentityMfaOptions> options)
    {
        _userManager = userManager;
        _stepUpService = stepUpService;
        _sessionService = sessionService;
        _auditWriter = auditWriter;
        _options = options.Value;
    }

    public async Task<TotpSetupResult?> BeginSetupAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        if (account is null)
        {
            return null;
        }

        var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(account);
        if (string.IsNullOrWhiteSpace(authenticatorKey))
        {
            var reset = await _userManager.ResetAuthenticatorKeyAsync(account);
            if (!reset.Succeeded)
            {
                return null;
            }

            authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(account);
            await InvalidateSecurityStateAsync(
                account,
                "identity.security.totp.key-created",
                "totp-key-created",
                cancellationToken);
        }

        return string.IsNullOrWhiteSpace(authenticatorKey)
            ? null
            : new TotpSetupResult(authenticatorKey);
    }

    public async Task<RecoveryCodesResult?> ConfirmSetupAsync(
        ClaimsPrincipal principal,
        string token,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        if (account is null || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            account,
            TokenOptions.DefaultAuthenticatorProvider,
            token.Trim());
        if (!valid)
        {
            return null;
        }

        var enabled = await _userManager.SetTwoFactorEnabledAsync(account, true);
        if (!enabled.Succeeded)
        {
            return null;
        }

        var recoveryCodes = await GenerateRecoveryCodesAsync(account);
        if (recoveryCodes is null)
        {
            await _userManager.SetTwoFactorEnabledAsync(account, false);
            return null;
        }

        await InvalidateSecurityStateAsync(
            account,
            "identity.security.totp.enabled",
            "totp-enabled",
            cancellationToken);
        return recoveryCodes;
    }

    public async Task<StepUpGrantResult?> VerifyAndGrantAsync(
        ClaimsPrincipal principal,
        string token,
        string action,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        if (account is null
            || !account.TwoFactorEnabled
            || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            account,
            TokenOptions.DefaultAuthenticatorProvider,
            token.Trim());
        return valid
            ? await _stepUpService.GrantAsync(
                principal,
                action,
                IdentityMfaMethods.Totp,
                context,
                cancellationToken)
            : null;
    }

    public async Task<StepUpGrantResult?> RedeemRecoveryCodeAndGrantAsync(
        ClaimsPrincipal principal,
        string recoveryCode,
        string action,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        if (account is null
            || !account.TwoFactorEnabled
            || string.IsNullOrWhiteSpace(recoveryCode))
        {
            return null;
        }

        var redemption = await _userManager.RedeemTwoFactorRecoveryCodeAsync(
            account,
            recoveryCode.Trim());
        if (!redemption.Succeeded)
        {
            return null;
        }

        _auditWriter.Record(
            account.Id,
            "identity.security.recovery-code.redeemed",
            $"account:{account.Id}",
            "succeeded",
            context.DeviceId ?? "unknown-device",
            "recovery-code-redeemed");
        return await _stepUpService.GrantAsync(
            principal,
            action,
            IdentityMfaMethods.RecoveryCode,
            context,
            cancellationToken);
    }

    public async Task<RecoveryCodesResult?> GenerateRecoveryCodesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        if (account is null || !account.TwoFactorEnabled)
        {
            return null;
        }

        var codes = await GenerateRecoveryCodesAsync(account);
        if (codes is null)
        {
            return null;
        }

        await InvalidateSecurityStateAsync(
            account,
            "identity.security.recovery-code.regenerated",
            "recovery-codes-regenerated",
            cancellationToken);
        return codes;
    }

    public async Task<bool> DisableAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        return account is not null
            && await ResetFactorsAsync(account.Id, account.Id, cancellationToken);
    }

    internal async Task<bool> ResetFactorsAsync(
        string accountId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByIdAsync(accountId);
        if (account is null)
        {
            return false;
        }

        var disabled = await _userManager.SetTwoFactorEnabledAsync(account, false);
        var resetKey = await _userManager.ResetAuthenticatorKeyAsync(account);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(account, 0);
        var securityStamp = await _userManager.UpdateSecurityStampAsync(account);
        if (!disabled.Succeeded
            || !resetKey.Succeeded
            || recoveryCodes is null
            || !securityStamp.Succeeded)
        {
            return false;
        }

        await _sessionService.RevokeSecurityStateAsync(
            account.Id,
            cancellationToken: cancellationToken);
        _auditWriter.Record(
            actor,
            "identity.security.totp.reset",
            $"account:{account.Id}",
            "succeeded",
            "all-devices",
            "totp-and-recovery-factors-reset");
        await _userManager.UpdateAsync(account);
        return true;
    }

    private async Task<IdentityAccount?> FindPrincipalAccountAsync(ClaimsPrincipal principal)
    {
        var accountId = GetPrincipalAccountId(principal);
        return accountId is null
            ? null
            : await _userManager.FindByIdAsync(accountId);
    }

    private async Task InvalidateSecurityStateAsync(
        IdentityAccount account,
        string action,
        string reason,
        CancellationToken cancellationToken)
    {
        await _sessionService.RevokeSecurityStateAsync(
            account.Id,
            cancellationToken: cancellationToken);
        _auditWriter.Record(
            account.Id,
            action,
            $"account:{account.Id}",
            "succeeded",
            "all-devices",
            reason);
    }

    private async Task<RecoveryCodesResult?> GenerateRecoveryCodesAsync(IdentityAccount account)
    {
        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(
            account,
            _options.RecoveryCodeCount);
        return codes is null
            ? null
            : new RecoveryCodesResult(codes.ToArray());
    }

    private static string? GetPrincipalAccountId(ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true
            ? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
            : null;
}
