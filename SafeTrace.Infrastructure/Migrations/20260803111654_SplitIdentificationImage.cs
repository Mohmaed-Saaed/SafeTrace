using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitIdentificationImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IdentificationImage",
                table: "ApplicationUsers",
                newName: "IdentificationImageback");

            migrationBuilder.AddColumn<string>(
                name: "IdentificationImageFront",
                table: "ApplicationUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentificationImageFront",
                table: "ApplicationUsers");

            migrationBuilder.RenameColumn(
                name: "IdentificationImageback",
                table: "ApplicationUsers",
                newName: "IdentificationImage");
        }
    }
}
