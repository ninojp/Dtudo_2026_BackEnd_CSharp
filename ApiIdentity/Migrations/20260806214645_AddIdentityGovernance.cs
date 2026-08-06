using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApiIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdultAgeConfirmedAtUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasConfirmedAdultAge",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "IdentityPermissionDefinitions",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityPermissionDefinitions", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "IdentityTermsDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityTermsDocuments", x => x.Id);
                    table.CheckConstraint("CK_IdentityTermsDocuments_Content", "LEN(LTRIM(RTRIM([Content]))) > 0");
                    table.CheckConstraint("CK_IdentityTermsDocuments_ContentHashSha256", "LEN([ContentHashSha256]) = 64 AND [ContentHashSha256] NOT LIKE '%[^0-9A-Fa-f]%'");
                    table.CheckConstraint("CK_IdentityTermsDocuments_DocumentType", "LEN(LTRIM(RTRIM([DocumentType]))) > 0");
                    table.CheckConstraint("CK_IdentityTermsDocuments_PublishedAtUtc", "DATEPART(TZOFFSET, [PublishedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentityTermsDocuments_Version", "LEN(LTRIM(RTRIM([Version]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "IdentityRolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityRolePermissions", x => new { x.RoleId, x.PermissionKey });
                    table.ForeignKey(
                        name: "FK_IdentityRolePermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdentityRolePermissions_IdentityPermissionDefinitions_PermissionKey",
                        column: x => x.PermissionKey,
                        principalTable: "IdentityPermissionDefinitions",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityTermsAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TermsDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityTermsAcceptances", x => x.Id);
                    table.CheckConstraint("CK_IdentityTermsAcceptances_AcceptedAtUtc", "DATEPART(TZOFFSET, [AcceptedAtUtc]) = 0");
                    table.ForeignKey(
                        name: "FK_IdentityTermsAcceptances_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdentityTermsAcceptances_IdentityTermsDocuments_TermsDocumentId",
                        column: x => x.TermsDocumentId,
                        principalTable: "IdentityTermsDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "206268dd-529c-49f9-973f-030ddcbba450", "206268dd-529c-49f9-973f-030ddcbba450", "Usuario do Site", "USUARIO DO SITE" },
                    { "bb9c24e5-6b8a-4464-a420-11db01021681", "bb9c24e5-6b8a-4464-a420-11db01021681", "Superadministrador", "SUPERADMINISTRADOR" }
                });

            migrationBuilder.InsertData(
                table: "IdentityPermissionDefinitions",
                columns: new[] { "Key", "Description" },
                values: new object[,]
                {
                    { "catalog.delete", "Exclusao do catalogo com step-up quando aplicavel." },
                    { "catalog.read", "Leitura do catalogo publico." },
                    { "catalog.write", "Criacao e alteracao do catalogo." },
                    { "filesystem.command", "Operacao de arquivos por ID e comando autorizado." },
                    { "health.read", "Leitura do health minimo restrito." },
                    { "identity.login", "Autenticacao no servico de identidade." },
                    { "identity.provision", "Bootstrap e provisionamento administrativo de contas." },
                    { "identity.self.read", "Leitura do proprio perfil de identidade." },
                    { "service.mal.read", "Chamada interna autorizada a dados MyAnimeList." }
                });

            migrationBuilder.InsertData(
                table: "IdentityRolePermissions",
                columns: new[] { "PermissionKey", "RoleId" },
                values: new object[,]
                {
                    { "catalog.read", "206268dd-529c-49f9-973f-030ddcbba450" },
                    { "identity.self.read", "206268dd-529c-49f9-973f-030ddcbba450" },
                    { "catalog.delete", "bb9c24e5-6b8a-4464-a420-11db01021681" },
                    { "catalog.read", "bb9c24e5-6b8a-4464-a420-11db01021681" },
                    { "catalog.write", "bb9c24e5-6b8a-4464-a420-11db01021681" },
                    { "filesystem.command", "bb9c24e5-6b8a-4464-a420-11db01021681" },
                    { "health.read", "bb9c24e5-6b8a-4464-a420-11db01021681" },
                    { "identity.provision", "bb9c24e5-6b8a-4464-a420-11db01021681" },
                    { "identity.self.read", "bb9c24e5-6b8a-4464-a420-11db01021681" }
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AspNetUsers_AdultAgeConfirmation",
                table: "AspNetUsers",
                sql: "([HasConfirmedAdultAge] = 0 AND [AdultAgeConfirmedAtUtc] IS NULL) OR ([HasConfirmedAdultAge] = 1 AND [AdultAgeConfirmedAtUtc] IS NOT NULL AND DATEPART(TZOFFSET, [AdultAgeConfirmedAtUtc]) = 0)");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityRolePermissions_PermissionKey",
                table: "IdentityRolePermissions",
                column: "PermissionKey");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityTermsAcceptances_TermsDocumentId",
                table: "IdentityTermsAcceptances",
                column: "TermsDocumentId");

            migrationBuilder.CreateIndex(
                name: "UX_IdentityTermsAcceptances_Account_Document",
                table: "IdentityTermsAcceptances",
                columns: new[] { "AccountId", "TermsDocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_IdentityTermsDocuments_ActiveType",
                table: "IdentityTermsDocuments",
                column: "DocumentType",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_IdentityTermsDocuments_Type_Version",
                table: "IdentityTermsDocuments",
                columns: new[] { "DocumentType", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityRolePermissions");

            migrationBuilder.DropTable(
                name: "IdentityTermsAcceptances");

            migrationBuilder.DropTable(
                name: "IdentityPermissionDefinitions");

            migrationBuilder.DropTable(
                name: "IdentityTermsDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AspNetUsers_AdultAgeConfirmation",
                table: "AspNetUsers");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "206268dd-529c-49f9-973f-030ddcbba450");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "bb9c24e5-6b8a-4464-a420-11db01021681");

            migrationBuilder.DropColumn(
                name: "AdultAgeConfirmedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HasConfirmedAdultAge",
                table: "AspNetUsers");
        }
    }
}
