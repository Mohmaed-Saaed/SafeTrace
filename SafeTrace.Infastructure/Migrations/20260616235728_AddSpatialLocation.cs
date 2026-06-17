using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpatialLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationLatitude",
                table: "UrgentCases");

            migrationBuilder.DropColumn(
                name: "LocationLongitude",
                table: "UrgentCases");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "UrgentCases",
                type: "geography",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "UrgentCases");

            migrationBuilder.AddColumn<double>(
                name: "LocationLatitude",
                table: "UrgentCases",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LocationLongitude",
                table: "UrgentCases",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
