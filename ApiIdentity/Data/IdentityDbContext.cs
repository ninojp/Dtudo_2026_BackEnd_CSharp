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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureIdentityAccount(builder);
        ConfigurePermissionCatalog(builder);
        ConfigureTerms(builder);
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
}
