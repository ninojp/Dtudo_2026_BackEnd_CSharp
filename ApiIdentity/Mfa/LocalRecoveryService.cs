using System.Security.Claims;
using System.Security.Cryptography;
using ApiIdentity.Authorization;
using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Mfa;

public sealed class LocalRecoveryService
{
    private readonly IdentityDbContext _context;
    private readonly UserManager<IdentityAccount> _userManager;
    private readonly IPasswordHasher<IdentityAccount> _passwordHasher;
    private readonly TotpMfaService _totpService;
    private readonly IdentitySecurityAuditWriter _auditWriter;
    private readonly TimeProvider _timeProvider;
    private readonly IdentityMfaOptions _options;
    private readonly string _unknownSecretHash;

    public LocalRecoveryService(
        IdentityDbContext context,
        UserManager<IdentityAccount> userManager,
        IPasswordHasher<IdentityAccount> passwordHasher,
        TotpMfaService totpService,
        IdentitySecurityAuditWriter auditWriter,
        TimeProvider timeProvider,
        IOptions<IdentityMfaOptions> options)
    {
        _context = context;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
        _totpService = totpService;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
        _options = options.Value;

        var unknownSecret = GenerateSecret();
        try
        {
            _unknownSecretHash = _passwordHasher.HashPassword(new IdentityAccount(), unknownSecret);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(System.Text.Encoding.UTF8.GetBytes(unknownSecret));
        }
    }

    public async Task<LocalRecoveryTicketDelivery?> IssueAsync(
        ClaimsPrincipal actorPrincipal,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var actor = await FindAuthorizedActorAsync(actorPrincipal, cancellationToken);
        if (actor is null || string.IsNullOrWhiteSpace(accountId))
        {
            return null;
        }

        var account = await _userManager.FindByIdAsync(accountId.Trim());
        if (account is null)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        await _context.RecoveryTickets
            .Where(ticket => ticket.AccountId == account.Id
                && ticket.UsedAtUtc == null
                && ticket.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(ticket => ticket.RevokedAtUtc, now),
                cancellationToken);

        var secret = GenerateSecret();
        var ticket = new IdentityRecoveryTicket
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            SecretHash = _passwordHasher.HashPassword(account, secret),
            IssuedBy = actor.Id,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(_options.LocalRecoveryLifetimeMinutes)
        };
        _context.RecoveryTickets.Add(ticket);
        _auditWriter.Record(
            actor.Id,
            "identity.security.local-recovery.issued",
            $"account:{account.Id}",
            "succeeded",
            GetDeviceId(actorPrincipal),
            "single-use-recovery-ticket-issued");

        await _context.SaveChangesAsync(cancellationToken);
        return new LocalRecoveryTicketDelivery(ticket.Id, secret, ticket.ExpiresAtUtc);
    }

    public async Task<bool> RedeemAsync(
        LocalRecoveryRedeemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null
            || request.TicketId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Secret)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            VerifyUnknownSecret(request?.Secret);
            return false;
        }

        var ticket = await _context.RecoveryTickets
            .Include(item => item.Account)
            .SingleOrDefaultAsync(item => item.Id == request.TicketId, cancellationToken);
        if (ticket?.Account is null)
        {
            VerifyUnknownSecret(request.Secret);
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        var accepted = ticket.UsedAtUtc is null
            && ticket.RevokedAtUtc is null
            && ticket.ExpiresAtUtc > now
            && VerifySecret(ticket.Account, ticket.SecretHash, request.Secret);
        if (!accepted || !await HasValidPasswordAsync(ticket.Account, request.NewPassword))
        {
            return false;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        ticket.UsedAtUtc = now;
        ticket.Account.PasswordHash = _passwordHasher.HashPassword(
            ticket.Account,
            request.NewPassword);
        if (!await _totpService.ResetFactorsAsync(
            ticket.Account.Id,
            "local-recovery",
            cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        _auditWriter.Record(
            "local-recovery",
            "identity.security.local-recovery.redeemed",
            $"account:{ticket.Account.Id}",
            "succeeded",
            "all-devices",
            "password-and-factors-reset");
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<bool> RevokeAsync(
        ClaimsPrincipal actorPrincipal,
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var actor = await FindAuthorizedActorAsync(actorPrincipal, cancellationToken);
        if (actor is null)
        {
            return false;
        }

        var ticket = await _context.RecoveryTickets.SingleOrDefaultAsync(
            item => item.Id == ticketId,
            cancellationToken);
        if (ticket is null || ticket.UsedAtUtc is not null || ticket.RevokedAtUtc is not null)
        {
            return false;
        }

        ticket.RevokedAtUtc = _timeProvider.GetUtcNow();
        _auditWriter.Record(
            actor.Id,
            "identity.security.local-recovery.revoked",
            $"ticket:{ticketId:D}",
            "succeeded",
            GetDeviceId(actorPrincipal),
            "recovery-ticket-revoked");
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    private async Task<IdentityAccount?> FindAuthorizedActorAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (principal.Identity?.IsAuthenticated != true
            || !HasProvisioningPermission(principal))
        {
            return null;
        }

        var actorId = GetPrincipalAccountId(principal);
        return actorId is null
            ? null
            : await _userManager.FindByIdAsync(actorId);
    }

    private bool HasProvisioningPermission(ClaimsPrincipal principal) =>
        principal.HasClaim(AuthorizationCatalog.PermissionClaimType, AuthorizationCatalog.Permissions.IdentityProvision)
        || principal.IsInRole(AuthorizationCatalog.Roles.SuperAdministrator);

    private async Task<bool> HasValidPasswordAsync(
        IdentityAccount account,
        string password)
    {
        foreach (var validator in _userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(_userManager, account, password);
            if (!validation.Succeeded)
            {
                return false;
            }
        }

        return true;
    }

    private bool VerifySecret(
        IdentityAccount account,
        string secretHash,
        string suppliedSecret) =>
        _passwordHasher.VerifyHashedPassword(account, secretHash, suppliedSecret)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;

    private void VerifyUnknownSecret(string? suppliedSecret)
    {
        _ = _passwordHasher.VerifyHashedPassword(
            new IdentityAccount(),
            _unknownSecretHash,
            suppliedSecret ?? string.Empty);
    }

    private static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        try
        {
            return WebEncoders.Base64UrlEncode(bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private static string? GetPrincipalAccountId(ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true
            ? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
            : null;

    private static string GetDeviceId(ClaimsPrincipal principal) =>
        principal.FindFirst("device_id")?.Value ?? "unknown-device";
}
