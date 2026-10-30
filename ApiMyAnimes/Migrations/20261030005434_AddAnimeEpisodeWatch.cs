using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMyAnimes.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimeEpisodeWatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimeEpisodeWatches",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MalId = table.Column<int>(type: "int", nullable: false),
                    EpisodeNumber = table.Column<int>(type: "int", nullable: false),
                    WatchedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimeEpisodeWatches", x => new { x.AccountId, x.MalId, x.EpisodeNumber });
                    table.CheckConstraint("CK_AnimeEpisodeWatches_EpisodeNumber", "[EpisodeNumber] >= 1");
                    table.CheckConstraint("CK_AnimeEpisodeWatches_MalId", "[MalId] > 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimeEpisodeWatches");
        }
    }
}
