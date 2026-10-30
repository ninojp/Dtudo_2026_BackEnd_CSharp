using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMyAnimes.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimeFolderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimeFolderRefreshRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MyAnimeId = table.Column<int>(type: "int", nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TotalAnimes = table.Column<int>(type: "int", nullable: false),
                    ProcessedAnimes = table.Column<int>(type: "int", nullable: false),
                    Updated = table.Column<int>(type: "int", nullable: false),
                    NotFound = table.Column<int>(type: "int", nullable: false),
                    Unavailable = table.Column<int>(type: "int", nullable: false),
                    Failed = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimeFolderRefreshRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnimeFolderStatus",
                columns: table => new
                {
                    MalId = table.Column<int>(type: "int", nullable: false),
                    MyAnimeId = table.Column<int>(type: "int", nullable: false),
                    RootKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VideoCount = table.Column<int>(type: "int", nullable: false),
                    ExpectedEpisodes = table.Column<int>(type: "int", nullable: true),
                    State = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Truncated = table.Column<bool>(type: "bit", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimeFolderStatus", x => x.MalId);
                    table.CheckConstraint("CK_AnimeFolderStatus_MalId", "[MalId] > 0");
                    table.CheckConstraint("CK_AnimeFolderStatus_State", "[State] IN ('none', 'partial', 'complete')");
                    table.CheckConstraint("CK_AnimeFolderStatus_VideoCount", "[VideoCount] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "AnimeFolderRefreshItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MalId = table.Column<int>(type: "int", nullable: false),
                    MyAnimeId = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    State = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    VideoCount = table.Column<int>(type: "int", nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimeFolderRefreshItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnimeFolderRefreshItems_AnimeFolderRefreshRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AnimeFolderRefreshRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimeFolderRefreshItems_RunId_Id",
                table: "AnimeFolderRefreshItems",
                columns: new[] { "RunId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimeFolderRefreshRuns_StartedAtUtc_Id",
                table: "AnimeFolderRefreshRuns",
                columns: new[] { "StartedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimeFolderStatus_MyAnimeId",
                table: "AnimeFolderStatus",
                column: "MyAnimeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimeFolderRefreshItems");

            migrationBuilder.DropTable(
                name: "AnimeFolderStatus");

            migrationBuilder.DropTable(
                name: "AnimeFolderRefreshRuns");
        }
    }
}
