using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wuno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Effects_Players_PlayerId",
                table: "Effects");

            migrationBuilder.DropIndex(
                name: "IX_Users_NameNormalized",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Turns_RoundId",
                table: "Turns");

            migrationBuilder.DropIndex(
                name: "IX_Effects_PlayerId",
                table: "Effects");

            migrationBuilder.DropIndex(
                name: "IX_Effects_RoundId",
                table: "Effects");

            migrationBuilder.DropColumn(
                name: "DurationSec",
                table: "Turns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Turns");

            migrationBuilder.DropColumn(
                name: "StartLetter",
                table: "Turns");

            migrationBuilder.DropColumn(
                name: "WordLen",
                table: "Turns");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                table: "Effects");

            migrationBuilder.RenameColumn(
                name: "NextSeat",
                table: "Games",
                newName: "CurSeat");

            migrationBuilder.RenameColumn(
                name: "AppliesOn",
                table: "Effects",
                newName: "TargetSeat");

            migrationBuilder.AlterColumn<string>(
                name: "Word",
                table: "Turns",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnsPlayedThisRound",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentRoundId",
                table: "Games",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentTurnId",
                table: "Games",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastWord",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppliesOnTurn",
                table: "Effects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NameNormalized",
                table: "Users",
                column: "NameNormalized",
                unique: true,
                filter: "[NameNormalized] IS NOT NULL AND [NameNormalized] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_RoundId_Word",
                table: "Turns",
                columns: new[] { "RoundId", "Word" },
                unique: true,
                filter: "[RoundId] IS NOT NULL AND [Word] IS NOT NULL AND [Word] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CurrentRoundId",
                table: "Games",
                column: "CurrentRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CurrentTurnId",
                table: "Games",
                column: "CurrentTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_Effects_RoundId_TargetSeat_AppliesOnTurn",
                table: "Effects",
                columns: new[] { "RoundId", "TargetSeat", "AppliesOnTurn" });

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Rounds_CurrentRoundId",
                table: "Games",
                column: "CurrentRoundId",
                principalTable: "Rounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Turns_CurrentTurnId",
                table: "Games",
                column: "CurrentTurnId",
                principalTable: "Turns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Rounds_CurrentRoundId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_Turns_CurrentTurnId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Users_NameNormalized",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Turns_RoundId_Word",
                table: "Turns");

            migrationBuilder.DropIndex(
                name: "IX_Games_CurrentRoundId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_CurrentTurnId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Effects_RoundId_TargetSeat_AppliesOnTurn",
                table: "Effects");

            migrationBuilder.DropColumn(
                name: "TurnsPlayedThisRound",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CurrentRoundId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "CurrentTurnId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "LastWord",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "AppliesOnTurn",
                table: "Effects");

            migrationBuilder.RenameColumn(
                name: "CurSeat",
                table: "Games",
                newName: "NextSeat");

            migrationBuilder.RenameColumn(
                name: "TargetSeat",
                table: "Effects",
                newName: "AppliesOn");

            migrationBuilder.AlterColumn<string>(
                name: "Word",
                table: "Turns",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSec",
                table: "Turns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Turns",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "StartLetter",
                table: "Turns",
                type: "nvarchar(1)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WordLen",
                table: "Turns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Rounds",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PlayerId",
                table: "Effects",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Users_NameNormalized",
                table: "Users",
                column: "NameNormalized",
                unique: true,
                filter: "[IsRegistered] = 1 AND [NameNormalized] IS NOT NULL AND [NameNormalized] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_RoundId",
                table: "Turns",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Effects_PlayerId",
                table: "Effects",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Effects_RoundId",
                table: "Effects",
                column: "RoundId");

            migrationBuilder.AddForeignKey(
                name: "FK_Effects_Players_PlayerId",
                table: "Effects",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
