using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VerificationUserStatusIOisVerified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "ApplicationUsers");

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "ApplicationUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "ApplicationUsers");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "ApplicationUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
