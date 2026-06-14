using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiCSharp.Migrations;

/// <inheritdoc />
public partial class RefazendoAsMigration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Animes",
            columns: table => new
            {
                MalId = table.Column<int>(type: "int", nullable: false),
                Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Episodios = table.Column<int>(type: "int", nullable: false),
                MalUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ImagensUrlMal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SubTitulos = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Animes", x => x.MalId);
            });

        migrationBuilder.CreateTable(
            name: "MyAnimes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Titulo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                AnimesMalId = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MyAnimes", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Animes");

        migrationBuilder.DropTable(
            name: "MyAnimes");
    }
}
