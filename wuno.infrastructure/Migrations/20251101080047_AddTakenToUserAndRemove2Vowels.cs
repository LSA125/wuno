using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wuno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTakenToUserAndRemove2Vowels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Require2Vowels",
                table: "Turns");

            migrationBuilder.RenameColumn(
                name: "IsHost",
                table: "Players",
                newName: "IsTaken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsTaken",
                table: "Players",
                newName: "IsHost");

            migrationBuilder.AddColumn<bool>(
                name: "Require2Vowels",
                table: "Turns",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
