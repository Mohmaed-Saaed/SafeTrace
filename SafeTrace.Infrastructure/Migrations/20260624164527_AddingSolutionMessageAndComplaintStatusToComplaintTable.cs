using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingSolutionMessageAndComplaintStatusToComplaintTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComplaintStatus",
                table: "Complaints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SolutionMessage",
                table: "Complaints",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComplaintStatus",
                table: "Complaints");

            migrationBuilder.DropColumn(
                name: "SolutionMessage",
                table: "Complaints");
        }
    }
}
