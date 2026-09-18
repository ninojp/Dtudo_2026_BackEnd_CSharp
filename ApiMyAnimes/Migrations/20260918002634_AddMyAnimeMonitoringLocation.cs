using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMyAnimes.Migrations
{
    /// <inheritdoc />
    public partial class AddMyAnimeMonitoringLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MyAnimeMonitoringLocations",
                columns: table => new
                {
                    MyAnimeId = table.Column<int>(type: "int", nullable: false),
                    RootKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyAnimeMonitoringLocations", x => x.MyAnimeId);
                    table.ForeignKey(
                        name: "FK_MyAnimeMonitoringLocations_MyAnimes_MyAnimeId",
                        column: x => x.MyAnimeId,
                        principalTable: "MyAnimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MyAnimeMonitoringLocations");
        }
    }
}
