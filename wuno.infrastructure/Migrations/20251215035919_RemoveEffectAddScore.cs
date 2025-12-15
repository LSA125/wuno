using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wuno.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEffectAddScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Effects");

            migrationBuilder.DropColumn(
                name: "FreeStart",
                table: "Turns");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Turns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Turns");

            migrationBuilder.AddColumn<bool>(
                name: "FreeStart",
                table: "Turns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Effects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppliesOnTurn = table.Column<int>(type: "int", nullable: false),
                    ConsumedTurnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceTurnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetSeat = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Effects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Effects_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Effects_Rounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "Rounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Effects_GameId",
                table: "Effects",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Effects_RoundId_TargetSeat_AppliesOnTurn",
                table: "Effects",
                columns: new[] { "RoundId", "TargetSeat", "AppliesOnTurn" });
        }
    }
}
