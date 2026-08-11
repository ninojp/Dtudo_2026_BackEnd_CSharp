using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMusicX.Migrations
{
    /// <inheritdoc />
    public partial class InitialMusicCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicArtists",
                columns: table => new
                {
                    MusicArtistId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ArtistType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SortName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicArtists", x => x.MusicArtistId);
                    table.CheckConstraint("CK_MusicArtists_ArtistType", "[ArtistType] IN ('Unknown', 'Solo', 'Band', 'Group')");
                });

            migrationBuilder.CreateTable(
                name: "MusicCollections",
                columns: table => new
                {
                    MusicCollectionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicCollections", x => x.MusicCollectionId);
                });

            migrationBuilder.CreateTable(
                name: "MusicReleases",
                columns: table => new
                {
                    MusicReleaseId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    NormalizedTitle = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ReleaseType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ReleaseYear = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicReleases", x => x.MusicReleaseId);
                    table.CheckConstraint("CK_MusicReleases_ReleaseType", "[ReleaseType] IN ('Unknown', 'Album', 'Single', 'EP', 'Compilation', 'Video')");
                    table.CheckConstraint("CK_MusicReleases_ReleaseYear", "[ReleaseYear] IS NULL OR [ReleaseYear] BETWEEN 1000 AND 9999");
                });

            migrationBuilder.CreateTable(
                name: "MusicArtistAliases",
                columns: table => new
                {
                    MusicArtistAliasId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusicArtistId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicArtistAliases", x => x.MusicArtistAliasId);
                    table.ForeignKey(
                        name: "FK_MusicArtistAliases_MusicArtists_MusicArtistId",
                        column: x => x.MusicArtistId,
                        principalTable: "MusicArtists",
                        principalColumn: "MusicArtistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicCollectionArtists",
                columns: table => new
                {
                    MusicCollectionId = table.Column<long>(type: "bigint", nullable: false),
                    MusicArtistId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicCollectionArtists", x => new { x.MusicCollectionId, x.MusicArtistId });
                    table.CheckConstraint("CK_MusicCollectionArtists_Role", "[Role] IN ('Unknown', 'Primary', 'Member', 'Associated')");
                    table.ForeignKey(
                        name: "FK_MusicCollectionArtists_MusicArtists_MusicArtistId",
                        column: x => x.MusicArtistId,
                        principalTable: "MusicArtists",
                        principalColumn: "MusicArtistId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusicCollectionArtists_MusicCollections_MusicCollectionId",
                        column: x => x.MusicCollectionId,
                        principalTable: "MusicCollections",
                        principalColumn: "MusicCollectionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicCollectionReleases",
                columns: table => new
                {
                    MusicCollectionId = table.Column<long>(type: "bigint", nullable: false),
                    MusicReleaseId = table.Column<long>(type: "bigint", nullable: false),
                    SourceCategory = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicCollectionReleases", x => new { x.MusicCollectionId, x.MusicReleaseId });
                    table.CheckConstraint("CK_MusicCollectionReleases_DisplayOrder", "[DisplayOrder] IS NULL OR [DisplayOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_MusicCollectionReleases_MusicCollections_MusicCollectionId",
                        column: x => x.MusicCollectionId,
                        principalTable: "MusicCollections",
                        principalColumn: "MusicCollectionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusicCollectionReleases_MusicReleases_MusicReleaseId",
                        column: x => x.MusicReleaseId,
                        principalTable: "MusicReleases",
                        principalColumn: "MusicReleaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicReleaseArtists",
                columns: table => new
                {
                    MusicReleaseId = table.Column<long>(type: "bigint", nullable: false),
                    MusicArtistId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicReleaseArtists", x => new { x.MusicReleaseId, x.MusicArtistId });
                    table.CheckConstraint("CK_MusicReleaseArtists_Role", "[Role] IN ('Unknown', 'Primary', 'Featured', 'Composer')");
                    table.ForeignKey(
                        name: "FK_MusicReleaseArtists_MusicArtists_MusicArtistId",
                        column: x => x.MusicArtistId,
                        principalTable: "MusicArtists",
                        principalColumn: "MusicArtistId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusicReleaseArtists_MusicReleases_MusicReleaseId",
                        column: x => x.MusicReleaseId,
                        principalTable: "MusicReleases",
                        principalColumn: "MusicReleaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MusicTracks",
                columns: table => new
                {
                    MusicTrackId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusicReleaseId = table.Column<long>(type: "bigint", nullable: false),
                    PositionLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    NormalizedTitle = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    DurationText = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicTracks", x => x.MusicTrackId);
                    table.CheckConstraint("CK_MusicTracks_Durations", "([DurationSeconds] IS NULL OR [DurationSeconds] >= 0) AND ([Sequence] IS NULL OR [Sequence] >= 0)");
                    table.ForeignKey(
                        name: "FK_MusicTracks_MusicReleases_MusicReleaseId",
                        column: x => x.MusicReleaseId,
                        principalTable: "MusicReleases",
                        principalColumn: "MusicReleaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalSourceIdentifiers",
                columns: table => new
                {
                    ExternalSourceIdentifierId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MusicArtistId = table.Column<long>(type: "bigint", nullable: true),
                    MusicCollectionId = table.Column<long>(type: "bigint", nullable: true),
                    MusicReleaseId = table.Column<long>(type: "bigint", nullable: true),
                    MusicTrackId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalSourceIdentifiers", x => x.ExternalSourceIdentifierId);
                    table.CheckConstraint("CK_ExternalSourceIdentifiers_ExactlyOneOwner", "(CASE WHEN [MusicArtistId] IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN [MusicCollectionId] IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN [MusicReleaseId] IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN [MusicTrackId] IS NOT NULL THEN 1 ELSE 0 END) = 1");
                    table.ForeignKey(
                        name: "FK_ExternalSourceIdentifiers_MusicArtists_MusicArtistId",
                        column: x => x.MusicArtistId,
                        principalTable: "MusicArtists",
                        principalColumn: "MusicArtistId");
                    table.ForeignKey(
                        name: "FK_ExternalSourceIdentifiers_MusicCollections_MusicCollectionId",
                        column: x => x.MusicCollectionId,
                        principalTable: "MusicCollections",
                        principalColumn: "MusicCollectionId");
                    table.ForeignKey(
                        name: "FK_ExternalSourceIdentifiers_MusicReleases_MusicReleaseId",
                        column: x => x.MusicReleaseId,
                        principalTable: "MusicReleases",
                        principalColumn: "MusicReleaseId");
                    table.ForeignKey(
                        name: "FK_ExternalSourceIdentifiers_MusicTracks_MusicTrackId",
                        column: x => x.MusicTrackId,
                        principalTable: "MusicTracks",
                        principalColumn: "MusicTrackId");
                });

            migrationBuilder.CreateTable(
                name: "MusicLocalFileReferences",
                columns: table => new
                {
                    MusicLocalFileReferenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MusicReleaseId = table.Column<long>(type: "bigint", nullable: false),
                    MusicTrackId = table.Column<long>(type: "bigint", nullable: true),
                    RelativePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    NormalizedPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    MediaKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicLocalFileReferences", x => x.MusicLocalFileReferenceId);
                    table.CheckConstraint("CK_MusicLocalFileReferences_RelativePath", "[NormalizedPath] <> '' AND [NormalizedPath] NOT LIKE '/%' AND [NormalizedPath] NOT LIKE '\\\\%' AND [NormalizedPath] NOT LIKE '[A-Za-z]:%' AND [NormalizedPath] NOT LIKE '%..%'");
                    table.ForeignKey(
                        name: "FK_MusicLocalFileReferences_MusicReleases_MusicReleaseId",
                        column: x => x.MusicReleaseId,
                        principalTable: "MusicReleases",
                        principalColumn: "MusicReleaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusicLocalFileReferences_MusicTracks_MusicTrackId",
                        column: x => x.MusicTrackId,
                        principalTable: "MusicTracks",
                        principalColumn: "MusicTrackId");
                });

            migrationBuilder.CreateTable(
                name: "MusicTrackArtists",
                columns: table => new
                {
                    MusicTrackId = table.Column<long>(type: "bigint", nullable: false),
                    MusicArtistId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicTrackArtists", x => new { x.MusicTrackId, x.MusicArtistId });
                    table.CheckConstraint("CK_MusicTrackArtists_Role", "[Role] IN ('Unknown', 'Primary', 'Featured', 'Composer')");
                    table.ForeignKey(
                        name: "FK_MusicTrackArtists_MusicArtists_MusicArtistId",
                        column: x => x.MusicArtistId,
                        principalTable: "MusicArtists",
                        principalColumn: "MusicArtistId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusicTrackArtists_MusicTracks_MusicTrackId",
                        column: x => x.MusicTrackId,
                        principalTable: "MusicTracks",
                        principalColumn: "MusicTrackId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSourceIdentifiers_MusicArtistId",
                table: "ExternalSourceIdentifiers",
                column: "MusicArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSourceIdentifiers_MusicCollectionId",
                table: "ExternalSourceIdentifiers",
                column: "MusicCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSourceIdentifiers_MusicReleaseId",
                table: "ExternalSourceIdentifiers",
                column: "MusicReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSourceIdentifiers_MusicTrackId",
                table: "ExternalSourceIdentifiers",
                column: "MusicTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalSourceIdentifiers_Provider_ResourceType_ExternalId",
                table: "ExternalSourceIdentifiers",
                columns: new[] { "Provider", "ResourceType", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicArtistAliases_MusicArtistId_NormalizedValue",
                table: "MusicArtistAliases",
                columns: new[] { "MusicArtistId", "NormalizedValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MusicArtists_NormalizedName",
                table: "MusicArtists",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_MusicCollectionArtists_MusicArtistId",
                table: "MusicCollectionArtists",
                column: "MusicArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicCollectionReleases_MusicReleaseId",
                table: "MusicCollectionReleases",
                column: "MusicReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicCollections_NormalizedName",
                table: "MusicCollections",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_MusicLocalFileReferences_MusicReleaseId_NormalizedPath",
                table: "MusicLocalFileReferences",
                columns: new[] { "MusicReleaseId", "NormalizedPath" },
                unique: true,
                filter: "[MusicTrackId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MusicLocalFileReferences_MusicTrackId_NormalizedPath",
                table: "MusicLocalFileReferences",
                columns: new[] { "MusicTrackId", "NormalizedPath" },
                unique: true,
                filter: "[MusicTrackId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleaseArtists_MusicArtistId",
                table: "MusicReleaseArtists",
                column: "MusicArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicReleases_NormalizedTitle",
                table: "MusicReleases",
                column: "NormalizedTitle");

            migrationBuilder.CreateIndex(
                name: "IX_MusicTrackArtists_MusicArtistId",
                table: "MusicTrackArtists",
                column: "MusicArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_MusicTracks_MusicReleaseId_PositionLabel_NormalizedTitle",
                table: "MusicTracks",
                columns: new[] { "MusicReleaseId", "PositionLabel", "NormalizedTitle" },
                unique: true,
                filter: "[PositionLabel] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MusicTracks_MusicReleaseId_Sequence_NormalizedTitle",
                table: "MusicTracks",
                columns: new[] { "MusicReleaseId", "Sequence", "NormalizedTitle" },
                unique: true,
                filter: "[Sequence] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalSourceIdentifiers");

            migrationBuilder.DropTable(
                name: "MusicArtistAliases");

            migrationBuilder.DropTable(
                name: "MusicCollectionArtists");

            migrationBuilder.DropTable(
                name: "MusicCollectionReleases");

            migrationBuilder.DropTable(
                name: "MusicLocalFileReferences");

            migrationBuilder.DropTable(
                name: "MusicReleaseArtists");

            migrationBuilder.DropTable(
                name: "MusicTrackArtists");

            migrationBuilder.DropTable(
                name: "MusicCollections");

            migrationBuilder.DropTable(
                name: "MusicArtists");

            migrationBuilder.DropTable(
                name: "MusicTracks");

            migrationBuilder.DropTable(
                name: "MusicReleases");
        }
    }
}
