using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeArena.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_ProblemId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_UserId",
                table: "Submissions");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ProblemId_UserId_Status",
                table: "Submissions",
                columns: new[] { "ProblemId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_UserId_Status",
                table: "Submissions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_ProblemId_UserId_Status",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_UserId_Status",
                table: "Submissions");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ProblemId",
                table: "Submissions",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_UserId",
                table: "Submissions",
                column: "UserId");
        }
    }
}
