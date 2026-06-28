using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConfigCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TName",
                table: "Cases",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Cases",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "SName",
                table: "Cases",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Relation",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "PreviousStatus",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LName",
                table: "Cases",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "FName",
                table: "Cases",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Cases",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CommunicationPhone",
                table: "Cases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CaseType",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CaseCode",
                table: "Cases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "10000");

            migrationBuilder.CreateIndex(
                name: "IX_UrgentCases_Status_CreatedAt",
                table: "Cases",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UrgentCases_Status_EndDate",
                table: "Cases",
                columns: new[] { "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UrgentCases_UserId_LimitReachDate",
                table: "Cases",
                columns: new[] { "UserId", "LimitReachDate" });

            migrationBuilder.CreateIndex(
                name: "UIX_Cases_CaseCode",
                table: "Cases",
                column: "CaseCode",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cases_Age",
                table: "Cases",
                sql: "[Age] BETWEEN 0 AND 120");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UrgentCase_EndDate",
                table: "Cases",
                sql: "([Discriminator] <> 'UrgentCase') OR ([EndDate] > [CreatedAt])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UrgentCase_LimitReachDate",
                table: "Cases",
                sql: "([Discriminator] <> 'UrgentCase') OR ([LimitReachDate] > [CreatedAt])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UrgentCases_Status_CreatedAt",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_UrgentCases_Status_EndDate",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_UrgentCases_UserId_LimitReachDate",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "UIX_Cases_CaseCode",
                table: "Cases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Cases_Age",
                table: "Cases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UrgentCase_EndDate",
                table: "Cases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UrgentCase_LimitReachDate",
                table: "Cases");

            migrationBuilder.AlterColumn<string>(
                name: "TName",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Cases",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "SName",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<int>(
                name: "Relation",
                table: "Cases",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "PreviousStatus",
                table: "Cases",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LName",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<int>(
                name: "Gender",
                table: "Cases",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FName",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CommunicationPhone",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CaseType",
                table: "Cases",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CaseCode",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "10000",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
