using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiFileStorage.Monitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialFileMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "monitoring");

            migrationBuilder.CreateTable(
                name: "Locations",
                schema: "monitoring",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RootKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Observations",
                schema: "monitoring",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousRelativePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                    table.CheckConstraint("CK_Observations_Source", "[Source] IN ('LiveNotification', 'InventoryComparison', 'Monitor')");
                    table.ForeignKey(
                        name: "FK_Observations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "monitoring",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Snapshots",
                schema: "monitoring",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Completion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileCount = table.Column<long>(type: "bigint", nullable: false),
                    DirectoryCount = table.Column<long>(type: "bigint", nullable: false),
                    TotalBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                    table.CheckConstraint("CK_Snapshots_Completion", "[Completion] IN ('Complete', 'Partial', 'Unavailable', 'Cancelled')");
                    table.CheckConstraint("CK_Snapshots_Counts", "[FileCount] >= 0 AND [DirectoryCount] >= 0 AND [TotalBytes] >= 0");
                    table.CheckConstraint("CK_Snapshots_Times", "[FinishedAtUtc] >= [StartedAtUtc]");
                    table.ForeignKey(
                        name: "FK_Snapshots_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "monitoring",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Entries",
                schema: "monitoring",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDirectory = table.Column<bool>(type: "bit", nullable: false),
                    LengthBytes = table.Column<long>(type: "bigint", nullable: false),
                    LastWriteTimeUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Attributes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                    table.CheckConstraint("CK_Entries_Length", "[LengthBytes] >= 0 AND ([IsDirectory] = 0 OR [LengthBytes] = 0)");
                    table.ForeignKey(
                        name: "FK_Entries_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalSchema: "monitoring",
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_SnapshotId_Id",
                schema: "monitoring",
                table: "Entries",
                columns: new[] { "SnapshotId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_RootKey",
                schema: "monitoring",
                table: "Locations",
                column: "RootKey");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_LocationId_ObservedAtUtc_Id",
                schema: "monitoring",
                table: "Observations",
                columns: new[] { "LocationId", "ObservedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_LocationId_Completion_FinishedAtUtc_Id",
                schema: "monitoring",
                table: "Snapshots",
                columns: new[] { "LocationId", "Completion", "FinishedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entries",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "Observations",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "Snapshots",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "Locations",
                schema: "monitoring");
        }
    }
}
