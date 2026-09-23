using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiFileStorage.Monitoring.Data.Migrations;

/// <inheritdoc />
public partial class OptimizeObservationCursor : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Observations_LocationId_ObservedAtUtc_Id",
            schema: "monitoring",
            table: "Observations");

        migrationBuilder.CreateIndex(
            name: "IX_Observations_LocationId_Id",
            schema: "monitoring",
            table: "Observations",
            columns: new[] { "LocationId", "Id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Observations_LocationId_Id",
            schema: "monitoring",
            table: "Observations");

        migrationBuilder.CreateIndex(
            name: "IX_Observations_LocationId_ObservedAtUtc_Id",
            schema: "monitoring",
            table: "Observations",
            columns: new[] { "LocationId", "ObservedAtUtc", "Id" });
    }
}
