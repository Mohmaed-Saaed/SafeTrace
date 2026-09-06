using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFacebookImportedPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacebookImportedPosts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacebookPageId = table.Column<long>(type: "bigint", nullable: false),
                    FacebookPostId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    FName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    SName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    TName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    LName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Age = table.Column<int>(type: "int", nullable: true),
                    Government = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CommunicationPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Relation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    LocationAccuracy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacebookImportedPosts", x => x.Id);
                    table.CheckConstraint("CK_FacebookImportedPosts_Age", "[Age] IS NULL OR ([Age] >= 0 AND [Age] <= 120)");
                    table.CheckConstraint("CK_FacebookImportedPosts_Confidence", "[Confidence] IS NULL OR ([Confidence] >= 0 AND [Confidence] <= 1)");
                    table.ForeignKey(
                        name: "FK_FacebookImportedPosts_FacebookPages_FacebookPageId",
                        column: x => x.FacebookPageId,
                        principalTable: "FacebookPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacebookImportedPostFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacebookImportedPostId = table.Column<long>(type: "bigint", nullable: false),
                    FacebookMediaId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacebookImportedPostFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacebookImportedPostFiles_FacebookImportedPosts_FacebookImportedPostId",
                        column: x => x.FacebookImportedPostId,
                        principalTable: "FacebookImportedPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacebookImportedPostFiles_FacebookImportedPostId",
                table: "FacebookImportedPostFiles",
                column: "FacebookImportedPostId");

            migrationBuilder.CreateIndex(
                name: "IX_FacebookImportedPosts_FacebookPageId_FacebookPostId",
                table: "FacebookImportedPosts",
                columns: new[] { "FacebookPageId", "FacebookPostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacebookImportedPosts_Status_Id",
                table: "FacebookImportedPosts",
                columns: new[] { "Status", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacebookImportedPostFiles");

            migrationBuilder.DropTable(
                name: "FacebookImportedPosts");
        }
    }
}
