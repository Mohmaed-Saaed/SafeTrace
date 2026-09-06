using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupFacebookIntegrationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacebookImportedPosts_Cases_DuplicateCaseId",
                table: "FacebookImportedPosts");

            migrationBuilder.DropIndex(
                name: "IX_FacebookImportedPosts_DuplicateCaseId",
                table: "FacebookImportedPosts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FacebookImportedPosts_CaseReferences",
                table: "FacebookImportedPosts");

            migrationBuilder.DropColumn(
                name: "DuplicateCaseId",
                table: "FacebookImportedPosts");

            migrationBuilder.DropColumn(
                name: "LocationAccuracy",
                table: "FacebookImportedPosts");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "FacebookImportedPosts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FacebookImportedPostFiles");

            migrationBuilder.DropColumn(
                name: "FacebookMediaId",
                table: "FacebookImportedPostFiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DuplicateCaseId",
                table: "FacebookImportedPosts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationAccuracy",
                table: "FacebookImportedPosts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "FacebookImportedPosts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FacebookImportedPostFiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FacebookMediaId",
                table: "FacebookImportedPostFiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacebookImportedPosts_DuplicateCaseId",
                table: "FacebookImportedPosts",
                column: "DuplicateCaseId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FacebookImportedPosts_CaseReferences",
                table: "FacebookImportedPosts",
                sql: "[CaseId] IS NULL OR [DuplicateCaseId] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FacebookImportedPosts_Cases_DuplicateCaseId",
                table: "FacebookImportedPosts",
                column: "DuplicateCaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
