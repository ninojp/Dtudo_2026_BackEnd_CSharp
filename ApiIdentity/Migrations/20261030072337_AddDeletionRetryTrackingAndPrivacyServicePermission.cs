using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletionRetryTrackingAndPrivacyServicePermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "IdentityPersonalDataDeletionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAttemptAtUtc",
                table: "IdentityPersonalDataDeletionRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "IdentityPersonalDataDeletionRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.InsertData(
                table: "IdentityPermissionDefinitions",
                columns: new[] { "Key", "Description" },
                values: new object[] { "service.privacy.myanimes", "Chamada interna (ApiIdentity para ApiMyAnimes) de exportacao e exclusao de dados pessoais por conta." });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPersonalDataDeletionRequests_Status_ScheduledForUtc",
                table: "IdentityPersonalDataDeletionRequests",
                columns: new[] { "Status", "ScheduledForUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_IdentityPersonalDataDeletionRequests_AttemptCount",
                table: "IdentityPersonalDataDeletionRequests",
                sql: "[AttemptCount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_IdentityPersonalDataDeletionRequests_LastAttemptAtUtc",
                table: "IdentityPersonalDataDeletionRequests",
                sql: "[LastAttemptAtUtc] IS NULL OR DATEPART(TZOFFSET, [LastAttemptAtUtc]) = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdentityPersonalDataDeletionRequests_Status_ScheduledForUtc",
                table: "IdentityPersonalDataDeletionRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IdentityPersonalDataDeletionRequests_AttemptCount",
                table: "IdentityPersonalDataDeletionRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_IdentityPersonalDataDeletionRequests_LastAttemptAtUtc",
                table: "IdentityPersonalDataDeletionRequests");

            migrationBuilder.DeleteData(
                table: "IdentityPermissionDefinitions",
                keyColumn: "Key",
                keyValue: "service.privacy.myanimes");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "IdentityPersonalDataDeletionRequests");

            migrationBuilder.DropColumn(
                name: "LastAttemptAtUtc",
                table: "IdentityPersonalDataDeletionRequests");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "IdentityPersonalDataDeletionRequests");
        }
    }
}
