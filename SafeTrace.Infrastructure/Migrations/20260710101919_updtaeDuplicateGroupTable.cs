using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updtaeDuplicateGroupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuplicateGroups_Cases_MasterCaseId",
                table: "DuplicateGroups");

            migrationBuilder.DropIndex(
                name: "IX_DuplicateGroups_MasterCaseId",
                table: "DuplicateGroups");

            migrationBuilder.DropColumn(
                name: "MasterCaseId",
                table: "DuplicateGroups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MasterCaseId",
                table: "DuplicateGroups",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateGroups_MasterCaseId",
                table: "DuplicateGroups",
                column: "MasterCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_DuplicateGroups_Cases_MasterCaseId",
                table: "DuplicateGroups",
                column: "MasterCaseId",
                principalTable: "Cases",
                principalColumn: "Id");
        }
    }
}
