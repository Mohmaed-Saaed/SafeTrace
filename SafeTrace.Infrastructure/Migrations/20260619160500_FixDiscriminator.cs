using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDiscriminator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "LimitReachDate",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PoliceReportImage",
                table: "Cases");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Cases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Government",
                table: "Cases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Cases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "LongTermMissingCase",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PoliceReportImage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongTermMissingCase", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LongTermMissingCases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongTermMissingCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongTermMissingCases_Cases_Id",
                        column: x => x.Id,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UrgentCase",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Location = table.Column<Point>(type: "geography", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LimitReachDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrgentCase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrgentCase_Cases_Id",
                        column: x => x.Id,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LongTermMissingCase");

            migrationBuilder.DropTable(
                name: "LongTermMissingCases");

            migrationBuilder.DropTable(
                name: "UrgentCase");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Cases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Government",
                table: "Cases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Cases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Cases",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LimitReachDate",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Cases",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoliceReportImage",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
