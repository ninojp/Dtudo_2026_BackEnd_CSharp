using ApiIdentity.Authorization;
using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Identity;
using ApiIdentity.Models;
using ApiIdentity.Mfa;
using ApiIdentity.Provisioning;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Tests;

public sealed class AccountProvisioningServiceTests
{
    private const string StrongPassword = "Dtudo2026!InitialPassword";

    [Fact]
    public async Task BootstrapRunsOnlyOnceAndAdministrativeProvisioningCreatesAnActivePasswordAccount()
    {
        await WithTemporaryDatabaseAsync(async services =>
        {
            var bootstrap = await BootstrapAsync(services);
            Assert.True(bootstrap.Succeeded);
            Assert.False(bootstrap.IsAlreadyCompleted);
            Assert.NotNull(bootstrap.Delivery);

            await using (var scope = services.CreateAsyncScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
                var replay = await service.BootstrapAsync(new BootstrapAccountRequest(
                    "second-admin",
                    "second-admin@example.test"));
                var provisioned = await service.ProvisionAsync(new ProvisionAccountRequest(
                    "site-user",
                    "site-user@example.test",
                    AuthorizationCatalog.Roles.SiteUser,
                    StrongPassword));

                Assert.False(replay.Succeeded);
                Assert.True(replay.IsAlreadyCompleted);
                Assert.Null(replay.Delivery);
                Assert.True(provisioned.Succeeded);
                Assert.Null(provisioned.Delivery);
            }

            await using var verificationScope = services.CreateAsyncScope();
            var context = verificationScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var bootstrapAccount = await context.Users.SingleAsync(account => account.UserName == "first-admin");
            var siteUser = await context.Users.SingleAsync(account => account.UserName == "site-user");
            var secrets = await context.InitialAccountSecrets.OrderBy(secret => secret.CreatedAtUtc).ToListAsync();
            var superAdministratorRoleId = AuthorizationCatalog.AllRoles
                .Single(item => item.Name == AuthorizationCatalog.Roles.SuperAdministrator).Id;
            var siteUserRoleId = AuthorizationCatalog.AllRoles
                .Single(item => item.Name == AuthorizationCatalog.Roles.SiteUser).Id;

            Assert.False(bootstrapAccount.IsActivationCompleted);
            Assert.Null(bootstrapAccount.PasswordHash);
            Assert.True(siteUser.IsActivationCompleted);
            Assert.NotNull(siteUser.ActivatedAtUtc);
            Assert.NotNull(siteUser.PasswordHash);
            var passwordHasher = verificationScope.ServiceProvider
                .GetRequiredService<IPasswordHasher<IdentityAccount>>();
            Assert.Equal(
                PasswordVerificationResult.Success,
                passwordHasher.VerifyHashedPassword(siteUser, siteUser.PasswordHash!, StrongPassword));
            Assert.True(await context.UserRoles.AnyAsync(role =>
                role.UserId == bootstrapAccount.Id
                && role.RoleId == superAdministratorRoleId));
            Assert.True(await context.UserRoles.AnyAsync(role =>
                role.UserId == siteUser.Id
                && role.RoleId == siteUserRoleId));
            Assert.Single(secrets);
            Assert.All(secrets, secret =>
            {
                Assert.NotEmpty(secret.SecretHash);
                Assert.False(secret.SecretHash.Contains(bootstrap.Delivery!.InitialSecret, StringComparison.Ordinal));
                Assert.Null(secret.UsedAtUtc);
                Assert.Null(secret.RevokedAtUtc);
            });
            Assert.Equal(2, await context.ProvisioningAuditEvents.CountAsync());

            var auditEvent = await context.ProvisioningAuditEvents.FirstAsync();
            context.ProvisioningAuditEvents.Remove(auditEvent);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task ReportsIdentityValidationErrorsWhenProvisioningPasswordIsRejected()
    {
        await WithTemporaryDatabaseAsync(async services =>
        {
            var bootstrap = await BootstrapAsync(services);
            Assert.True(bootstrap.Succeeded);

            await using var scope = services.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
            var result = await service.ProvisionAsync(new ProvisionAccountRequest(
                "invalid-password-user",
                "invalid-password@example.test",
                AuthorizationCatalog.Roles.SiteUser,
                "weak"));

            Assert.False(result.Succeeded);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors!);
        });
    }

    [Fact]
    public async Task RejectsReplayAfterSuccessfulActivation()
    {
        await WithTemporaryDatabaseAsync(async services =>
        {
            var bootstrap = await BootstrapAsync(services);
            var delivery = Assert.IsType<InitialSecretDelivery>(bootstrap.Delivery);

            var first = await ActivateAsync(services, delivery, StrongPassword);
            var replay = await ActivateAsync(services, delivery, StrongPassword);

            Assert.True(first.Activated);
            Assert.False(replay.Activated);

            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var secret = await context.InitialAccountSecrets.SingleAsync();
            var account = await context.Users.SingleAsync();

            Assert.NotNull(secret.UsedAtUtc);
            Assert.True(account.IsActivationCompleted);
            Assert.NotNull(account.ActivatedAtUtc);
            Assert.False(string.IsNullOrWhiteSpace(account.PasswordHash));
        });
    }

    [Fact]
    public async Task RejectsExpiredInitialSecret()
    {
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));
        await WithTemporaryDatabaseAsync(async services =>
        {
            var bootstrap = await BootstrapAsync(services);
            var delivery = Assert.IsType<InitialSecretDelivery>(bootstrap.Delivery);
            timeProvider.Advance(TimeSpan.FromMinutes(61));

            var activation = await ActivateAsync(services, delivery, StrongPassword);

            Assert.False(activation.Activated);
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var secret = await context.InitialAccountSecrets.SingleAsync();
            Assert.Null(secret.UsedAtUtc);
        }, timeProvider);
    }

