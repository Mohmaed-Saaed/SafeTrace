using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceUserLatLngWithPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLocationLatitude",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "CurrentLocationLongitude",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "HomeLocationLatitude",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "HomeLocationLongitude",
                table: "ApplicationUsers");

            migrationBuilder.AddColumn<Point>(
                name: "CurrentLocation",
                table: "ApplicationUsers",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "HomeLocation",
                table: "ApplicationUsers",
                type: "geography",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLocation",
                table: "ApplicationUsers");

            migrationBuilder.DropColumn(
                name: "HomeLocation",
                table: "ApplicationUsers");

            migrationBuilder.AddColumn<double>(
                name: "CurrentLocationLatitude",
                table: "ApplicationUsers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLocationLongitude",
                table: "ApplicationUsers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeLocationLatitude",
                table: "ApplicationUsers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HomeLocationLongitude",
                table: "ApplicationUsers",
                type: "float",
                nullable: true);
        }
    }
}
