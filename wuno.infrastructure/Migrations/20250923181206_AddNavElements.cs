using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace wuno.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNavElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StartLetter",
                table: "Turns",
                type: "nvarchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Games",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Turns_RoundId",
                table: "Turns",
                column: "RoundId");

            migrationBuilder.AddForeignKey(
                name: "FK_Turns_Rounds_RoundId",
                table: "Turns",
                column: "RoundId",
                principalTable: "Rounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turns_Rounds_RoundId",
                table: "Turns");

            migrationBuilder.DropIndex(
                name: "IX_Turns_RoundId",
                table: "Turns");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Games");

            migrationBuilder.AlterColumn<string>(
                name: "StartLetter",
                table: "Turns",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1)",
                oldNullable: true);
        }
    }
}
