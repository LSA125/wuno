using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wuno.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHostBoolToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHost",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHost",
                table: "Players");
        }
    }
}
