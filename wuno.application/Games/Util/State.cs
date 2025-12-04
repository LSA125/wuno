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
        public static TurnState TurnToState(Turn turn, List<EffectState> effects)
        {
            return new TurnState(
                turn.Id,
                turn.Index,
                turn.Seat,
                turn.StartedAt,
                turn.DueAt,
                turn.MinLen,
                turn.FreeStart,
                effects
            );
        }
        public static RoundState RoundToState(Round round)
        {
            return new RoundState(
                round.Id,
                round.Index,
                round.WinnerId,
                round.StartedAt,
                round.EndedAt
            );
        }
        public static PlayerState PlayerToState(Player player)
        {
            return new PlayerState(
                player.Id,
                player.Seat,
                player.IsActive,
                player.IsConnected,
                player.Name,
                player.IconUrl,
                player.RoundWins,
                player.LastWord
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
                players,
                currentRound,
                currentTurn
            );
        }
    }
}
