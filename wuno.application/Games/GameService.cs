using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.domain.Rules;
using wuno.infrastructure;

namespace Wuno.Application.Games
{
    public sealed class GameService : IGameService
    {
        private readonly AppDbContext _db;
        public GameService(AppDbContext db) { _db = db; }

        public async Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct)
        {
            var n = Math.Clamp(req.PlayerCount, Constants.MIN_PLAYERS, Constants.MAX_PLAYERS);
            var game = new Game { TargetWins = Math.Clamp(req.TargetWins, Constants.MIN_TARGET_WINS, Constants.MAX_TARGET_WINS), 
                                  NextSeat = 1, Status = GameStatus.ACTIVE };
            for (int i = 1; i <= n; i++) game.Players.Add(new Player { Seat = i, GameId = game.Id });

            var round0 = new Round { GameId = game.Id, Index = 0, Active = true };
            game.Rounds.Add(round0);

            var turn0 = new Turn
            {
                GameId = game.Id,
                RoundId = round0.Id,
                Index = 0,
                Seat = 1,
                StartLetter = null,
                FreeStart = true,
                MinLen = 1,
                Require2Vowels = false,
                DurationSec = Constants.DEFAULT_TURN_DUR_SEC,
                StartedAt = DateTime.UtcNow
            };
            game.Turns.Add(turn0);

            _db.Games.Add(game);
            await _db.SaveChangesAsync(ct);

            return new NewGameResponse(game.Id, turn0.Id, 1, n, game.TargetWins);
        }

        public async Task<object?> GetGameStateAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
              .Include(g => g.Players.OrderBy(p => p.Seat))
              .Include(g => g.Rounds.OrderByDescending(r => r.Index))
              .Include(g => g.Turns.OrderByDescending(t => t.Index))
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) return null;
            var round = game.Rounds.FirstOrDefault();
            var turn = game.Turns.FirstOrDefault();
            return new
            {
                game = new { game.Id, game.Status, game.TargetWins, game.NextSeat, playerCount = game.Players.Count },
                players = game.Players.Select(p => new { p.Seat, p.LastWordLength }),
                round,
                turn
            };
        }

        public async Task<SubmitWordResponse> SubmitWordAsync(Guid gameId, SubmitWordRequest req, CancellationToken ct)
        {
            var game = await _db.Games
              .Include(g => g.Players)
              .Include(g => g.Rounds.OrderByDescending(r => r.Index))
              .Include(g => g.Turns.OrderByDescending(t => t.Index))
              .Include(g => g.Effects)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);

            if (game is null) return new(false, "Not found");
            if (game.Status != GameStatus.ACTIVE) return new(false, "Game over");

            var turn = game.Turns.First(); var round = game.Rounds.First();
            if (game.NextSeat != 0 && game.NextSeat != req.Seat) return new(false, "Not your turn");

            // server time check
            var elapsed = (DateTime.UtcNow - turn.StartedAt).TotalSeconds;
            if (elapsed >= turn.DurationSec)
            {
                turn.EndedAt = DateTime.UtcNow; turn.EndReason = TurnEndReason.TIMEOUT;
                await StartNextTurnAsync(_db, game, round, prevAcceptedLetter: turn.Word?.LastOrDefault(), ct);
                await _db.SaveChangesAsync(ct);
                return new(false, "Timeout");
            }

            // Validate word vs snapshot
            var w = req.Word ?? "";
            if (!Words.IsWord(w)) return new(false, "Not a valid word");
            var mustStart = !turn.FreeStart && turn.StartLetter.HasValue ? turn.StartLetter : null;
            if (mustStart is not null && Words.First(w) != mustStart) return new(false, $"Must start with '{mustStart}'");
            if (w.Length < turn.MinLen) return new(false, $"Must be at least {turn.MinLen} letters");
            if (turn.Require2Vowels && Words.VowelCount(w) < 2) return new(false, "Must contain ≥2 vowels");

            // Accept word and advance — atomic in one SaveChanges (single DB)
            var me = game.Players.Single(p => p.Seat == req.Seat);
            var opp = game.Players.Single(p => p.Seat == PrevSeat(game.Players.Count, req.Seat, game.Direction));

            turn.Word = w; turn.WordLen = w.Length; turn.EndedAt = DateTime.UtcNow; turn.EndReason = TurnEndReason.END;
            me.LastWord = w; me.LastWordLength = w.Length;

            // Queue effects to SELF / NEXT
            var specials = EffectsLogic.SpecialsFromWord(w, opp.LastWord);
            var upcomingSeat = NextSeat(game.Players.Count, req.Seat, game.Direction);
            var myTurns = game.Turns.Count(t => t.Seat == me.Seat);
            var nextTurns = game.Turns.Count(t => t.Seat == upcomingSeat);
            var nextPlayer = game.Players.Single(p => p.Seat == upcomingSeat);
            var oppTurns = game.Turns.Count(t => t.Seat == opp.Seat);

            foreach (var (type, val, target) in specials)
            {
                Guid recipient;
                int appliesOn;
                switch (target)
                {
                    case EffectTarget.PREV: recipient = opp.Id; appliesOn = oppTurns + 1; break;
                    case EffectTarget.SELF: recipient = me.Id; appliesOn = myTurns + 1; break;
                    case EffectTarget.NEXT: recipient = nextPlayer.Id; appliesOn = nextTurns + 1; break;
                    default: throw new Exception("Unhandled effect target");
                }
                game.Effects.Add(new Effect { GameId = game.Id, PlayerId = recipient, Type = type, Value = val, AppliesOn = appliesOn });
            }

            // Start next turn snapshot
            char? nextLetter = EffectsLogic.NextStartLetterFrom(w);
            await StartNextTurnAsync(_db, game, round, nextLetter, ct);

            await _db.SaveChangesAsync(ct);
            return new(true, null);
        }

        static int NextSeat(int n, int seat, int dir) => ((seat - 1 + dir + n) % n) + 1;
        static int PrevSeat(int n, int seat, int dir) => ((seat - 1 - dir + n) % n) + 1;

        static async Task StartNextTurnAsync(AppDbContext db, Game game, Round round, char? prevAcceptedLetter, CancellationToken ct)
        {
            var prev = game.Turns.First(); // latest
            var seat = NextSeat(game.Players.Count, prev.Seat, game.Direction);
            var player = game.Players.Single(p => p.Seat == seat);

            // compute personal turn index for seat
            var personalIndex = game.Turns.Count(t => t.Seat == seat) + 1;
            var effects = game.Effects.Where(e => e.PlayerId == player.Id && e.AppliesOn == personalIndex)
                                      .Select(e => (e.Type, e.Value));

            var baseC = Constraints.Base(prevAcceptedLetter ?? prev.StartLetter);
            var applied = EffectsLogic.Apply(baseC, effects);

            var minLen = Math.Max(applied.MinLen, player.LastWordLength);

            var newTurn = new Turn
            {
                GameId = game.Id,
                RoundId = round.Id,
                Index = game.Turns.Count,
                Seat = seat,
                StartLetter = applied.FreeStart ? null : applied.StartLetter,
                FreeStart = applied.FreeStart,
                MinLen = minLen,
                Require2Vowels = applied.Require2Vowels,
                DurationSec = applied.DurationSec,
                StartedAt = DateTime.UtcNow
            };
            game.Turns.Insert(0, newTurn); // we keep latest at [0] in memory; EF will save anyway
            game.NextSeat = seat;
            await Task.CompletedTask;
        }
    }
}
