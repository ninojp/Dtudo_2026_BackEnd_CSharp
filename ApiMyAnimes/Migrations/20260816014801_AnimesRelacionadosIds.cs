using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMyAnimes.Migrations
{
    /// <inheritdoc />
    public partial class AnimesRelacionadosIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnimesRelacionadosIds",
                table: "Animes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnimesRelacionadosIds",
                table: "Animes");
        }
    }
}
