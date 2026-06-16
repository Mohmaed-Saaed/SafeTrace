using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnknownnn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnknownCases_BaseCases_Id",
                table: "UnknownCases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnknownCases",
                table: "UnknownCases");

            migrationBuilder.RenameTable(
                name: "UnknownCases",
                newName: "UnknownCase");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "UnknownCase",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Government",
                table: "UnknownCase",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "UnknownCase",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnknownCase",
                table: "UnknownCase",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnknownCase_BaseCases_Id",
                table: "UnknownCase",
                column: "Id",
                principalTable: "BaseCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnknownCase_BaseCases_Id",
                table: "UnknownCase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnknownCase",
                table: "UnknownCase");

            migrationBuilder.DropColumn(
                name: "City",
                table: "UnknownCase");

            migrationBuilder.DropColumn(
                name: "Government",
                table: "UnknownCase");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "UnknownCase");

            migrationBuilder.RenameTable(
                name: "UnknownCase",
                newName: "UnknownCases");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnknownCases",
                table: "UnknownCases",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnknownCases_BaseCases_Id",
                table: "UnknownCases",
                column: "Id",
                principalTable: "BaseCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
