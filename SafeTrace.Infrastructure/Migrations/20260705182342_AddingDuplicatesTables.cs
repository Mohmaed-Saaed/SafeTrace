using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingDuplicatesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DuplicateGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MasterCaseId = table.Column<long>(type: "bigint", nullable: true),
                    GroupStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuplicateGroups_Cases_MasterCaseId",
                        column: x => x.MasterCaseId,
                        principalTable: "Cases",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DuplicateGroupCases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DuplicateGroupId = table.Column<long>(type: "bigint", nullable: false),
                    CaseId = table.Column<long>(type: "bigint", nullable: false),
                    SimilarityScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MatchedBy = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateGroupCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuplicateGroupCases_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DuplicateGroupCases_DuplicateGroups_DuplicateGroupId",
                        column: x => x.DuplicateGroupId,
                        principalTable: "DuplicateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateGroupCases_CaseId",
                table: "DuplicateGroupCases",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateGroupCases_DuplicateGroupId",
                table: "DuplicateGroupCases",
                column: "DuplicateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateGroups_MasterCaseId",
                table: "DuplicateGroups",
                column: "MasterCaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuplicateGroupCases");

            migrationBuilder.DropTable(
                name: "DuplicateGroups");
        }
    }
}
