using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeArena.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPromotedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PromotedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromotedAt",
                table: "Users");
        }
    }
}
