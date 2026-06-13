using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiJikanCSharp.Migrations
{
    /// <inheritdoc />
    public partial class CriandoTabelasMyAnimeEAnime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MyAnimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyAnimes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Animes",
                columns: table => new
                {
                    MalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MalUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Episodios = table.Column<int>(type: "int", nullable: false),
                    Imagens = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Titles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MyAnimeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animes", x => x.MalId);
                    table.ForeignKey(
                        name: "FK_Animes_MyAnimes_MyAnimeId",
                        column: x => x.MyAnimeId,
                        principalTable: "MyAnimes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Animes_MyAnimeId",
                table: "Animes",
                column: "MyAnimeId");
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
}
