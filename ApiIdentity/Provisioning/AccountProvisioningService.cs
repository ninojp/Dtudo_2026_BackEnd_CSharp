using ApiIdentity.Authorization;
using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace ApiIdentity.Provisioning;

public sealed class AccountProvisioningService
{
    private const string BootstrapActor = "local-bootstrap";
    private const string LocalAdministratorActor = "local-administrator";
    private const string LocalDeviceId = "loopback";
    private const string InternalCorrelationId = "local-provisioning";

    private readonly IdentityDbContext _context;
    private readonly UserManager<IdentityAccount> _userManager;
    private readonly IPasswordHasher<IdentityAccount> _passwordHasher;
    private readonly IdentityProvisioningAuditWriter _auditWriter;
    private readonly TimeProvider _timeProvider;
    private readonly LocalProvisioningOptions _options;
    private readonly string _unknownSecretHash;

    public AccountProvisioningService(
        IdentityDbContext context,
        UserManager<IdentityAccount> userManager,
        IPasswordHasher<IdentityAccount> passwordHasher,
        IdentityProvisioningAuditWriter auditWriter,
        TimeProvider timeProvider,
        IOptions<LocalProvisioningOptions> options)
    {
        _context = context;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
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

    public async Task<BootstrapAccountResult> BootstrapAsync(
        BootstrapAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await _context.BootstrapStates.AsNoTracking().AnyAsync(cancellationToken))
        {
            return new BootstrapAccountResult(false, true, null);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await _context.BootstrapStates.AsNoTracking().AnyAsync(cancellationToken))
            {
                return new BootstrapAccountResult(false, true, null);
            }

            var account = CreateAccount(request.UserName, request.Email);
            var creation = await CreateAccountWithRoleAsync(
                account,
                AuthorizationCatalog.Roles.SuperAdministrator);
            if (!creation)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new BootstrapAccountResult(false, false, null);
            }

            var delivery = IssueInitialSecret(account);
            var completedAtUtc = _timeProvider.GetUtcNow();
            _context.BootstrapStates.Add(new IdentityBootstrapState
            {
                Id = IdentityBootstrapState.SingletonId,
                BootstrappedAccountId = account.Id,
                CompletedAtUtc = completedAtUtc
            });
            RecordAudit(
                BootstrapActor,
                "identity.bootstrap.completed",
                $"account:{account.Id}",
                "succeeded",
                "first-superadministrator-provisioned");

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new BootstrapAccountResult(true, false, delivery);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _context.ChangeTracker.Clear();

