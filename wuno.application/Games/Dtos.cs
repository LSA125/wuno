using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.domain.Rules;

namespace Wuno.Application.Games
{
    public record NewGameRequest(int PlayerCount, int TargetWins);
    public record NewGameResponse(Guid GameId,int NextSeat, int PlayerCount, int TargetWins);
    public record GameStateResponse(object State);
    public record ErrorResponse(string Reason);
    public record SubmitWordRequest(int Seat, string Word);
    public record SubmitWordResponse(bool Ok, string? Reason);
    public record JoinGameRequest(Guid GameId, Guid UserId);
    public record JoinGameResponse(Guid GameId, GameState State);
    public record LeaveGameRequest(Guid GameId, Guid PlayerId);
    public record PlayerState(Guid PlayerId, int Seat, bool IsActive, bool IsConnected, bool IsHost, string Name, string? IconUrl, int RoundWins, string? LastWord);
    public record TurnState(Guid TurnId,int Index, int Seat, DateTime StartedAt, int DurationSec, int MinLen, bool FreeStart, bool Req2Vowels);
    public record RoundState(Guid RoundId, int Index, bool Active, Guid? WinnerId, DateTime? StartedAt, DateTime? EndedAt);
    public record GameState(Guid GameId, GameStatus Status, int NextSeat, int Direction, int TargetWins, List<PlayerState> Players, RoundState CurrentRound, TurnState CurrentTurn);
}
