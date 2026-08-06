using ApiIdentity.Authorization;
using ApiIdentity.Data;
using ApiIdentity.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ApiIdentity.Tests;

public sealed class IdentityGovernanceDatabaseTests
{
    [Fact]
    public async Task SeedsTheCentralRoleAndPermissionCatalog()
    {
        await WithTemporaryDatabaseAsync(async context =>
        {
            var permissionKeys = await context.PermissionDefinitions
                .Select(permission => permission.Key)
                .OrderBy(key => key)
                .ToListAsync();
            var roleNames = await context.Roles
                .Select(role => role.Name)
                .OrderBy(name => name)
                .ToListAsync();

            Assert.Equal(
                AuthorizationCatalog.AllPermissions.Select(permission => permission.Key).OrderBy(key => key),
                permissionKeys);
            Assert.Equal(
                AuthorizationCatalog.AllRoles.Select(role => role.Name).OrderBy(name => name),
                roleNames);
        });
    }

    [Fact]
    public async Task RejectsInvalidAdultAgeConfirmationAtTheDatabaseLevel()
    {
        await WithTemporaryDatabaseAsync(async context =>
        {
            context.Users.Add(CreateAccount(hasConfirmedAdultAge: true, confirmedAtUtc: null));

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

            context.ChangeTracker.Clear();
            context.Users.Add(CreateAccount(
                hasConfirmedAdultAge: true,
                confirmedAtUtc: new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.FromHours(-3))));

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task RejectsDuplicateTermsAcceptanceAndUnknownPermission()
    {
        await WithTemporaryDatabaseAsync(async context =>
        {
            var account = CreateAccount(hasConfirmedAdultAge: false, confirmedAtUtc: null);
            var document = new TermsDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = "terms-of-use",
                Version = "1.0",
                Content = "Termos de uso de teste.",
                ContentHashSha256 = new string('A', 64),
                PublishedAtUtc = DateTimeOffset.UtcNow,
                IsActive = true
            };
            context.Users.Add(account);
            context.TermsDocuments.Add(document);
            await context.SaveChangesAsync();

            context.TermsAcceptances.Add(new TermsAcceptance
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                TermsDocumentId = document.Id,
                AcceptedAtUtc = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();

            context.TermsAcceptances.Add(new TermsAcceptance
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                TermsDocumentId = document.Id,
                AcceptedAtUtc = DateTimeOffset.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

            context.ChangeTracker.Clear();
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = AuthorizationCatalog.AllRoles[0].Id,
                PermissionKey = "permission.unknown"
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task RollsBackTheGovernanceMigrationToZero()
    {
        await WithTemporaryDatabaseAsync(async context =>
        {
            Assert.True(await TableExistsAsync(context, "IdentityTermsDocuments"));
            Assert.True(await TableExistsAsync(context, "IdentityRolePermissions"));

            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("0");

            Assert.False(await TableExistsAsync(context, "IdentityTermsDocuments"));
            Assert.False(await TableExistsAsync(context, "IdentityRolePermissions"));
            Assert.False(await TableExistsAsync(context, "AspNetUsers"));
        });
    }

    private static IdentityAccount CreateAccount(bool hasConfirmedAdultAge, DateTimeOffset? confirmedAtUtc) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        UserName = $"user-{Guid.NewGuid():N}",
        NormalizedUserName = $"USER-{Guid.NewGuid():N}",
        Email = $"user-{Guid.NewGuid():N}@example.test",
        NormalizedEmail = $"USER-{Guid.NewGuid():N}@EXAMPLE.TEST",
        HasConfirmedAdultAge = hasConfirmedAdultAge,
        AdultAgeConfirmedAtUtc = confirmedAtUtc
    };

    private static async Task WithTemporaryDatabaseAsync(Func<IdentityDbContext, Task> test)
    {
        var databaseName = $"DtudoIdentity.Stage10Tests.{Guid.NewGuid():N}";
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            Encrypt = false,
            TrustServerCertificate = true
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new IdentityDbContext(options);
        try
        {
            await context.Database.MigrateAsync();
            await test(context);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<bool> TableExistsAsync(IdentityDbContext context, string tableName)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = @tableName";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
