using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAtUtc",
                table: "IdentitySecuritySessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TrustedAtUtc",
                table: "IdentitySecurityDevices",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TrustedUntilUtc",
                table: "IdentitySecurityDevices",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE IdentitySecuritySessions SET ExpiresAtUtc = DATEADD(day, 30, CreatedAtUtc) WHERE ExpiresAtUtc IS NULL; " +
                "UPDATE IdentitySecurityDevices SET TrustedAtUtc = CreatedAtUtc, TrustedUntilUtc = DATEADD(day, 30, CreatedAtUtc) WHERE TrustedAtUtc IS NULL OR TrustedUntilUtc IS NULL;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAtUtc",
                table: "IdentitySecuritySessions",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TrustedAtUtc",
                table: "IdentitySecurityDevices",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TrustedUntilUtc",
                table: "IdentitySecurityDevices",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "IdentitySecurityTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TokenType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentitySecurityTokens", x => x.Id);
                    table.CheckConstraint("CK_IdentitySecurityTokens_CreatedAtUtc", "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentitySecurityTokens_ExpiresAtUtc", "DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentitySecurityTokens_Expiry", "[ExpiresAtUtc] > [CreatedAtUtc]");
                    table.CheckConstraint("CK_IdentitySecurityTokens_RevokedAtUtc", "[RevokedAtUtc] IS NULL OR DATEPART(TZOFFSET, [RevokedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentitySecurityTokens_TokenType", "[TokenType] IN ('access', 'refresh')");
                    table.CheckConstraint("CK_IdentitySecurityTokens_UsedAtUtc", "[UsedAtUtc] IS NULL OR DATEPART(TZOFFSET, [UsedAtUtc]) = 0");
                    table.ForeignKey(
                        name: "FK_IdentitySecurityTokens_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_IdentitySecuritySessions_ExpiresAtUtc",
                table: "IdentitySecuritySessions",
                sql: "[ExpiresAtUtc] > [CreatedAtUtc] AND DATEPART(TZOFFSET, [ExpiresAtUtc]) = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IdentitySecurityDevices_TrustedAtUtc",
                table: "IdentitySecurityDevices",
                sql: "DATEPART(TZOFFSET, [TrustedAtUtc]) = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IdentitySecurityDevices_TrustedUntilUtc",
                table: "IdentitySecurityDevices",
                sql: "[TrustedUntilUtc] > [TrustedAtUtc] AND DATEPART(TZOFFSET, [TrustedUntilUtc]) = 0");

            migrationBuilder.CreateIndex(
                name: "IX_IdentitySecurityTokens_AccountId_ExpiresAtUtc",
                table: "IdentitySecurityTokens",
                columns: new[] { "AccountId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentitySecurityTokens_FamilyId_RevokedAtUtc",
                table: "IdentitySecurityTokens",
                columns: new[] { "FamilyId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentitySecurityTokens_SessionId_RevokedAtUtc",
                table: "IdentitySecurityTokens",
                columns: new[] { "SessionId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_IdentitySecurityTokens_TokenHash",
                table: "IdentitySecurityTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentitySecurityTokens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IdentitySecuritySessions_ExpiresAtUtc",
                table: "IdentitySecuritySessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IdentitySecurityDevices_TrustedAtUtc",
                table: "IdentitySecurityDevices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IdentitySecurityDevices_TrustedUntilUtc",
                table: "IdentitySecurityDevices");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "IdentitySecuritySessions");

            migrationBuilder.DropColumn(
                name: "TrustedAtUtc",
                table: "IdentitySecurityDevices");

            migrationBuilder.DropColumn(
                name: "TrustedUntilUtc",
                table: "IdentitySecurityDevices");
        }
    }
}
