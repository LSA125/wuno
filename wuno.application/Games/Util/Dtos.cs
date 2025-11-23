using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.domain.Rules;

namespace Wuno.Application.Games.Util
{
    public record TmpUserRequest(Guid UserId, string Name, string? IconUrl, string? Email);
    public record RegUserRequest(Guid UserId, string Pass, string? Name, string? IconUrl, string? Email);
    public record UserResponse(bool Ok, Guid? UserId, string? Name, string? IconUrl, string? Email, string? Msg);
    public record NewGameRequest(int PlayerCount, int TargetWins);
    public record NewGameResponse(string GameCode, int PlayerCount, int TargetWins);
    public record GameCodeResponse(bool Ok, bool? InGame, string? GameCode);
    public record ErrorResponse(string Code, string Reason);
    public record SubmitWordResponse(bool Ok, string? Reason);
    public record JoinGameRequest(Guid GameId, Guid UserId);
    public record JoinGameResponse(Guid PlayerId, GameState State);
    public record LeaveGameRequest(Guid GameId, Guid PlayerId);
    public record PlayerState(Guid PlayerId, int Seat, bool IsActive, bool IsConnected, string Name, string? IconUrl, int RoundWins, string? LastWord);
    public record TurnState(Guid TurnId,int Index, int Seat, DateTime StartedAt, DateTime DueAt, int MinLen, bool FreeStart, List<EffectState> Effects);
    public record RoundState(Guid RoundId, int Index, Guid? WinnerId, DateTime? StartedAt, DateTime? EndedAt);
    public record GameState(Guid GameId, GameStatus Status, int NextSeat, int Direction, int TargetWins, List<PlayerState> Players, RoundState CurrentRound, TurnState CurrentTurn);
}
