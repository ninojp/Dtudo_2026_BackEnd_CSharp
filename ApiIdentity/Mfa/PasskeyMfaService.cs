using System.Text;
using System.Security.Claims;
using System.Data;
using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Mfa;

public sealed class PasskeyMfaService
{
    private readonly UserManager<IdentityAccount> _userManager;
    private readonly IdentityDbContext _context;
    private readonly IFido2 _fido2;
    private readonly IdentitySecurityChallengeService _challengeService;
    private readonly StepUpService _stepUpService;
    private readonly SecuritySessionService _sessionService;
    private readonly IdentitySecurityAuditWriter _auditWriter;
    private readonly TimeProvider _timeProvider;
    private readonly IdentityMfaOptions _options;

    public PasskeyMfaService(
        UserManager<IdentityAccount> userManager,
        IdentityDbContext context,
        IFido2 fido2,
        IdentitySecurityChallengeService challengeService,
        StepUpService stepUpService,
        SecuritySessionService sessionService,
        IdentitySecurityAuditWriter auditWriter,
        TimeProvider timeProvider,
        IOptions<IdentityMfaOptions> options)
    {
        _userManager = userManager;
        _context = context;
        _fido2 = fido2;
        _challengeService = challengeService;
        _stepUpService = stepUpService;
        _sessionService = sessionService;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<PasskeyRegistrationOptions?> BeginRegistrationAsync(
        ClaimsPrincipal principal,
        string? passkeyName,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        if (account is null)
        {
            return null;
        }

        var user = ToFidoUser(account);
        var existingPasskeys = await _userManager.GetPasskeysAsync(account);
        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = user,
            ExcludeCredentials = existingPasskeys
                .Select(passkey => new PublicKeyCredentialDescriptor(passkey.CredentialId))
                .ToArray(),
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Required
            },
            AttestationPreference = AttestationConveyancePreference.None
        });
        var challenge = await _challengeService.CreateAsync(
            account.Id,
            IdentitySecurityChallengeKinds.PasskeyRegistration,
            options.ToJson(),
            context,
            TimeSpan.FromSeconds(_options.ChallengeLifetimeSeconds),
            cancellationToken);
        return challenge is null
            ? null
            : new PasskeyRegistrationOptions(challenge.Id, options);
    }

    public async Task<bool> CompleteRegistrationAsync(
        ClaimsPrincipal principal,
        Guid challengeId,
        AuthenticatorAttestationRawResponse response,
        string? passkeyName,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        if (account is null || response?.RawId is null)
        {
            return false;
        }

        var optionsJson = await _challengeService.ReadAndConsumeAsync<string>(
            challengeId,
            account.Id,
            IdentitySecurityChallengeKinds.PasskeyRegistration,
            context,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return false;
        }

        try
        {
            var options = CredentialCreateOptions.FromJson(optionsJson);
            var credential = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = response,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = async (parameters, callbackCancellationToken) =>
                    await _userManager.FindByPasskeyIdAsync(
                        parameters.CredentialId) is null
            }, cancellationToken);
            var transports = credential.Transports is null
                ? []
                : credential.Transports
                    .Select(transport => transport.ToEnumMemberValue())
                    .ToArray();
            var passkey = new UserPasskeyInfo(
                credential.Id,
                credential.PublicKey,
                _timeProvider.GetUtcNow(),
                credential.SignCount,
                transports,
                true,
                credential.IsBackupEligible,
                credential.IsBackedUp,
                credential.AttestationObject,
                credential.AttestationClientDataJson)
            {
                Name = NormalizeName(passkeyName)
            };
            var result = await _userManager.AddOrUpdatePasskeyAsync(account, passkey);
            if (!result.Succeeded)
            {
                return false;
            }

            await _sessionService.RevokeSecurityStateAsync(
                account.Id,
                cancellationToken: cancellationToken);
            _auditWriter.Record(
                account.Id,
                "identity.security.passkey.registered",
                $"account:{account.Id}",
                "succeeded",
                context.DeviceId ?? "unknown-device",
                "passkey-registered");
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<PasskeyAuthenticationOptions?> BeginAuthenticationAsync(
        ClaimsPrincipal principal,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        if (account is null)
        {
            return null;
        }

        var passkeys = await _userManager.GetPasskeysAsync(account);
        if (passkeys.Count == 0)
        {
            return null;
        }

        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = passkeys
                .Select(passkey => new PublicKeyCredentialDescriptor(passkey.CredentialId))
                .ToArray(),
            UserVerification = UserVerificationRequirement.Required
        });
        var challenge = await _challengeService.CreateAsync(
            account.Id,
            IdentitySecurityChallengeKinds.PasskeyAuthentication,
            options.ToJson(),
            context,
            TimeSpan.FromSeconds(_options.ChallengeLifetimeSeconds),
            cancellationToken);
        return challenge is null
            ? null
            : new PasskeyAuthenticationOptions(challenge.Id, options);
    }

    public async Task<PasskeyAuthenticationResult?> CompleteAuthenticationAndGrantAsync(
        ClaimsPrincipal principal,
        Guid challengeId,
        AuthenticatorAssertionRawResponse response,
        string action,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetPrincipalAccountId(principal);
        var account = accountId is null
            ? null
            : await _userManager.FindByIdAsync(accountId);
        if (account is null || response?.RawId is null)
        {
            return null;
        }

        var optionsJson = await _challengeService.ReadAndConsumeAsync<string>(
            challengeId,
            account.Id,
            IdentitySecurityChallengeKinds.PasskeyAuthentication,
            context,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return null;
        }

        var connection = _context.Database.GetDbConnection();
        var connectionWasOpen = connection.State == ConnectionState.Open;
        if (!connectionWasOpen)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        _context.Database.UseTransaction(transaction);
        try
        {
            var passkey = await _userManager.GetPasskeyAsync(account, response.RawId);
            if (passkey is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var options = AssertionOptions.FromJson(optionsJson);
            var assertion = await _fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = response,
                OriginalOptions = options,
                StoredPublicKey = passkey.PublicKey,
                StoredSignatureCounter = passkey.SignCount,
                IsUserHandleOwnerOfCredentialIdCallback = async (parameters, callbackCancellationToken) =>
                {
                    var owner = await _userManager.FindByPasskeyIdAsync(
                        parameters.CredentialId);
                    var expectedUserHandle = Encoding.UTF8.GetBytes(account.Id);
                    return owner?.Id == account.Id
                        && parameters.UserHandle.AsSpan().SequenceEqual(expectedUserHandle);
                }
            }, cancellationToken);
            if (passkey.SignCount != 0
                && (assertion.SignCount == 0 || assertion.SignCount <= passkey.SignCount))
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            passkey.SignCount = assertion.SignCount;
            passkey.IsBackedUp = assertion.IsBackedUp;
            var update = await _userManager.AddOrUpdatePasskeyAsync(account, passkey);
            if (!update.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            var grant = await _stepUpService.GrantAsync(
                principal,
                action,
                IdentityMfaMethods.Passkey,
                context,
                cancellationToken);
            if (grant is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            await transaction.CommitAsync(cancellationToken);
            return new PasskeyAuthenticationResult(grant, assertion.SignCount);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            _context.Database.UseTransaction(null);
            if (!connectionWasOpen)
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }

    public async Task<bool> RemoveAsync(
        ClaimsPrincipal principal,
        byte[] credentialId,
        CancellationToken cancellationToken = default)
    {
        var account = await FindPrincipalAccountAsync(principal);
        if (account is null || credentialId is null || credentialId.Length == 0)
        {
            return false;
        }

        if (await _userManager.GetPasskeyAsync(account, credentialId) is null)
        {
            return false;
        }

        var result = await _userManager.RemovePasskeyAsync(account, credentialId);
        if (!result.Succeeded)
        {
            return false;
        }

        await _sessionService.RevokeSecurityStateAsync(
            account.Id,
            cancellationToken: cancellationToken);
        _auditWriter.Record(
            account.Id,
            "identity.security.passkey.removed",
            $"account:{account.Id}",
            "succeeded",
            GetDeviceId(principal),
            "passkey-removed");
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Fido2User ToFidoUser(IdentityAccount account)
    {
        var name = account.UserName ?? account.Email ?? account.Id;
        return new Fido2User
        {
            DisplayName = name,
            Name = name,
            Id = Encoding.UTF8.GetBytes(account.Id)
        };
    }

    private static string? NormalizeName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<IdentityAccount?> FindPrincipalAccountAsync(ClaimsPrincipal principal)
    {
        var accountId = GetPrincipalAccountId(principal);
        return accountId is null
            ? null
            : await _userManager.FindByIdAsync(accountId);
    }

    private static string? GetPrincipalAccountId(ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true
            ? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
            : null;

    private static string GetDeviceId(ClaimsPrincipal principal) =>
        principal.FindFirst("device_id")?.Value ?? "unknown-device";
}
