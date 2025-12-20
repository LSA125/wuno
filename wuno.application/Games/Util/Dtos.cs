using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;

namespace Wuno.Application.Games.Util
{
    public record TmpUserRequest(Guid UserId, string Name, string? IconUrl, string? Email);
    public record RegUserRequest(Guid UserId, string Pass, string? Name, string? IconUrl, string? Email);
    public record UserResponse(bool Ok, Guid? UserId, string? Name, string? IconUrl, string? Email, string? Msg);
    public record AuthResponse(bool Ok, Guid? UserId, string? Name, string? IconUrl, string? Email, string? Msg, string? AccessToken);
    public record NewGameRequest(int PlayerCount, int TargetWins, bool IsPublic = false);
    public record NewGameResponse(string GameCode, int PlayerCount, int TargetWins);
    public record MatchmakingResponse(bool Ok, string GameCode, bool WasCreated);
    public record GameCodeResponse(bool Ok, bool? InGame, string? GameCode);
    public record ErrorResponse(string Code, string Reason);
    public record SubmitWordResponse(bool Ok, string? Reason);
    public record JoinGameRequest(Guid GameId, Guid UserId);
    public record JoinGameResponse(Guid PlayerId, GameState State);
    public record LeaveGameRequest(Guid GameId, Guid PlayerId);
    public record PlayerState(Guid PlayerId, Guid? UserId, int Seat, bool IsActive, bool IsConnected, string Name, string? IconUrl, int RoundWins, string? LastWord, double RemainingTime);
    public record TurnState(Guid TurnId, int Index, int Seat, DateTime StartedAt, DateTime DueAt, int MinLen, int Score);
    public record RoundState(Guid RoundId, int Index, Guid? WinnerId, DateTime? StartedAt, DateTime? EndedAt);
    public record TurnHistoryState(Guid TurnId, int Index, int Seat, string Word, int MinLen, int Score);
    public record GameState(Guid GameId, GameStatus Status, int NextSeat, int Direction, int TargetWins, string? LastWord, List<PlayerState> Players, RoundState? CurrentRound, TurnState? CurrentTurn);
    public record ProcessTurnOutcome(bool Ok, string? Reason, GameState? State, TurnHistoryState? CompletedTurn);
    public record TopWordEntry(string Word, int Score);
    public record UserStatsResponse(
        bool Ok,
        int GamesPlayed,
        int GamesWon,
        double WinRate,
        int RoundsWon,
        int HighestSingleRoundScore,
        List<TopWordEntry> TopWords,
        int TotalWordsPlayed,
        double AverageWordLength,
        string? LongestWord
    );
    public record InGameStatsResponse(
        int TotalWins,
        int GamesPlayed,
        double WinRate,
        int HighestScore,
        List<TopWordEntry> TopWords
    );
}