using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeTrace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "LongTermCaseSequence",
                startValue: 1000L);

            migrationBuilder.CreateSequence<int>(
                name: "UnknownCaseSequence",
                startValue: 1000L);

            migrationBuilder.CreateSequence<int>(
                name: "UrgentCaseSequence",
                startValue: 1000L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "LongTermCaseSequence");

            migrationBuilder.DropSequence(
                name: "UnknownCaseSequence");

            migrationBuilder.DropSequence(
                name: "UrgentCaseSequence");
        }
    }
}
