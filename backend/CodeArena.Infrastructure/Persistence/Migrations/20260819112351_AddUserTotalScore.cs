using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeArena.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTotalScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalScore",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalScore",
                table: "Users");
        }
    }
}