            var alreadyCompleted = await _context.BootstrapStates.AsNoTracking().AnyAsync(cancellationToken);
            return new BootstrapAccountResult(false, alreadyCompleted, null);
        }
    }

    public async Task<ProvisionAccountResult> ProvisionAsync(
        ProvisionAccountRequest request,
        string actor = LocalAdministratorActor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await _context.BootstrapStates.AsNoTracking().AnyAsync(cancellationToken)
            || !IsCatalogRole(request.RoleName))
        {
            return new ProvisionAccountResult(false, null);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var account = CreateAccount(request.UserName, request.Email);
        var creation = await CreateAccountWithRoleAsync(account, request.RoleName.Trim());
        if (!creation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new ProvisionAccountResult(false, null);
        }

        var delivery = IssueInitialSecret(account);
        RecordAudit(
            actor,
            "identity.account.provisioned",
            $"account:{account.Id}",
            "succeeded",
            $"role:{request.RoleName.Trim()}");

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ProvisionAccountResult(true, delivery);
    }

    public async Task<bool> RevokeInitialSecretAsync(
        Guid activationId,
        string actor = LocalAdministratorActor,
        CancellationToken cancellationToken = default)
    {
        var secret = await _context.InitialAccountSecrets
            .SingleOrDefaultAsync(item => item.Id == activationId, cancellationToken);
        if (secret is null || secret.UsedAtUtc is not null || secret.RevokedAtUtc is not null)
        {
            return false;
        }

        secret.RevokedAtUtc = _timeProvider.GetUtcNow();
        RecordAudit(
            actor,
            "identity.initial-secret.revoked",
            $"activation:{activationId}",
            "succeeded",
            "administrative-revocation");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<AccountActivationResult> ActivateAsync(
        InitialAccountActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var secret = await _context.InitialAccountSecrets
            .Include(item => item.Account)
            .SingleOrDefaultAsync(item => item.Id == request.ActivationId, cancellationToken);
        if (secret is null)
        {
            VerifyUnknownSecret(request.InitialSecret);
            await RecordRejectedActivationAsync(request.ActivationId, "activation-not-found", cancellationToken);
            return new AccountActivationResult(false);
        }

        var now = _timeProvider.GetUtcNow();
        if (secret.Account is null
            || secret.UsedAtUtc is not null
            || secret.RevokedAtUtc is not null
            || secret.ExpiresAtUtc <= now
            || secret.Account.IsActivationCompleted
            || !VerifySecret(secret.Account, secret.SecretHash, request.InitialSecret))
        {
            await RecordRejectedActivationAsync(request.ActivationId, "activation-secret-rejected", cancellationToken);
            return new AccountActivationResult(false);
        }

        if (!await HasValidPasswordAsync(secret.Account, request.NewPassword))
        {
            await RecordRejectedActivationAsync(request.ActivationId, "activation-password-rejected", cancellationToken);
            return new AccountActivationResult(false);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        secret.UsedAtUtc = now;
        secret.Account.IsActivationCompleted = true;
        secret.Account.ActivatedAtUtc = now;
        secret.Account.PasswordHash = _passwordHasher.HashPassword(secret.Account, request.NewPassword);
        secret.Account.SecurityStamp = Guid.NewGuid().ToString("N");
        secret.Account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        RecordAudit(
            "anonymous-activation",
            "identity.initial-secret.activated",
            $"activation:{request.ActivationId}",
            "succeeded",
            "initial-secret-consumed");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new AccountActivationResult(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            return new AccountActivationResult(false);
        }
    }

    private async Task<bool> CreateAccountWithRoleAsync(IdentityAccount account, string roleName)
    {
        var creation = await _userManager.CreateAsync(account);
        if (!creation.Succeeded)
        {
            return false;
        }

        var roleAssignment = await _userManager.AddToRoleAsync(account, roleName);
        return roleAssignment.Succeeded;
    }

    private InitialSecretDelivery IssueInitialSecret(IdentityAccount account)
    {
        var initialSecret = GenerateSecret();
        var createdAtUtc = _timeProvider.GetUtcNow();
        var expiresAtUtc = createdAtUtc.AddMinutes(_options.InitialSecretLifetimeMinutes);
        var activationId = Guid.NewGuid();
        _context.InitialAccountSecrets.Add(new InitialAccountSecret
        {
            Id = activationId,
            AccountId = account.Id,
            SecretHash = _passwordHasher.HashPassword(account, initialSecret),
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc
        });

        return new InitialSecretDelivery(activationId, initialSecret, expiresAtUtc);
    }

    private async Task RecordRejectedActivationAsync(
        Guid activationId,
        string reason,
        CancellationToken cancellationToken)
    {
        RecordAudit(
            "anonymous-activation",
            "identity.initial-secret.activation-rejected",
            $"activation:{activationId}",
            "denied",
            reason);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private void RecordAudit(string actor, string action, string target, string result, string reason) =>
        _auditWriter.Record(
            actor,
            action,
            target,
            result,
            LocalDeviceId,
            InternalCorrelationId,
            reason);

    private static IdentityAccount CreateAccount(string userName, string email) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        UserName = userName?.Trim(),
        Email = email?.Trim(),
        SecurityStamp = Guid.NewGuid().ToString("N"),
        ConcurrencyStamp = Guid.NewGuid().ToString("N")
    };

    private static bool IsCatalogRole(string? roleName) => AuthorizationCatalog.AllRoles
        .Any(role => string.Equals(role.Name, roleName?.Trim(), StringComparison.Ordinal));

    private async Task<bool> HasValidPasswordAsync(IdentityAccount account, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

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

    private bool VerifySecret(IdentityAccount account, string secretHash, string suppliedSecret) =>
        !string.IsNullOrWhiteSpace(suppliedSecret)
        && _passwordHasher.VerifyHashedPassword(account, secretHash, suppliedSecret)
            is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;

    private void VerifyUnknownSecret(string suppliedSecret)
    {
        _ = _passwordHasher.VerifyHashedPassword(
            new IdentityAccount(),
            _unknownSecretHash,
            suppliedSecret ?? string.Empty);
    }
}
