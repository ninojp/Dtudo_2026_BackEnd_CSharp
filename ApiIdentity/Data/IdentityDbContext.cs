using ApiIdentity.Models;
using ApiIdentity.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApiIdentity.Data;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<IdentityAccount>(options)
{
    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<TermsDocument> TermsDocuments => Set<TermsDocument>();

    public DbSet<TermsAcceptance> TermsAcceptances => Set<TermsAcceptance>();

    public DbSet<InitialAccountSecret> InitialAccountSecrets => Set<InitialAccountSecret>();

    public DbSet<IdentityBootstrapState> BootstrapStates => Set<IdentityBootstrapState>();

    public DbSet<IdentityProvisioningAuditEvent> ProvisioningAuditEvents => Set<IdentityProvisioningAuditEvent>();

    public DbSet<IdentitySecurityChallenge> SecurityChallenges => Set<IdentitySecurityChallenge>();

    public DbSet<IdentityStepUpGrant> StepUpGrants => Set<IdentityStepUpGrant>();

    public DbSet<IdentitySecurityDevice> SecurityDevices => Set<IdentitySecurityDevice>();

    public DbSet<IdentitySecuritySession> SecuritySessions => Set<IdentitySecuritySession>();

    public DbSet<IdentitySecurityToken> SecurityTokens => Set<IdentitySecurityToken>();

    public DbSet<IdentitySecuritySnapshot> SecuritySnapshots => Set<IdentitySecuritySnapshot>();

    public DbSet<IdentityRecoveryTicket> RecoveryTickets => Set<IdentityRecoveryTicket>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureProvisioningAuditEventsAreAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureProvisioningAuditEventsAreAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureIdentityAccount(builder);
        ConfigurePermissionCatalog(builder);
        ConfigureTerms(builder);
        ConfigureAccountProvisioning(builder);
        ConfigureMfa(builder);
        builder.UseOpenIddict();
    }

    private static void ConfigureIdentityAccount(ModelBuilder builder)
    {
        builder.Entity<IdentityAccount>(account =>
        {
            account.Property(identityAccount => identityAccount.AdultAgeConfirmedAtUtc)
                .HasColumnType("datetimeoffset");

            account.ToTable("AspNetUsers", table => table.HasCheckConstraint(
                "CK_AspNetUsers_AdultAgeConfirmation",
                "([HasConfirmedAdultAge] = 0 AND [AdultAgeConfirmedAtUtc] IS NULL) OR " +
                "([HasConfirmedAdultAge] = 1 AND [AdultAgeConfirmedAtUtc] IS NOT NULL " +
                "AND DATEPART(TZOFFSET, [AdultAgeConfirmedAtUtc]) = 0)"));

            account.ToTable("AspNetUsers", table => table.HasCheckConstraint(
                "CK_AspNetUsers_ActivationState",
                "([IsActivationCompleted] = 0 AND [ActivatedAtUtc] IS NULL) OR " +
                "([IsActivationCompleted] = 1 AND [ActivatedAtUtc] IS NOT NULL " +
                "AND DATEPART(TZOFFSET, [ActivatedAtUtc]) = 0)"));
        });
    }

    private static void ConfigurePermissionCatalog(ModelBuilder builder)
    {
        builder.Entity<PermissionDefinition>(permission =>
        {
            permission.ToTable("IdentityPermissionDefinitions");
            permission.HasKey(permissionDefinition => permissionDefinition.Key);
            permission.Property(permissionDefinition => permissionDefinition.Key).HasMaxLength(100);
            permission.Property(permissionDefinition => permissionDefinition.Description)
                .HasMaxLength(256)
                .IsRequired();
            permission.HasData(AuthorizationCatalog.AllPermissions.Select(permissionDefinition => new PermissionDefinition
            {
                Key = permissionDefinition.Key,
                Description = permissionDefinition.Description
            }));
        });

        builder.Entity<IdentityRole>().HasData(AuthorizationCatalog.AllRoles.Select(role => new IdentityRole
        {
            Id = role.Id,
            Name = role.Name,
            NormalizedName = role.Name.ToUpperInvariant(),
            ConcurrencyStamp = role.Id
        }));

        builder.Entity<RolePermission>(rolePermission =>
        {
            rolePermission.ToTable("IdentityRolePermissions");
            rolePermission.HasKey(item => new { item.RoleId, item.PermissionKey });
            rolePermission.Property(item => item.RoleId).HasMaxLength(450);
            rolePermission.Property(item => item.PermissionKey).HasMaxLength(100);
            rolePermission.HasIndex(item => item.PermissionKey);
            rolePermission.HasOne(item => item.Role)
                .WithMany()
                .HasForeignKey(item => item.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            rolePermission.HasOne(item => item.Permission)
                .WithMany(permission => permission.RolePermissions)
                .HasForeignKey(item => item.PermissionKey)
                .OnDelete(DeleteBehavior.Restrict);
            rolePermission.HasData(AuthorizationCatalog.AllRoles.SelectMany(role => role.PermissionKeys.Select(permissionKey =>
                new RolePermission { RoleId = role.Id, PermissionKey = permissionKey })));
        });
    }

    private static void ConfigureTerms(ModelBuilder builder)
    {
        builder.Entity<TermsDocument>(document =>
        {
            document.ToTable("IdentityTermsDocuments", table =>
            {
                table.HasCheckConstraint(
                    "CK_IdentityTermsDocuments_DocumentType",
                    "LEN(LTRIM(RTRIM([DocumentType]))) > 0");
                table.HasCheckConstraint(
                    "CK_IdentityTermsDocuments_Version",
                    "LEN(LTRIM(RTRIM([Version]))) > 0");
                table.HasCheckConstraint(
                    "CK_IdentityTermsDocuments_Content",
                    "LEN(LTRIM(RTRIM([Content]))) > 0");
                table.HasCheckConstraint(
                    "CK_IdentityTermsDocuments_ContentHashSha256",
                    "LEN([ContentHashSha256]) = 64 AND [ContentHashSha256] NOT LIKE '%[^0-9A-Fa-f]%'");
                table.HasCheckConstraint(
                    "CK_IdentityTermsDocuments_PublishedAtUtc",
                    "DATEPART(TZOFFSET, [PublishedAtUtc]) = 0");
            });
            document.HasKey(item => item.Id);
            document.Property(item => item.DocumentType).HasMaxLength(80).IsRequired();
            document.Property(item => item.Version).HasMaxLength(40).IsRequired();
            document.Property(item => item.Content).IsRequired();
            document.Property(item => item.ContentHashSha256).HasMaxLength(64).IsRequired();
            document.Property(item => item.PublishedAtUtc).HasColumnType("datetimeoffset");
            document.HasIndex(item => new { item.DocumentType, item.Version })
                .IsUnique()
                .HasDatabaseName("UX_IdentityTermsDocuments_Type_Version");
            document.HasIndex(item => item.DocumentType)
                .HasFilter("[IsActive] = 1")
                .IsUnique()
                .HasDatabaseName("UX_IdentityTermsDocuments_ActiveType");
        });

        builder.Entity<TermsAcceptance>(acceptance =>
        {
            acceptance.ToTable("IdentityTermsAcceptances", table => table.HasCheckConstraint(
                "CK_IdentityTermsAcceptances_AcceptedAtUtc",
                "DATEPART(TZOFFSET, [AcceptedAtUtc]) = 0"));
            acceptance.HasKey(item => item.Id);
            acceptance.Property(item => item.AccountId).HasMaxLength(450).IsRequired();
            acceptance.Property(item => item.AcceptedAtUtc).HasColumnType("datetimeoffset");
            acceptance.HasIndex(item => new { item.AccountId, item.TermsDocumentId })
                .IsUnique()
                .HasDatabaseName("UX_IdentityTermsAcceptances_Account_Document");
            acceptance.HasIndex(item => item.TermsDocumentId);
            acceptance.HasOne(item => item.Account)
                .WithMany()
                .HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            acceptance.HasOne(item => item.TermsDocument)
                .WithMany(document => document.Acceptances)
                .HasForeignKey(item => item.TermsDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAccountProvisioning(ModelBuilder builder)
    {
        builder.Entity<InitialAccountSecret>(secret =>
        {
            secret.ToTable("IdentityInitialAccountSecrets", table =>
            {
                table.HasCheckConstraint(
                    "CK_IdentityInitialAccountSecrets_Expiry",
                    "[ExpiresAtUtc] > [CreatedAtUtc]");
                table.HasCheckConstraint(
                    "CK_IdentityInitialAccountSecrets_CreatedAtUtc",
                    "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentityInitialAccountSecrets_ExpiresAtUtc",
                    "DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentityInitialAccountSecrets_UsedAtUtc",
                    "[UsedAtUtc] IS NULL OR DATEPART(TZOFFSET, [UsedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentityInitialAccountSecrets_RevokedAtUtc",
                    "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
            });
            secret.HasKey(item => item.Id);
            secret.Property(item => item.AccountId).HasMaxLength(450).IsRequired();
            secret.Property(item => item.SecretHash).HasMaxLength(512).IsRequired();
            secret.Property(item => item.CreatedAtUtc).HasColumnType("datetimeoffset");
            secret.Property(item => item.ExpiresAtUtc).HasColumnType("datetimeoffset");
            secret.Property(item => item.UsedAtUtc).HasColumnType("datetimeoffset");
            secret.Property(item => item.RevokedAtUtc).HasColumnType("datetimeoffset");
            secret.Property(item => item.RowVersion).IsRowVersion();
            secret.HasIndex(item => item.AccountId)
                .IsUnique()
                .HasFilter("[UsedAtUtc] IS NULL AND [RevokedAtUtc] IS NULL")
                .HasDatabaseName("UX_IdentityInitialAccountSecrets_ActiveAccount");
            secret.HasOne(item => item.Account)
                .WithMany()
                .HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentityBootstrapState>(state =>
        {
            state.ToTable("IdentityBootstrapState", table =>
            {
                table.HasCheckConstraint("CK_IdentityBootstrapState_Singleton", "[Id] = 1");
                table.HasCheckConstraint(
                    "CK_IdentityBootstrapState_CompletedAtUtc",
                    "DATEPART(TZOFFSET, [CompletedAtUtc]) = 0");
            });
            state.HasKey(item => item.Id);
            state.Property(item => item.Id).ValueGeneratedNever();
            state.Property(item => item.BootstrappedAccountId).HasMaxLength(450).IsRequired();
            state.Property(item => item.CompletedAtUtc).HasColumnType("datetimeoffset");
            state.HasOne(item => item.BootstrappedAccount)
                .WithMany()
                .HasForeignKey(item => item.BootstrappedAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentityProvisioningAuditEvent>(auditEvent =>
        {
            auditEvent.ToTable("IdentityProvisioningAuditEvents");
            auditEvent.HasKey(item => item.Id);
            auditEvent.Property(item => item.Actor).HasMaxLength(256).IsRequired();
            auditEvent.Property(item => item.Action).HasMaxLength(128).IsRequired();
            auditEvent.Property(item => item.Target).HasMaxLength(512).IsRequired();
            auditEvent.Property(item => item.Result).HasMaxLength(64).IsRequired();
            auditEvent.Property(item => item.OccurredAtUtc).HasColumnType("datetimeoffset");
            auditEvent.Property(item => item.DeviceId).HasMaxLength(256).IsRequired();
            auditEvent.Property(item => item.CorrelationId).HasMaxLength(128).IsRequired();
            auditEvent.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
            auditEvent.Property(item => item.RetentionUntilUtc).HasColumnType("datetimeoffset");
            auditEvent.HasIndex(item => new { item.OccurredAtUtc, item.Id });
            auditEvent.HasIndex(item => item.RetentionUntilUtc);
        });
    }

    private void EnsureProvisioningAuditEventsAreAppendOnly()
    {
        if (ChangeTracker.Entries<IdentityProvisioningAuditEvent>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException(
                "Identity provisioning audit events are append-only and cannot be modified or deleted by the application.");
        }
    }

    private static void ConfigureMfa(ModelBuilder builder)
    {
        builder.Entity<IdentitySecurityChallenge>(challenge =>
        {
            challenge.ToTable("IdentitySecurityChallenges", table =>
            {
                table.HasCheckConstraint("CK_IdentitySecurityChallenges_Expiry", "[ExpiresAtUtc] > [CreatedAtUtc]");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityChallenges_CreatedAtUtc",
                    "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityChallenges_ExpiresAtUtc",
                    "DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityChallenges_ConsumedAtUtc",
                    "[ConsumedAtUtc] IS NULL OR DATEPART(TZOFFSET, [ConsumedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityChallenges_RevokedAtUtc",
                    "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
            });
            challenge.HasKey(item => item.Id);
            challenge.Property(item => item.AccountId).HasMaxLength(450).IsRequired();
            challenge.Property(item => item.Kind).HasMaxLength(80).IsRequired();
            challenge.Property(item => item.ProtectedPayload).IsRequired();
            challenge.Property(item => item.SessionId).HasMaxLength(450);
            challenge.Property(item => item.DeviceId).HasMaxLength(450);
            challenge.Property(item => item.CreatedAtUtc).HasColumnType("datetimeoffset");
            challenge.Property(item => item.ExpiresAtUtc).HasColumnType("datetimeoffset");
            challenge.Property(item => item.ConsumedAtUtc).HasColumnType("datetimeoffset");
            challenge.Property(item => item.RevokedAtUtc).HasColumnType("datetimeoffset");
            challenge.Property(item => item.RowVersion).IsRowVersion();
            challenge.HasIndex(item => new { item.AccountId, item.Kind, item.ExpiresAtUtc });
            challenge.HasOne(item => item.Account)
                .WithMany()
                .HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentityStepUpGrant>(grant =>
        {
            grant.ToTable("IdentityStepUpGrants", table =>
            {
                table.HasCheckConstraint("CK_IdentityStepUpGrants_Expiry", "[ExpiresAtUtc] > [GrantedAtUtc]");
                table.HasCheckConstraint(
                    "CK_IdentityStepUpGrants_GrantedAtUtc",
                    "DATEPART(TZOFFSET, [GrantedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentityStepUpGrants_ExpiresAtUtc",
                    "DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentityStepUpGrants_RevokedAtUtc",
                    "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
            });
            grant.HasKey(item => item.Id);
            grant.Property(item => item.AccountId).HasMaxLength(450).IsRequired();
            grant.Property(item => item.Action).HasMaxLength(160).IsRequired();
            grant.Property(item => item.Method).HasMaxLength(40).IsRequired();
            grant.Property(item => item.SessionId).HasMaxLength(450);
            grant.Property(item => item.DeviceId).HasMaxLength(450);
            grant.Property(item => item.GrantedAtUtc).HasColumnType("datetimeoffset");
            grant.Property(item => item.ExpiresAtUtc).HasColumnType("datetimeoffset");
            grant.Property(item => item.RevokedAtUtc).HasColumnType("datetimeoffset");
            grant.Property(item => item.RowVersion).IsRowVersion();
            grant.HasIndex(item => new { item.AccountId, item.Action, item.ExpiresAtUtc });
            grant.HasOne(item => item.Account)
                .WithMany()
                .HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentitySecurityDevice>(device =>
        {
            device.ToTable("IdentitySecurityDevices", table =>
            {
                table.HasCheckConstraint(
                    "CK_IdentitySecurityDevices_CreatedAtUtc",
                    "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityDevices_LastSeenAtUtc",
                    "DATEPART(TZOFFSET, [LastSeenAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityDevices_TrustedAtUtc",
                    "DATEPART(TZOFFSET, [TrustedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityDevices_TrustedUntilUtc",
                    "[TrustedUntilUtc] > [TrustedAtUtc] AND DATEPART(TZOFFSET, [TrustedUntilUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityDevices_RevokedAtUtc",
                    "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
            });
            device.HasKey(item => item.Id);
            device.Property(item => item.AccountId).HasMaxLength(450).IsRequired();
            device.Property(item => item.Name).HasMaxLength(120).IsRequired();
            device.Property(item => item.CreatedAtUtc).HasColumnType("datetimeoffset");
            device.Property(item => item.LastSeenAtUtc).HasColumnType("datetimeoffset");
            device.Property(item => item.TrustedAtUtc).HasColumnType("datetimeoffset");
            device.Property(item => item.TrustedUntilUtc).HasColumnType("datetimeoffset");
            device.Property(item => item.RevokedAtUtc).HasColumnType("datetimeoffset");
            device.Property(item => item.RowVersion).IsRowVersion();
            device.HasIndex(item => new { item.AccountId, item.RevokedAtUtc });
            device.HasAlternateKey(item => new { item.AccountId, item.Id });
            device.HasOne(item => item.Account)
                .WithMany()
                .HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentitySecuritySession>(session =>
        {
            session.ToTable("IdentitySecuritySessions", table =>
            {
                table.HasCheckConstraint(
                    "CK_IdentitySecuritySessions_CreatedAtUtc",
                    "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecuritySessions_LastSeenAtUtc",
                    "DATEPART(TZOFFSET, [LastSeenAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecuritySessions_ExpiresAtUtc",
                    "[ExpiresAtUtc] > [CreatedAtUtc] AND DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecuritySessions_RevokedAtUtc",
                    "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
            });
            session.HasKey(item => item.Id);
            session.Property(item => item.AccountId).HasMaxLength(450).IsRequired();
            session.Property(item => item.CreatedAtUtc).HasColumnType("datetimeoffset");
            session.Property(item => item.LastSeenAtUtc).HasColumnType("datetimeoffset");
            session.Property(item => item.ExpiresAtUtc).HasColumnType("datetimeoffset");
            session.Property(item => item.RevokedAtUtc).HasColumnType("datetimeoffset");
            session.Property(item => item.RowVersion).IsRowVersion();
            session.HasIndex(item => new { item.AccountId, item.RevokedAtUtc });
            session.HasOne(item => item.Account)
                .WithMany()
                .HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            session.HasOne(item => item.Device)
                .WithMany()
                .HasForeignKey(item => new { item.AccountId, item.DeviceId })
                .HasPrincipalKey(item => new { item.AccountId, item.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentitySecurityToken>(token =>
        {
            token.ToTable("IdentitySecurityTokens", table =>
            {
                table.HasCheckConstraint(
                    "CK_IdentitySecurityTokens_TokenType",
                    "[TokenType] IN ('access', 'refresh')");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityTokens_Expiry",
                    "[ExpiresAtUtc] > [CreatedAtUtc]");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityTokens_CreatedAtUtc",
                    "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityTokens_ExpiresAtUtc",
                    "DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityTokens_UsedAtUtc",
                    "[UsedAtUtc] IS NULL OR DATEPART(TZOFFSET, [UsedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecurityTokens_RevokedAtUtc",
                    "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
            });
            token.HasKey(item => item.Id);
            token.Property(item => item.AccountId).HasMaxLength(450).IsRequired();
            token.Property(item => item.TokenHash).HasMaxLength(64).IsRequired();
            token.Property(item => item.TokenType).HasMaxLength(16).IsRequired();
            token.Property(item => item.CreatedAtUtc).HasColumnType("datetimeoffset");
            token.Property(item => item.ExpiresAtUtc).HasColumnType("datetimeoffset");
            token.Property(item => item.UsedAtUtc).HasColumnType("datetimeoffset");
            token.Property(item => item.RevokedAtUtc).HasColumnType("datetimeoffset");
            token.Property(item => item.RowVersion).IsRowVersion();
            token.HasIndex(item => item.TokenHash)
                .IsUnique()
                .HasDatabaseName("UX_IdentitySecurityTokens_TokenHash");
            token.HasIndex(item => new { item.FamilyId, item.RevokedAtUtc });
            token.HasIndex(item => new { item.SessionId, item.RevokedAtUtc });
            token.HasIndex(item => new { item.AccountId, item.ExpiresAtUtc });
            token.HasOne(item => item.Account)
                .WithMany()
                .HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentitySecuritySnapshot>(snapshot =>
        {
            snapshot.ToTable("IdentitySecuritySnapshots", table =>
            {
                table.HasCheckConstraint(
                    "CK_IdentitySecuritySnapshots_CreatedAtUtc",
                    "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecuritySnapshots_Expiry",
                    "[ExpiresAtUtc] > [CreatedAtUtc]");
                table.HasCheckConstraint(
                    "CK_IdentitySecuritySnapshots_ExpiresAtUtc",
                    "DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecuritySnapshots_RestoredAtUtc",
                    "[RestoredAtUtc] IS NULL OR DATEPART(TZOFFSET, [RestoredAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentitySecuritySnapshots_RevokedAtUtc",
                    "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
            });
            snapshot.HasKey(item => item.Id);
            snapshot.Property(item => item.AccountId).HasMaxLength(450).IsRequired();
            snapshot.Property(item => item.ProtectedPayload).IsRequired();
            snapshot.Property(item => item.CreatedBy).HasMaxLength(450).IsRequired();
            snapshot.Property(item => item.CreatedAtUtc).HasColumnType("datetimeoffset");
            snapshot.Property(item => item.ExpiresAtUtc).HasColumnType("datetimeoffset");
            snapshot.Property(item => item.RestoredAtUtc).HasColumnType("datetimeoffset");
            snapshot.Property(item => item.RevokedAtUtc).HasColumnType("datetimeoffset");
            snapshot.Property(item => item.RestoredBy).HasMaxLength(450);
            snapshot.Property(item => item.RowVersion).IsRowVersion();
            snapshot.HasIndex(item => new { item.AccountId, item.CreatedAtUtc });
            snapshot.HasOne(item => item.Account)
                .WithMany()
                .HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<IdentityRecoveryTicket>(ticket =>
        {
            ticket.ToTable("IdentityRecoveryTickets", table =>
            {
                table.HasCheckConstraint("CK_IdentityRecoveryTickets_Expiry", "[ExpiresAtUtc] > [CreatedAtUtc]");
                table.HasCheckConstraint(
                    "CK_IdentityRecoveryTickets_CreatedAtUtc",
                    "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentityRecoveryTickets_ExpiresAtUtc",
                    "DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentityRecoveryTickets_UsedAtUtc",
                    "[UsedAtUtc] IS NULL OR DATEPART(TZOFFSET, [UsedAtUtc]) = 0");
                table.HasCheckConstraint(
                    "CK_IdentityRecoveryTickets_RevokedAtUtc",
                    "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
            });
            ticket.HasKey(item => item.Id);
            ticket.Property(item => item.AccountId).HasMaxLength(450).IsRequired();
            ticket.Property(item => item.SecretHash).HasMaxLength(512).IsRequired();
            ticket.Property(item => item.IssuedBy).HasMaxLength(450).IsRequired();
            ticket.Property(item => item.CreatedAtUtc).HasColumnType("datetimeoffset");
            ticket.Property(item => item.ExpiresAtUtc).HasColumnType("datetimeoffset");
            ticket.Property(item => item.UsedAtUtc).HasColumnType("datetimeoffset");
            ticket.Property(item => item.RevokedAtUtc).HasColumnType("datetimeoffset");
            ticket.Property(item => item.RowVersion).IsRowVersion();
            ticket.HasIndex(item => new { item.AccountId, item.ExpiresAtUtc });
            ticket.HasIndex(item => item.AccountId)
                .IsUnique()
                .HasFilter("[UsedAtUtc] IS NULL AND [RevokedAtUtc] IS NULL")
                .HasDatabaseName("UX_IdentityRecoveryTickets_ActiveAccount");
            ticket.HasOne(item => item.Account)
                .WithMany()
                .HasForeignKey(item => item.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
