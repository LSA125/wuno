using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;

namespace Wuno.Application.Games
{
    public record NewGameRequest(int PlayerCount, int TargetWins);
    public record NewGameResponse(Guid GameId, Guid RoundId, int NextSeat, int PlayerCount, int TargetWins);
    public record GameStateResponse(object State);
    public record ErrorResponse(string Reason);
    public record SubmitWordRequest(int Seat, string Word);
    public record SubmitWordResponse(bool Ok, string? Reason);
    public record JoinGameRequest(Guid GameId, Guid UserId);
    public record JoinGameResponse(Guid GameId, GameState State);
    public record LeaveGameRequest(Guid GameId, Guid PlayerId);
    public record EffectState(EffectType Type, int Value);
    public record PlayerState(Guid PlayerId, int Seat, bool IsActive, bool IsConnected, bool IsHost, string Name, string? IconUrl, int RoundWins, string? LastWord, int LastWordLength);
    public record TurnState(Guid TurnId,int Index, int Seat, DateTime StartedAt, int DurationSec, bool Completed, string? WordPlayed, List<EffectState> Effects);
    public record RoundState(Guid RoundId, int Index, bool Active, Guid? WinnerId, DateTime? StartedAt, DateTime? EndedAt);
    public record GameState(Guid GameId, GameStatus Status, int NextSeat, int Direction, int TargetWins, List<PlayerState> Players, Round CurrentRound, Turn CurrentTurn);
}
