using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMyAnimes.Migrations
{
    /// <inheritdoc />
    public partial class RecebendoDadosDetalhadoApiJikan : Migration
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
                    SubTitulos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trailer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Approved = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleEnglish = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleJapanese = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleSynonyms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Episodes = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Airing = table.Column<bool>(type: "bit", nullable: false),
                    Aired = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rating = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<double>(type: "float", nullable: true),
                    ScoredBy = table.Column<int>(type: "int", nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: true),
                    Popularity = table.Column<int>(type: "int", nullable: true),
                    Members = table.Column<int>(type: "int", nullable: true),
                    Favorites = table.Column<int>(type: "int", nullable: true),
                    Synopsis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Background = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Season = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    Producers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Licensors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Studios = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Genres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExplicitGenres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Themes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Demographics = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
}
