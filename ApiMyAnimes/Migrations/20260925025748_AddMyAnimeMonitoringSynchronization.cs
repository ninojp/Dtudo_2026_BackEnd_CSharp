using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMyAnimes.Migrations
{
    /// <inheritdoc />
    public partial class AddMyAnimeMonitoringSynchronization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MyAnimeMonitoringSyncRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AuthorizedRoots = table.Column<int>(type: "int", nullable: false),
                    AvailableRoots = table.Column<int>(type: "int", nullable: false),
                    DiscoveredFolders = table.Column<int>(type: "int", nullable: false),
                    AddedMappings = table.Column<int>(type: "int", nullable: false),
                    UpdatedMappings = table.Column<int>(type: "int", nullable: false),
                    UnchangedMappings = table.Column<int>(type: "int", nullable: false),
                    MissingCatalog = table.Column<int>(type: "int", nullable: false),
                    AmbiguousFolders = table.Column<int>(type: "int", nullable: false),
                    Conflicts = table.Column<int>(type: "int", nullable: false),
                    Errors = table.Column<int>(type: "int", nullable: false),
                    Warnings = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyAnimeMonitoringSyncRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MyAnimeMonitoringSyncEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RootKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MyAnimeId = table.Column<int>(type: "int", nullable: true),
                    PreviousRootKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NewRootKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyAnimeMonitoringSyncEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MyAnimeMonitoringSyncEvents_MyAnimeMonitoringSyncRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "MyAnimeMonitoringSyncRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MyAnimeMonitoringSyncEvents_RunId_Id",
                table: "MyAnimeMonitoringSyncEvents",
                columns: new[] { "RunId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MyAnimeMonitoringSyncRuns_StartedAtUtc_Id",
                table: "MyAnimeMonitoringSyncRuns",
                columns: new[] { "StartedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MyAnimeMonitoringSyncEvents");

            migrationBuilder.DropTable(
                name: "MyAnimeMonitoringSyncRuns");
        }
    }
}
