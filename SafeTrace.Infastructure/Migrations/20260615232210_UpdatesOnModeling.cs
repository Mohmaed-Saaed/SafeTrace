using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatesOnModeling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseCases_AgeCategories_AgeCategoryId",
                table: "BaseCases");

            migrationBuilder.DropForeignKey(
                name: "FK_BaseCases_ApplicationUsers_UserId",
                table: "BaseCases");

            migrationBuilder.DropForeignKey(
                name: "FK_CasePhotos_BaseCases_CaseId",
                table: "CasePhotos");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_BaseCases_CaseId",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_FoundPersonInfos_BaseCases_CaseId",
                table: "FoundPersonInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_LongTermMissingCases_BaseCases_Id",
                table: "LongTermMissingCases");

            migrationBuilder.DropForeignKey(
                name: "FK_UnknownCase_BaseCases_Id",
                table: "UnknownCase");

            migrationBuilder.DropForeignKey(
                name: "FK_UrgentCases_BaseCases_Id",
                table: "UrgentCases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnknownCase",
                table: "UnknownCase");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BaseCases",
                table: "BaseCases");

            migrationBuilder.RenameTable(
                name: "UnknownCase",
                newName: "UnknownCases");

            migrationBuilder.RenameTable(
                name: "BaseCases",
                newName: "BaseCase");

            migrationBuilder.RenameIndex(
                name: "IX_BaseCases_UserId",
                table: "BaseCase",
                newName: "IX_BaseCase_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_BaseCases_AgeCategoryId",
                table: "BaseCase",
                newName: "IX_BaseCase_AgeCategoryId");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "UnknownCases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Government",
                table: "UnknownCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "UnknownCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnknownCases",
                table: "UnknownCases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BaseCase",
                table: "BaseCase",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BaseCase_AgeCategories_AgeCategoryId",
                table: "BaseCase",
                column: "AgeCategoryId",
                principalTable: "AgeCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseCase_ApplicationUsers_UserId",
                table: "BaseCase",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CasePhotos_BaseCase_CaseId",
                table: "CasePhotos",
                column: "CaseId",
                principalTable: "BaseCase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_BaseCase_CaseId",
                table: "Chats",
                column: "CaseId",
                principalTable: "BaseCase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FoundPersonInfos_BaseCase_CaseId",
                table: "FoundPersonInfos",
                column: "CaseId",
                principalTable: "BaseCase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LongTermMissingCases_BaseCase_Id",
                table: "LongTermMissingCases",
                column: "Id",
                principalTable: "BaseCase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnknownCases_BaseCase_Id",
                table: "UnknownCases",
                column: "Id",
                principalTable: "BaseCase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UrgentCases_BaseCase_Id",
                table: "UrgentCases",
                column: "Id",
                principalTable: "BaseCase",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseCase_AgeCategories_AgeCategoryId",
                table: "BaseCase");

            migrationBuilder.DropForeignKey(
                name: "FK_BaseCase_ApplicationUsers_UserId",
                table: "BaseCase");

            migrationBuilder.DropForeignKey(
                name: "FK_CasePhotos_BaseCase_CaseId",
                table: "CasePhotos");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_BaseCase_CaseId",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_FoundPersonInfos_BaseCase_CaseId",
                table: "FoundPersonInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_LongTermMissingCases_BaseCase_Id",
                table: "LongTermMissingCases");

            migrationBuilder.DropForeignKey(
                name: "FK_UnknownCases_BaseCase_Id",
                table: "UnknownCases");

            migrationBuilder.DropForeignKey(
                name: "FK_UrgentCases_BaseCase_Id",
                table: "UrgentCases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnknownCases",
                table: "UnknownCases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BaseCase",
                table: "BaseCase");

            migrationBuilder.RenameTable(
                name: "UnknownCases",
                newName: "UnknownCase");

            migrationBuilder.RenameTable(
                name: "BaseCase",
                newName: "BaseCases");

            migrationBuilder.RenameIndex(
                name: "IX_BaseCase_UserId",
                table: "BaseCases",
                newName: "IX_BaseCases_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_BaseCase_AgeCategoryId",
                table: "BaseCases",
                newName: "IX_BaseCases_AgeCategoryId");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "UnknownCase",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Government",
                table: "UnknownCase",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "UnknownCase",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnknownCase",
                table: "UnknownCase",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BaseCases",
                table: "BaseCases",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BaseCases_AgeCategories_AgeCategoryId",
                table: "BaseCases",
                column: "AgeCategoryId",
                principalTable: "AgeCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseCases_ApplicationUsers_UserId",
                table: "BaseCases",
                column: "UserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CasePhotos_BaseCases_CaseId",
                table: "CasePhotos",
                column: "CaseId",
                principalTable: "BaseCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_BaseCases_CaseId",
                table: "Chats",
                column: "CaseId",
                principalTable: "BaseCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FoundPersonInfos_BaseCases_CaseId",
                table: "FoundPersonInfos",
                column: "CaseId",
                principalTable: "BaseCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LongTermMissingCases_BaseCases_Id",
                table: "LongTermMissingCases",
                column: "Id",
                principalTable: "BaseCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnknownCase_BaseCases_Id",
                table: "UnknownCase",
                column: "Id",
                principalTable: "BaseCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UrgentCases_BaseCases_Id",
                table: "UrgentCases",
                column: "Id",
                principalTable: "BaseCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
