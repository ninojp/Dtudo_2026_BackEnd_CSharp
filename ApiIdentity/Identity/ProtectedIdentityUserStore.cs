using System.Security.Cryptography;
using System.Data;
using System.Text;
using ApiIdentity.Data;
using ApiIdentity.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApiIdentity.Identity;

public sealed class ProtectedIdentityUserStore : UserStore<IdentityAccount>
{
    private const string InternalLoginProvider = "[AspNetUserStore]";
    private const string RecoveryCodeTokenName = "RecoveryCodes";
    private readonly IDataProtector _authenticatorKeyProtector;
    private readonly IDataProtector _recoveryCodeProtector;

    public ProtectedIdentityUserStore(
        ApiIdentity.Data.IdentityDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        _authenticatorKeyProtector = dataProtectionProvider.CreateProtector(
            "Dtudo2026",
            "ApiIdentity",
            "IdentityAuthenticatorKey");
        _recoveryCodeProtector = dataProtectionProvider.CreateProtector(
            "Dtudo2026",
            "ApiIdentity",
            "IdentityRecoveryCode");
    }

    public override Task SetAuthenticatorKeyAsync(
        IdentityAccount user,
        string key,
        CancellationToken cancellationToken)
    {
        return base.SetAuthenticatorKeyAsync(
            user,
            _authenticatorKeyProtector.Protect(key),
            cancellationToken);
    }

    public override async Task<string?> GetAuthenticatorKeyAsync(
        IdentityAccount user,
        CancellationToken cancellationToken)
    {
        var protectedKey = await base.GetAuthenticatorKeyAsync(user, cancellationToken);
        return TryUnprotect(_authenticatorKeyProtector, protectedKey);
    }

    public override Task ReplaceCodesAsync(
        IdentityAccount user,
        IEnumerable<string> recoveryCodes,
        CancellationToken cancellationToken)
    {
        var protectedCodes = recoveryCodes.Select(code => _recoveryCodeProtector.Protect(code));
        return SetTokenAsync(
            user,
            InternalLoginProvider,
            RecoveryCodeTokenName,
            string.Join(';', protectedCodes),
            cancellationToken);
    }

    public override async Task<bool> RedeemCodeAsync(
        IdentityAccount user,
        string code,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        await Context.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await Context.Database.GetDbConnection()
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        Context.Database.UseTransaction(transaction);
        try
        {
            var mergedCodes = await GetTokenAsync(
                user,
                InternalLoginProvider,
                RecoveryCodeTokenName,
                cancellationToken) ?? string.Empty;
            var protectedCodes = mergedCodes.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var matchingCode = protectedCodes.FirstOrDefault(protectedCode =>
                Matches(_recoveryCodeProtector, protectedCode, code));
            if (matchingCode is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var remainingCodes = protectedCodes.Where(protectedCode => protectedCode != matchingCode);
            await SetTokenAsync(
                user,
                InternalLoginProvider,
                RecoveryCodeTokenName,
                string.Join(';', remainingCodes),
                cancellationToken);
            await SaveChanges(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            Context.Database.UseTransaction(null);
            await Context.Database.CloseConnectionAsync();
        }
    }

    public override async Task<int> CountCodesAsync(
        IdentityAccount user,
        CancellationToken cancellationToken)
    {
        var mergedCodes = await GetTokenAsync(
            user,
            InternalLoginProvider,
            RecoveryCodeTokenName,
            cancellationToken) ?? string.Empty;
        return mergedCodes
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Count(protectedCode => TryUnprotect(_recoveryCodeProtector, protectedCode) is not null);
    }

    private static bool Matches(IDataProtector protector, string protectedValue, string expected)
    {
        var value = TryUnprotect(protector, protectedValue);
        if (value is null)
        {
            return false;
        }

        var valueBytes = Encoding.UTF8.GetBytes(value);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        try
        {
            return valueBytes.Length == expectedBytes.Length
                && CryptographicOperations.FixedTimeEquals(valueBytes, expectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(valueBytes);
            CryptographicOperations.ZeroMemory(expectedBytes);
        }
    }

    private static string? TryUnprotect(IDataProtector protector, string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            return null;
        }

        try
        {
            return protector.Unprotect(protectedValue);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
