using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBaseCasesProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "LongTermMissingCases");

            migrationBuilder.DropColumn(
                name: "Government",
                table: "LongTermMissingCases");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "LongTermMissingCases");

            migrationBuilder.DropColumn(
                name: "LocationLatitude",
                table: "BaseCases");

            migrationBuilder.DropColumn(
                name: "LocationLongitude",
                table: "BaseCases");

            migrationBuilder.RenameColumn(
                name: "LostDate",
                table: "BaseCases",
                newName: "EventDate");

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

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "BaseCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Government",
                table: "BaseCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "BaseCases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationLatitude",
                table: "UrgentCases");

            migrationBuilder.DropColumn(
                name: "LocationLongitude",
                table: "UrgentCases");

            migrationBuilder.DropColumn(
                name: "City",
                table: "BaseCases");

            migrationBuilder.DropColumn(
                name: "Government",
                table: "BaseCases");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "BaseCases");

            migrationBuilder.RenameColumn(
                name: "EventDate",
                table: "BaseCases",
                newName: "LostDate");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "LongTermMissingCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Government",
                table: "LongTermMissingCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "LongTermMissingCases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "LocationLatitude",
                table: "BaseCases",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LocationLongitude",
                table: "BaseCases",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
