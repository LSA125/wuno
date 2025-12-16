using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.domain.Rules;

namespace Wuno.Application.Games.Util
{
    public static class State
    {
        private static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        private static DateTime? EnsureUtc(DateTime? value) => value is null ? null : EnsureUtc(value.Value);

        public static TurnState TurnToState(Turn turn)
        {
            return new TurnState(
                turn.Id,
                turn.Index,
                turn.Seat,
                EnsureUtc(turn.StartedAt),
                EnsureUtc(turn.DueAt),
                turn.MinLen,
                turn.Score
            );
        }
        public static RoundState RoundToState(Round round)
        {
            return new RoundState(
                round.Id,
                round.Index,
                round.WinnerId,
                EnsureUtc(round.StartedAt),
                EnsureUtc(round.EndedAt)
            );
        }
        public static PlayerState PlayerToState(Player player)
        {
            // Cap remaining time by the max time for this player's upcoming turn
            int maxTime = EffectsLogic.CalculateMaxTime(player.TurnsPlayedThisRound, isFirstTurn: false);
            double cappedRemainingTime = Math.Min(player.RemainingTime, maxTime);
            
            return new PlayerState(
                player.Id,
                player.UserId,
                player.Seat,
                player.IsActive,
                player.IsConnected,
                player.Name,
                player.IconUrl,
                player.RoundWins,
                player.LastWord,
                cappedRemainingTime
            );
        }
        public static GameState GameToState(Game game,
            List<PlayerState> players,
            RoundState currentRound,
            TurnState currentTurn)
        {
            return new GameState(
                game.Id,
                game.Status,
                game.CurSeat,
                game.Direction,
                game.TargetWins,
                game.LastWord,
                players,
                currentRound,
                currentTurn
            );
        }
    }
}
