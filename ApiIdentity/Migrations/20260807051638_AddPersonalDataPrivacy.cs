using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiIdentity.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalDataPrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityPersonalDataDeletionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScheduledForUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RetentionUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityPersonalDataDeletionRequests", x => x.Id);
                    table.CheckConstraint("CK_IdentityPersonalDataDeletionRequests_ProcessedAtUtc", "[ProcessedAtUtc] IS NULL OR DATEPART(TZOFFSET, [ProcessedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentityPersonalDataDeletionRequests_RetentionUntilUtc", "[RetentionUntilUtc] IS NULL OR DATEPART(TZOFFSET, [RetentionUntilUtc]) = 0");
                    table.CheckConstraint("CK_IdentityPersonalDataDeletionRequests_Schedule", "[ScheduledForUtc] > [RequestedAtUtc] AND DATEPART(TZOFFSET, [RequestedAtUtc]) = 0 AND DATEPART(TZOFFSET, [ScheduledForUtc]) = 0");
                    table.CheckConstraint("CK_IdentityPersonalDataDeletionRequests_Status", "[Status] IN ('Pending', 'Completed')");
                });

            migrationBuilder.CreateTable(
                name: "IdentityPersonalFavorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ResourceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityPersonalFavorites", x => x.Id);
                    table.CheckConstraint("CK_IdentityPersonalFavorites_CreatedAtUtc", "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentityPersonalFavorites_ResourceKey", "LEN(LTRIM(RTRIM([ResourceKey]))) > 0");
                    table.CheckConstraint("CK_IdentityPersonalFavorites_ResourceType", "LEN(LTRIM(RTRIM([ResourceType]))) > 0");
                    table.ForeignKey(
                        name: "FK_IdentityPersonalFavorites_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityPersonalLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityPersonalLists", x => x.Id);
                    table.UniqueConstraint("AK_IdentityPersonalLists_AccountId_Id", x => new { x.AccountId, x.Id });
                    table.CheckConstraint("CK_IdentityPersonalLists_CreatedAtUtc", "DATEPART(TZOFFSET, [CreatedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentityPersonalLists_Name", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_IdentityPersonalLists_UpdatedAtUtc", "DATEPART(TZOFFSET, [UpdatedAtUtc]) = 0");
                    table.ForeignKey(
                        name: "FK_IdentityPersonalLists_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityPersonalPreferences",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityPersonalPreferences", x => new { x.AccountId, x.Key });
                    table.CheckConstraint("CK_IdentityPersonalPreferences_Key", "LEN(LTRIM(RTRIM([Key]))) > 0");
                    table.ForeignKey(
                        name: "FK_IdentityPersonalPreferences_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityPersonalListItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ResourceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    AddedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityPersonalListItems", x => x.Id);
                    table.CheckConstraint("CK_IdentityPersonalListItems_AddedAtUtc", "DATEPART(TZOFFSET, [AddedAtUtc]) = 0");
                    table.CheckConstraint("CK_IdentityPersonalListItems_Position", "[Position] >= 0");
                    table.CheckConstraint("CK_IdentityPersonalListItems_ResourceKey", "LEN(LTRIM(RTRIM([ResourceKey]))) > 0");
                    table.CheckConstraint("CK_IdentityPersonalListItems_ResourceType", "LEN(LTRIM(RTRIM([ResourceType]))) > 0");
                    table.ForeignKey(
                        name: "FK_IdentityPersonalListItems_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdentityPersonalListItems_IdentityPersonalLists_AccountId_ListId",
                        columns: x => new { x.AccountId, x.ListId },
                        principalTable: "IdentityPersonalLists",
                        principalColumns: new[] { "AccountId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPersonalDataDeletionRequests_AccountId_Status",
                table: "IdentityPersonalDataDeletionRequests",
                columns: new[] { "AccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_IdentityPersonalDataDeletionRequests_PendingAccount",
                table: "IdentityPersonalDataDeletionRequests",
                column: "AccountId",
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPersonalFavorites_AccountId_CreatedAtUtc",
                table: "IdentityPersonalFavorites",
                columns: new[] { "AccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_IdentityPersonalFavorites_Account_Resource",
                table: "IdentityPersonalFavorites",
                columns: new[] { "AccountId", "ResourceType", "ResourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPersonalListItems_AccountId_ListId",
                table: "IdentityPersonalListItems",
                columns: new[] { "AccountId", "ListId" });

            migrationBuilder.CreateIndex(
                name: "UX_IdentityPersonalListItems_List_Resource",
                table: "IdentityPersonalListItems",
                columns: new[] { "ListId", "ResourceType", "ResourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPersonalLists_AccountId_UpdatedAtUtc",
                table: "IdentityPersonalLists",
                columns: new[] { "AccountId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityPersonalPreferences_AccountId",
                table: "IdentityPersonalPreferences",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityPersonalDataDeletionRequests");

            migrationBuilder.DropTable(
                name: "IdentityPersonalFavorites");

            migrationBuilder.DropTable(
                name: "IdentityPersonalListItems");

            migrationBuilder.DropTable(
                name: "IdentityPersonalPreferences");

            migrationBuilder.DropTable(
                name: "IdentityPersonalLists");
        }
    }
}
