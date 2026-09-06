using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFacebookPostReviewAndPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CaseId",
                table: "FacebookImportedPosts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DuplicateCaseId",
                table: "FacebookImportedPosts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FacebookImportedPosts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_FacebookImportedPosts_CaseId",
                table: "FacebookImportedPosts",
                column: "CaseId",
                unique: true,
                filter: "[CaseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FacebookImportedPosts_DuplicateCaseId",
                table: "FacebookImportedPosts",
                column: "DuplicateCaseId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FacebookImportedPosts_CaseReferences",
                table: "FacebookImportedPosts",
                sql: "[CaseId] IS NULL OR [DuplicateCaseId] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FacebookImportedPosts_Cases_CaseId",
                table: "FacebookImportedPosts",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FacebookImportedPosts_Cases_DuplicateCaseId",
                table: "FacebookImportedPosts",
                column: "DuplicateCaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacebookImportedPosts_Cases_CaseId",
                table: "FacebookImportedPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_FacebookImportedPosts_Cases_DuplicateCaseId",
                table: "FacebookImportedPosts");

            migrationBuilder.DropIndex(
                name: "IX_FacebookImportedPosts_CaseId",
                table: "FacebookImportedPosts");

            migrationBuilder.DropIndex(
                name: "IX_FacebookImportedPosts_DuplicateCaseId",
                table: "FacebookImportedPosts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FacebookImportedPosts_CaseReferences",
                table: "FacebookImportedPosts");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "FacebookImportedPosts");

            migrationBuilder.DropColumn(
                name: "DuplicateCaseId",
                table: "FacebookImportedPosts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FacebookImportedPosts");
        }
    }
}
