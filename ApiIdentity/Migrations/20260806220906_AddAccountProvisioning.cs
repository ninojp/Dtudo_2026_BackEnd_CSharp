using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountProvisioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActivatedAtUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActivationCompleted",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "IdentityBootstrapState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BootstrappedAccountId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityBootstrapState", x => x.Id);
                    table.CheckConstraint("CK_IdentityBootstrapState_CompletedAtUtc", "DATEPART(TZOFFSET, [CompletedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentityBootstrapState_Singleton", "[Id] = 1");
                    table.ForeignKey(
                        name: "FK_IdentityBootstrapState_AspNetUsers_BootstrappedAccountId",
                        column: x => x.BootstrappedAccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityInitialAccountSecrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SecretHash = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityInitialAccountSecrets", x => x.Id);
                    table.CheckConstraint("CK_IdentityInitialAccountSecrets_CreatedAtUtc", "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentityInitialAccountSecrets_ExpiresAtUtc", "DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentityInitialAccountSecrets_Expiry", "[ExpiresAtUtc] > [CreatedAtUtc]");
                    table.CheckConstraint("CK_IdentityInitialAccountSecrets_RevokedAtUtc", "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentityInitialAccountSecrets_UsedAtUtc", "[UsedAtUtc] IS NULL OR DATEPART(TZOFFSET, [UsedAtUtc]) = 0");
                    table.ForeignKey(
                        name: "FK_IdentityInitialAccountSecrets_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityProvisioningAuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Target = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RetentionUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProvisioningAuditEvents", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AspNetUsers_ActivationState",
                table: "AspNetUsers",
                sql: "([IsActivationCompleted] = 0 AND [ActivatedAtUtc] IS NULL) OR ([IsActivationCompleted] = 1 AND [ActivatedAtUtc] IS NOT NULL AND DATEPART(TZOFFSET, [ActivatedAtUtc]) = 0)");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityBootstrapState_BootstrappedAccountId",
                table: "IdentityBootstrapState",
                column: "BootstrappedAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_IdentityInitialAccountSecrets_ActiveAccount",
                table: "IdentityInitialAccountSecrets",
                column: "AccountId",
                unique: true,
                filter: "[UsedAtUtc] IS NULL AND [RevokedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProvisioningAuditEvents_OccurredAtUtc_Id",
                table: "IdentityProvisioningAuditEvents",
                columns: new[] { "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProvisioningAuditEvents_RetentionUntilUtc",
                table: "IdentityProvisioningAuditEvents",
                column: "RetentionUntilUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityBootstrapState");

            migrationBuilder.DropTable(
                name: "IdentityInitialAccountSecrets");

            migrationBuilder.DropTable(
                name: "IdentityProvisioningAuditEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AspNetUsers_ActivationState",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ActivatedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActivationCompleted",
                table: "AspNetUsers");
        }
    }
}
