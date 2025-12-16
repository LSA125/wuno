using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wuno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingTimeToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "RemainingTime",
                table: "Players",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingTime",
                table: "Players");
        }
    }
}