    [Fact]
    public async Task RejectsRevokedInitialSecret()
    {
        await WithTemporaryDatabaseAsync(async services =>
        {
            var bootstrap = await BootstrapAsync(services);
            var delivery = Assert.IsType<InitialSecretDelivery>(bootstrap.Delivery);

            await using (var scope = services.CreateAsyncScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
                Assert.True(await service.RevokeInitialSecretAsync(delivery.ActivationId));
            }

            var activation = await ActivateAsync(services, delivery, StrongPassword);

            Assert.False(activation.Activated);
            await using var verificationScope = services.CreateAsyncScope();
            var context = verificationScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var secret = await context.InitialAccountSecrets.SingleAsync();
            Assert.NotNull(secret.RevokedAtUtc);
            Assert.Null(secret.UsedAtUtc);
        });
    }

    [Fact]
    public async Task AllowsExactlyOneConcurrentActivation()
    {
        await WithTemporaryDatabaseAsync(async services =>
        {
            var bootstrap = await BootstrapAsync(services);
            var delivery = Assert.IsType<InitialSecretDelivery>(bootstrap.Delivery);

            var activations = await Task.WhenAll(
                ActivateAsync(services, delivery, StrongPassword),
                ActivateAsync(services, delivery, StrongPassword));

            Assert.Equal(1, activations.Count(result => result.Activated));

            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var secret = await context.InitialAccountSecrets.SingleAsync();
            Assert.NotNull(secret.UsedAtUtc);
        });
    }

    [Fact]
    public async Task ReturnsTheSameGenericResultForUnknownAndInvalidActivationSecrets()
    {
        await WithTemporaryDatabaseAsync(async services =>
        {
            var bootstrap = await BootstrapAsync(services);
            var delivery = Assert.IsType<InitialSecretDelivery>(bootstrap.Delivery);

            await using var scope = services.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
            var unknown = await service.ActivateAsync(new InitialAccountActivationRequest(
                Guid.NewGuid(),
                "invalid-secret",
                StrongPassword));
            var invalid = await service.ActivateAsync(new InitialAccountActivationRequest(
                delivery.ActivationId,
                "invalid-secret",
                StrongPassword));

            Assert.False(unknown.Activated);
            Assert.False(invalid.Activated);
        });
    }

    [Fact]
    public async Task RollsBackAccountProvisioningMigrationToIdentityGovernance()
    {
        await WithTemporaryDatabaseAsync(async services =>
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var targetMigration = context.Database.GetMigrations()
                .Single(migration => migration.EndsWith("_AddIdentityGovernance", StringComparison.Ordinal));

            Assert.True(await TableExistsAsync(context, "IdentityBootstrapState"));
            Assert.True(await TableExistsAsync(context, "IdentityInitialAccountSecrets"));
            Assert.True(await TableExistsAsync(context, "IdentityProvisioningAuditEvents"));

            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(targetMigration);

            Assert.False(await TableExistsAsync(context, "IdentityBootstrapState"));
            Assert.False(await TableExistsAsync(context, "IdentityInitialAccountSecrets"));
            Assert.False(await TableExistsAsync(context, "IdentityProvisioningAuditEvents"));
            Assert.True(await TableExistsAsync(context, "IdentityTermsDocuments"));
        });
    }

    private static async Task<BootstrapAccountResult> BootstrapAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
        return await service.BootstrapAsync(new BootstrapAccountRequest(
            "first-admin",
            "first-admin@example.test"));
    }

    private static async Task<AccountActivationResult> ActivateAsync(
        IServiceProvider services,
        InitialSecretDelivery delivery,
        string password)
    {
        await using var scope = services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
        return await service.ActivateAsync(new InitialAccountActivationRequest(
            delivery.ActivationId,
            delivery.InitialSecret,
            password));
    }

    private static async Task WithTemporaryDatabaseAsync(
        Func<ServiceProvider, Task> test,
        MutableTimeProvider? timeProvider = null)
    {
        var databaseName = $"DtudoIdentity.Stage11Tests.{Guid.NewGuid():N}";
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            Encrypt = false,
            TrustServerCertificate = true
        }.ConnectionString;
        var services = new ServiceCollection();
        services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connectionString));
        services.AddDataProtection()
            .SetApplicationName("Dtudo2026.ApiIdentity.Tests");
        services.AddIdentityCore<IdentityAccount>(options =>
        {
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 4;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddUserStore<ProtectedIdentityUserStore>()
            .AddDefaultTokenProviders();
        services.AddSingleton<TimeProvider>(timeProvider ?? TimeProvider.System);
        services.AddSingleton<IOptions<IdentityMfaOptions>>(
            Options.Create(new IdentityMfaOptions
            {
                ChallengeLifetimeSeconds = 120,
                StepUpLifetimeSeconds = 300,
                LocalRecoveryLifetimeMinutes = 15,
                SnapshotLifetimeHours = 24,
                RecoveryCodeCount = 10,
                ClockSkewSeconds = 30
            }));
        services.AddSingleton<IOptions<LocalProvisioningOptions>>(
            Options.Create(new LocalProvisioningOptions
            {
                AdministrationSecret = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8",
                InitialSecretLifetimeMinutes = 60
            }));
        services.AddScoped<IdentityProvisioningAuditWriter>();
        services.AddScoped<AccountProvisioningService>();

        await using var serviceProvider = services.BuildServiceProvider();
        try
        {
            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                await context.Database.MigrateAsync();
            }

            await test(serviceProvider);
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
