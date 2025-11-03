using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wuno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueEnforcedOnlyRegisteredName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Name",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<bool>(
                name: "IsRegistered",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NameNormalized",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NameNormalized",
                table: "Users",
                column: "NameNormalized",
                unique: true,
                filter: "[IsRegistered] = 1 AND [NameNormalized] IS NOT NULL AND [NameNormalized] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_NameNormalized",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsRegistered",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NameNormalized",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true,
                filter: "[Name] IS NOT NULL AND [Name] <> ''");
        }
    }
}
