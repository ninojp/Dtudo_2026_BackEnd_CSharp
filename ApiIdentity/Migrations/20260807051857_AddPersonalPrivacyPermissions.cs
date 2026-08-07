using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApiIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalPrivacyPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "IdentityPermissionDefinitions",
                columns: new[] { "Key", "Description" },
                values: new object[,]
                {
                    { "personal.read", "Leitura dos recursos pessoais do proprio usuario." },
                    { "personal.write", "Alteracao dos recursos pessoais do proprio usuario." },
                    { "privacy.delete", "Solicitacao de exclusao dos dados do proprio usuario." },
                    { "privacy.export", "Exportacao dos dados pessoais do proprio usuario." }
                });

            migrationBuilder.InsertData(
                table: "IdentityRolePermissions",
                columns: new[] { "PermissionKey", "RoleId" },
                values: new object[,]
                {
                    { "personal.read", "206268dd-529c-49f9-973f-030ddcbba450" },
                    { "personal.write", "206268dd-529c-49f9-973f-030ddcbba450" },
                    { "privacy.delete", "206268dd-529c-49f9-973f-030ddcbba450" },
                    { "privacy.export", "206268dd-529c-49f9-973f-030ddcbba450" },
                    { "personal.read", "bb9c24e5-6b8a-4464-a420-11db01021681" },
                    { "personal.write", "bb9c24e5-6b8a-4464-a420-11db01021681" },
                    { "privacy.delete", "bb9c24e5-6b8a-4464-a420-11db01021681" },
                    { "privacy.export", "bb9c24e5-6b8a-4464-a420-11db01021681" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdentityRolePermissions",
                keyColumns: new[] { "PermissionKey", "RoleId" },
                keyValues: new object[] { "personal.read", "206268dd-529c-49f9-973f-030ddcbba450" });

            migrationBuilder.DeleteData(
                table: "IdentityRolePermissions",
                keyColumns: new[] { "PermissionKey", "RoleId" },
                keyValues: new object[] { "personal.write", "206268dd-529c-49f9-973f-030ddcbba450" });

            migrationBuilder.DeleteData(
                table: "IdentityRolePermissions",
                keyColumns: new[] { "PermissionKey", "RoleId" },
                keyValues: new object[] { "privacy.delete", "206268dd-529c-49f9-973f-030ddcbba450" });

            migrationBuilder.DeleteData(
                table: "IdentityRolePermissions",
                keyColumns: new[] { "PermissionKey", "RoleId" },
                keyValues: new object[] { "privacy.export", "206268dd-529c-49f9-973f-030ddcbba450" });

            migrationBuilder.DeleteData(
                table: "IdentityRolePermissions",
                keyColumns: new[] { "PermissionKey", "RoleId" },
                keyValues: new object[] { "personal.read", "bb9c24e5-6b8a-4464-a420-11db01021681" });

            migrationBuilder.DeleteData(
                table: "IdentityRolePermissions",
                keyColumns: new[] { "PermissionKey", "RoleId" },
                keyValues: new object[] { "personal.write", "bb9c24e5-6b8a-4464-a420-11db01021681" });

            migrationBuilder.DeleteData(
                table: "IdentityRolePermissions",
                keyColumns: new[] { "PermissionKey", "RoleId" },
                keyValues: new object[] { "privacy.delete", "bb9c24e5-6b8a-4464-a420-11db01021681" });

            migrationBuilder.DeleteData(
                table: "IdentityRolePermissions",
                keyColumns: new[] { "PermissionKey", "RoleId" },
                keyValues: new object[] { "privacy.export", "bb9c24e5-6b8a-4464-a420-11db01021681" });

            migrationBuilder.DeleteData(
                table: "IdentityPermissionDefinitions",
                keyColumn: "Key",
                keyValue: "personal.read");

            migrationBuilder.DeleteData(
                table: "IdentityPermissionDefinitions",
                keyColumn: "Key",
                keyValue: "personal.write");

            migrationBuilder.DeleteData(
                table: "IdentityPermissionDefinitions",
                keyColumn: "Key",
                keyValue: "privacy.delete");

            migrationBuilder.DeleteData(
                table: "IdentityPermissionDefinitions",
                keyColumn: "Key",
                keyValue: "privacy.export");
        }
    }
}
