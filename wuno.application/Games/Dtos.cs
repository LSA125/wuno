using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;

namespace Wuno.Application.Games
{
    public record NewGameRequest(int PlayerCount = 2, int TargetWins = 2);
    public record NewGameResponse(Guid GameId, Guid TurnId, int NextSeat, int PlayerCount, int TargetWins);
    public record GameStateResponse(object State);
    public record ErrorResponse(string Reason);
    public record SubmitWordRequest(int Seat, string Word);
    public record SubmitWordResponse(bool Ok, string? Reason);
    public record JoinGameRequest(Guid GameId, Guid UserId);
    public record LeaveGameRequest(Guid GameId, Guid PlayerId);
    public record GameState(Guid GameId, GameStatus Status, int NextSeat, int Direction, int TargetWins, List<Player> Players, Round CurrentRound, Turn CurrentTurn);
}
