using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiIdentity.Data;
using ApiIdentity.Identity;
using ApiIdentity.Models;
using ApiIdentity.Privacy;
using ApiIdentity.Provisioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace ApiIdentity.Tests;

public sealed class IdentityPrivacyServiceTests
{
    private static readonly DateTimeOffset InitialTime =
        new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PersonalResourcesAreIsolatedByOwner()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountA = await CreateAccountAsync(services, "owner-a");
            var accountB = await CreateAccountAsync(services, "owner-b");

            Guid favoriteAId;
            Guid favoriteBId;
            Guid listBId;
            await using (var scope = services.CreateAsyncScope())
            {
                var privacy = scope.ServiceProvider.GetRequiredService<IdentityPrivacyService>();
                var favoriteA = await privacy.AddFavoriteAsync(
                    accountA,
                    new PersonalResourceRequest(PersonalResourceTypes.Anime, "101"));
                var favoriteB = await privacy.AddFavoriteAsync(
                    accountB,
                    new PersonalResourceRequest(PersonalResourceTypes.Anime, "202"));
                var listB = await privacy.CreateListAsync(accountB, new PersonalListRequest("Lista B"));

                Assert.NotNull(favoriteA);
                Assert.NotNull(favoriteB);
                Assert.NotNull(listB);
                favoriteAId = favoriteA!.Id;
                favoriteBId = favoriteB!.Id;
                listBId = listB!.Id;
                var preferenceA = await privacy.SetPreferenceAsync(
                    accountA,
                    new PersonalPreferenceRequest("theme", "dark"));
                var listItemB = await privacy.AddListItemAsync(
                    accountB,
                    listBId,
                    new PersonalListItemRequest(PersonalResourceTypes.MyAnime, "303", 0));
                Assert.NotNull(preferenceA);
                Assert.NotNull(listItemB);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var privacy = scope.ServiceProvider.GetRequiredService<IdentityPrivacyService>();
                var favoritesA = await privacy.GetFavoritesAsync(accountA);
                var preferencesA = await privacy.GetPreferencesAsync(accountA);
                var listsA = await privacy.GetListsAsync(accountA);

                Assert.Single(favoritesA);
                Assert.Equal(favoriteAId, favoritesA[0].Id);
                Assert.Single(preferencesA);
                Assert.Equal("dark", preferencesA[0].Value);
                Assert.Empty(listsA);
                Assert.False(await privacy.RemoveFavoriteAsync(accountA, favoriteBId));
                Assert.Null(await privacy.AddListItemAsync(
                    accountA,
                    listBId,
                    new PersonalListItemRequest(PersonalResourceTypes.Anime, "404", 0)));
            }
        });
    }

    [Fact]
    public async Task AdultAgeAndTermsUseMinimalVersionedData()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "governed-user");
            var termsDocumentId = await CreateTermsDocumentAsync(
                services,
                "terms-of-use",
                "2.0",
                "Termos versionados para teste.");

            await using (var scope = services.CreateAsyncScope())
            {
                var privacy = scope.ServiceProvider.GetRequiredService<IdentityPrivacyService>();
                var age = await privacy.ConfirmAdultAgeAsync(accountId);
                var currentTerms = await privacy.GetActiveTermsAsync("terms-of-use");
                var acceptance = await privacy.AcceptTermsAsync(accountId, termsDocumentId);
                var repeatedAcceptance = await privacy.AcceptTermsAsync(accountId, termsDocumentId);

                Assert.NotNull(age);
                Assert.NotNull(currentTerms);
                Assert.NotNull(acceptance);
                Assert.NotNull(repeatedAcceptance);
                Assert.True(age!.HasConfirmedAdultAge);
                Assert.Equal(InitialTime, age.AdultAgeConfirmedAtUtc);
                Assert.Equal("2.0", currentTerms!.Version);
                Assert.Equal(termsDocumentId, acceptance!.TermsDocumentId);
                Assert.Equal(acceptance.AcceptanceId, repeatedAcceptance!.AcceptanceId);
            }

            await using var verificationScope = services.CreateAsyncScope();
            var context = verificationScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var accountModel = context.Model.FindEntityType(typeof(IdentityAccount));
            Assert.Null(accountModel?.FindProperty("DateOfBirth"));

            var account = await context.Users.SingleAsync(item => item.Id == accountId);
            Assert.True(account.HasConfirmedAdultAge);
            Assert.Equal(InitialTime, account.AdultAgeConfirmedAtUtc);
            Assert.Equal(1, await context.TermsAcceptances.CountAsync(item => item.AccountId == accountId));
        });
    }

    [Fact]
    public async Task ExportContainsOwnedDataButOmitsAuthenticationSecrets()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "export-user");
            var termsDocumentId = await CreateTermsDocumentAsync(
                services,
                "privacy-policy",
                "1.1",
                "Politica de privacidade para exportacao.");

            await using (var scope = services.CreateAsyncScope())
            {
                var privacy = scope.ServiceProvider.GetRequiredService<IdentityPrivacyService>();
                await privacy.ConfirmAdultAgeAsync(accountId);
                await privacy.AcceptTermsAsync(accountId, termsDocumentId);
                await privacy.AddFavoriteAsync(
                    accountId,
                    new PersonalResourceRequest(PersonalResourceTypes.Anime, "505"));
                await privacy.SetPreferenceAsync(
                    accountId,
                    new PersonalPreferenceRequest("language", "pt-BR"));
                var list = await privacy.CreateListAsync(accountId, new PersonalListRequest("Exportacao"));
                Assert.NotNull(list);
                await privacy.AddListItemAsync(
                    accountId,
                    list!.Id,
                    new PersonalListItemRequest(PersonalResourceTypes.Anime, "606", 1));

                var export = await privacy.ExportAsync(accountId);
                var serializedExport = JsonSerializer.Serialize(export);

                Assert.NotNull(export);
                Assert.Equal(accountId, export!.AccountId);
                Assert.Single(export.Favorites);
                Assert.Single(export.Preferences);
                Assert.Single(export.Lists);
                Assert.Single(export.Lists[0].Items);
                Assert.Single(export.AcceptedTerms);
                Assert.DoesNotContain("PasswordHash", serializedExport, StringComparison.Ordinal);
                Assert.DoesNotContain("SecretHash", serializedExport, StringComparison.Ordinal);
                Assert.DoesNotContain("ProtectedPayload", serializedExport, StringComparison.Ordinal);
                Assert.DoesNotContain("TokenHash", serializedExport, StringComparison.Ordinal);
            }
        });
    }

    [Fact]
    public async Task DeletionRequestUsesGracePeriodAndRetainsAuditAfterRemoval()
    {
        await WithTemporaryDatabaseAsync(async (services, timeProvider) =>
        {
            var accountId = await CreateAccountAsync(services, "delete-user");

            Guid deletionRequestId;
            await using (var scope = services.CreateAsyncScope())
            {
                var privacy = scope.ServiceProvider.GetRequiredService<IdentityPrivacyService>();
                await privacy.AddFavoriteAsync(
                    accountId,
                    new PersonalResourceRequest(PersonalResourceTypes.Anime, "707"));
                await privacy.SetPreferenceAsync(
                    accountId,
                    new PersonalPreferenceRequest("theme", "dark"));
                var list = await privacy.CreateListAsync(accountId, new PersonalListRequest("Apagar"));
                Assert.NotNull(list);
                await privacy.AddListItemAsync(
                    accountId,
                    list!.Id,
                    new PersonalListItemRequest(PersonalResourceTypes.MyAnime, "808", 0));

                var request = await privacy.RequestDeletionAsync(accountId);
                var repeatedRequest = await privacy.RequestDeletionAsync(accountId);
                Assert.NotNull(request);
                Assert.NotNull(repeatedRequest);
                deletionRequestId = request!.Id;

                Assert.Equal(InitialTime.AddDays(7), request.ScheduledForUtc);
                Assert.Equal(request.Id, repeatedRequest!.Id);
                Assert.False(await privacy.ProcessDueDeletionAsync(deletionRequestId));
            }

            timeProvider.Advance(TimeSpan.FromDays(7));
            await using (var scope = services.CreateAsyncScope())
            {
                var privacy = scope.ServiceProvider.GetRequiredService<IdentityPrivacyService>();
                Assert.True(await privacy.ProcessDueDeletionAsync(deletionRequestId));
            }

            await using var verificationScope = services.CreateAsyncScope();
            var context = verificationScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            Assert.False(await context.Users.AnyAsync(item => item.Id == accountId));
            Assert.Empty(await context.PersonalFavorites.Where(item => item.AccountId == accountId).ToListAsync());
            Assert.Empty(await context.PersonalPreferences.Where(item => item.AccountId == accountId).ToListAsync());
            Assert.Empty(await context.PersonalLists.Where(item => item.AccountId == accountId).ToListAsync());

            var deletionRequest = await context.PersonalDataDeletionRequests
                .SingleAsync(item => item.Id == deletionRequestId);
            Assert.Equal(PersonalDataDeletionStatuses.Completed, deletionRequest.Status);
            Assert.Equal(InitialTime.AddDays(7), deletionRequest.ProcessedAtUtc);
            Assert.Equal(InitialTime.AddDays(7).AddMonths(12), deletionRequest.RetentionUntilUtc);

            var auditEvents = await context.ProvisioningAuditEvents
                .Where(item => item.Target == $"account:{accountId}")
                .ToListAsync();
            var requestAudit = Assert.Single(auditEvents, item => item.Action == "privacy.deletion-requested");
            var completionAudit = Assert.Single(auditEvents, item => item.Action == "privacy.deletion-completed");
            Assert.Equal(InitialTime.AddMonths(12), requestAudit.RetentionUntilUtc);
            Assert.Equal(InitialTime.AddDays(7).AddMonths(12), completionAudit.RetentionUntilUtc);
        });
    }

    [Fact]
    public async Task InvalidPersonalPayloadsAreRejectedAndDoNotCreateRows()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "minimized-user");

            await using var scope = services.CreateAsyncScope();
            var privacy = scope.ServiceProvider.GetRequiredService<IdentityPrivacyService>();
            Assert.Null(await privacy.AddFavoriteAsync(
                accountId,
                new PersonalResourceRequest("password", "secret")));
            Assert.Null(await privacy.AddFavoriteAsync(
                accountId,
                new PersonalResourceRequest(PersonalResourceTypes.Anime, "../private")));
            Assert.Null(await privacy.SetPreferenceAsync(
                accountId,
                new PersonalPreferenceRequest("password", "secret")));
            Assert.Null(await privacy.CreateListAsync(accountId, new PersonalListRequest("\0")));

            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            Assert.Empty(await context.PersonalFavorites.ToListAsync());
            Assert.Empty(await context.PersonalPreferences.ToListAsync());
            Assert.Empty(await context.PersonalLists.ToListAsync());
        });
    }

    [Fact]
    public async Task PersonalDataMigrationRollsBackToThePreviousIdentityVersion()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var migrator = context.Database.GetService<IMigrator>();

            await migrator.MigrateAsync("20260807020126_AddSessionTokens");

            Assert.False(await TableExistsAsync(context, "IdentityPersonalFavorites"));
            Assert.False(await TableExistsAsync(context, "IdentityPersonalLists"));
            Assert.False(await TableExistsAsync(context, "IdentityPersonalDataDeletionRequests"));
        });
    }

    private static async Task<string> CreateAccountAsync(IServiceProvider services, string key)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var account = new IdentityAccount
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = key,
            NormalizedUserName = key.ToUpperInvariant(),
            Email = $"{key}@example.test",
            NormalizedEmail = $"{key}@EXAMPLE.TEST",
            PasswordHash = "password-hash-that-is-not-exported"
        };
        context.Users.Add(account);
        await context.SaveChangesAsync();
        return account.Id;
    }

    private static async Task<Guid> CreateTermsDocumentAsync(
        IServiceProvider services,
        string documentType,
        string version,
        string content)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var document = new TermsDocument
        {
            Id = Guid.NewGuid(),
            DocumentType = documentType,
            Version = version,
            Content = content,
            ContentHashSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))),
            PublishedAtUtc = InitialTime,
            IsActive = true
        };
        context.TermsDocuments.Add(document);
        await context.SaveChangesAsync();
        return document.Id;
    }

    private static async Task WithTemporaryDatabaseAsync(
        Func<ServiceProvider, MutableTimeProvider, Task> test)
    {
        var databaseName = $"DtudoIdentity.Stage18Tests.{Guid.NewGuid():N}";
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            Encrypt = false,
            TrustServerCertificate = true
        }.ConnectionString;
        var timeProvider = new MutableTimeProvider(InitialTime);
        var services = new ServiceCollection();
        services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentityCore<IdentityAccount>(options =>
        {
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdentityDbContext>();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddScoped<IdentityProvisioningAuditWriter>();
        services.AddScoped<IdentityPrivacyService>();

        await using var serviceProvider = services.BuildServiceProvider();
        try
        {
            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                await context.Database.MigrateAsync();
            }

            await test(serviceProvider, timeProvider);
        }
        finally
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
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

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
