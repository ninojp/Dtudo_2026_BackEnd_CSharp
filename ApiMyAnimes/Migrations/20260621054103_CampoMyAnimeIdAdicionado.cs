using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMyAnimes.Migrations
{
    /// <inheritdoc />
    public partial class CampoMyAnimeIdAdicionado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MyAnimeID",
                table: "Animes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MyAnimeID",
                table: "Animes");
        }
    }
}
