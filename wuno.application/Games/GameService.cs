using Azure.Identity;
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

        public async Task<Guid> GetGameId(string code, CancellationToken ct)
        {
            var game = await _db.Games
              .AsNoTracking()
              .FirstOrDefaultAsync(g => g.Code == code, ct);
            if (game is null) throw new Exception("Game not found");
            return game.Id;
        }
        public async Task ReadyAsync(Guid gameId, int seat, bool isReady, CancellationToken ct)
        {
            var game = await _db.Games
              .Include(g => g.Players)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) throw new Exception("Game not found");
            if(game.Status != GameStatus.WAITING) throw new Exception("Game already started");
            var player = game.Players.SingleOrDefault(p => p.Seat == seat);
            if (player is null) throw new Exception("Player not found");
            player.IsActive = isReady;
            await _db.SaveChangesAsync(ct);
        }
        public async Task<bool> AreAllPlayersReadyAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
              .Include(g => g.Players)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) throw new Exception("Game not found");
            return game.Players.All(p => p.IsActive);
        }
        public async Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct)
        {
            var n = Math.Clamp(req.PlayerCount, Constants.MIN_PLAYERS, Constants.MAX_PLAYERS);
            var game = new Game
            {
                TargetWins = Math.Clamp(req.TargetWins, Constants.MIN_TARGET_WINS, Constants.MAX_TARGET_WINS),
                NextSeat = 1,
                Status = GameStatus.WAITING
            };
            for (int i = 1; i <= n; i++) game.Players.Add(new Player { Seat = i, GameId = game.Id });

            _db.Games.Add(game);
            await _db.SaveChangesAsync(ct);

            return new NewGameResponse(game.Id, 1, n, game.TargetWins);
        }
        public async Task<TurnState> StartMatchAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
              .Include(g => g.Players)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) throw new Exception("Game not found");
            if (game.Status != GameStatus.WAITING) throw new Exception("Game already started");
            if (game.Players.Count(p => p.IsActive) < 2) throw new Exception("Not enough players ready");
            game.Status = GameStatus.ACTIVE;
            TurnState newTurn = StartRoundAsync(gameId, ct).Result;
            await _db.SaveChangesAsync(ct);
            return newTurn;
        }
        public async Task<bool> IsMatchEndAsync(Guid gameId, Guid playerId, CancellationToken ct)
        {
            return await _db.Games
              .Include(g => g.Players)
              .AnyAsync(g => g.Id == gameId && g.Players.Any(p => p.Id == playerId && p.RoundWins >= g.TargetWins), ct);
        }
        public async Task EndMatchAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) throw new Exception("Game not found");
            game.Status = GameStatus.FINISHED;
            await _db.SaveChangesAsync(ct);
        }
        public async Task<TurnState> StartRoundAsync(Guid gameId, CancellationToken ct)
        {
            Game game = await _db.Games
              .Include(g => g.Players)
              .Include(g => g.Rounds)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct)
              ?? throw new Exception("Game not found");

            // reset players
            foreach (var p in game.Players)
            {
                p.IsActive = true;
                p.LastWord = null;
            }
            // new round
            var round = new Round
            {
                GameId = game.Id,
                Index = game.Rounds.Count,
                Active = true,
                StartedAt = DateTime.UtcNow
            };
            game.Rounds.Add(round);
            // first turn
            return StartTurn(game, round, prevAcceptedLetter: null, ct);
        }
        public async Task<bool> IsRoundEndAsync(Guid gameId, CancellationToken ct)
        {
            return await _db.Games
                .Include(g => g.Players)
                .AnyAsync(g => g.Id == gameId && g.Players.Count(p => p.IsActive) <= 1, ct);
        }
        public async Task EndRoundAsync(Guid gameId, Guid roundId, CancellationToken ct)
        {
            Round round = await _db.Rounds
              .Include(r => r.Game)
              .ThenInclude(g => g.Players)
              .FirstOrDefaultAsync(r => r.Id == roundId && r.GameId == gameId, ct)
              ?? throw new Exception("Round not found");
            round.Active = false;
            round.EndedAt = DateTime.UtcNow;
            round.WinnerId = winnerId;
            var winner = round.Game.Players.Single(p => p.Id == winnerId);
            winner.RoundWins += 1;
            await _db.SaveChangesAsync(ct);
        }
        public async Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
              .Include(g => g.Players)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) throw new Exception("Game not found");
            return game.Players
                .OrderBy(p => p.Seat)
                .Select(p => new PlayerState(p.Id, p.Seat, p.IsActive, p.IsConnected, p.IsHost, p.Name, p.IconUrl, p.RoundWins, p.LastWord))
                .ToList();
        }
        public async Task<RoundState> GetRoundAsync(Guid roundId, CancellationToken ct)
        {
            var round = await _db.Rounds
              .AsNoTracking()
              .FirstOrDefaultAsync(r => r.Id == roundId, ct);
            if (round is null) throw new Exception("Round not found");
            return new RoundState(round.Id, round.Index, round.Active, round.WinnerId, round.StartedAt, round.EndedAt);
        }
        public async Task<TurnState> GetTurnAsync(Guid turnId, CancellationToken ct)
        {
            var turn = await _db.Turns
              .AsNoTracking()
              .FirstOrDefaultAsync(t => t.Id == turnId, ct);
            if (turn is null) throw new Exception("Turn not found");
            return new TurnState(turn.Id, turn.Index, turn.Seat, turn.StartedAt, 
                                    turn.DurationSec, turn.MinLen, turn.FreeStart, turn.Require2Vowels);
        }
        public async Task<(Guid turnId, int seat, DateTime dueAt)?> GetCurrentTurnInfoAsync(Guid gameId, CancellationToken ct)
        {
            var dto = await _db.Turns
                .AsNoTracking()
                .Where(t => t.GameId == gameId && t.EndedAt == null)
                .OrderByDescending(t => t.Index)
                .Select(t => new { t.Id, t.Seat, t.StartedAt, t.DurationSec }) // SQL-friendly
                .FirstOrDefaultAsync(ct);

            return dto is null
                ? ((Guid, int, DateTime)?)null
                : (dto.Id, dto.Seat, dto.StartedAt.AddSeconds(dto.DurationSec));
        }
        public async Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
              .Include(g => g.Players.OrderBy(p => p.Seat))
              .Include(g => g.Rounds.OrderByDescending(r => r.Index))
              .Include(g => g.Turns.OrderByDescending(t => t.Index))
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) throw new Exception("Game not found");
            var round = game.Rounds.FirstOrDefault();
            var turn = game.Turns.FirstOrDefault();
            if (round is null || turn is null) throw new Exception("Game has no active round/turn");
            return new GameState(
                game.Id, game.Status, game.TargetWins, game.Direction, game.NextSeat,
                GetPlayersAsync(gameId, ct).Result,
                GetRoundAsync(round.Id,ct).Result,
                GetTurnAsync(turn.Id, ct).Result
            );
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
                StartTurn(game, round, prevAcceptedLetter: turn.Word?.LastOrDefault(), ct);
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
            me.LastWord = w;

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
            StartTurn(game, round, nextLetter, ct);

            await _db.SaveChangesAsync(ct);
            return new SubmitWordResponse(true, null);
        }
        static int NextSeat(int n, int seat, int dir) => ((seat - 1 + dir + n) % n) + 1;
        static int PrevSeat(int n, int seat, int dir) => ((seat - 1 - dir + n) % n) + 1;
        public TurnState StartTurn(Game game, Round round, char? prevAcceptedLetter, CancellationToken ct)
        {
            var prev = game.Turns.First(); // latest
            // first turn case
            if (prev is null)
            {
                var turn = new Turn
                {
                    GameId = game.Id,
                    RoundId = round.Id,
                    Index = 0,
                    Seat = game.NextSeat,
                    StartLetter = null,
                    FreeStart = true,
                    MinLen = Constants.DEFAULT_START_LEN,
                    Require2Vowels = false,
                    DurationSec = Constants.DEFAULT_TURN_DUR_SEC,
                    StartedAt = DateTime.UtcNow
                };
                game.Turns.Add(turn);
            }
            else
            {
                var seat = NextSeat(game.Players.Count, prev.Seat, game.Direction);
                var player = game.Players.Single(p => p.Seat == seat);

                // compute personal turn index for seat
                var personalIndex = game.Turns.Count(t => t.Seat == seat) + 1;
                var effects = game.Effects.Where(e => e.PlayerId == player.Id && e.AppliesOn == personalIndex)
                                          .Select(e => new EffectState(e.Type, e.Value)).ToList();
                var prevWord = player.LastWord;

                var baseC = Constraints.Base(prevAcceptedLetter ?? prev.StartLetter, personalIndex, prevWord);
                var applied = EffectsLogic.Apply(baseC, effects);

                var minLen = Math.Max(applied.MinLen, player.LastWord?.Length ?? 0);

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
            }
            Turn turn1 = game.Turns.First();
            return new TurnState(turn1.Id, turn1.Index, turn1.Seat, turn1.StartedAt,
                                    turn1.DurationSec, turn1.MinLen, turn1.FreeStart, turn1.Require2Vowels);
            
        }
        async public Task<JoinGameResponse> JoinGameAsync(Guid gameId, Guid userId, CancellationToken ct)
        {
            var game = await _db.Games
              .Include(g => g.Players)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) throw new Exception("Game not found");
            //check for reconnect
            User? user = _db.Users.Include(u => u.ActivePlayer).FirstOrDefault(u => u.Id == userId);
            if(user is null)
            {
                throw new Exception("User does not exist when joining game");
            }
            Player? player = user.ActivePlayer;
            //if player is already in a game
            if (player is not null && player.GameId == gameId)
            {
                player.IsConnected = true;
                await _db.SaveChangesAsync(ct);
                GameState? gameState = await GetGameStateAsync(gameId, ct) as GameState;
                if (gameState is null) throw new Exception("Failed to get game state after joining");
                return new JoinGameResponse(gameId, gameState);
            }
            else if (game.Status != GameStatus.WAITING) throw new Exception("Game not joinable");
            var inactive = game.Players.FirstOrDefault(p => !p.IsActive);
            if (inactive is null) throw new Exception("Game full");
            inactive.IsActive = true;
            inactive.Name = user.Name;
            inactive.IconUrl = user.IconUrl;
            await _db.SaveChangesAsync(ct);
            GameState? state = await GetGameStateAsync(gameId, ct) as GameState;
            if (state is null) throw new Exception("Failed to get game state after joining");
            return new JoinGameResponse(gameId, state);
        }
        async public Task DisconnectProtocolAsync(Guid gameId, Guid playerId, CancellationToken ct)
        {
            //mark player as disconnected
            var player = _db.Find<Player>(playerId);
            if (player is null) throw new Exception("Player not found when disconnecting");
            player.IsConnected = false;
            await _db.SaveChangesAsync(ct);
        }
        async public Task LeaveGameAsync(Guid gameId, Guid playerId, CancellationToken ct)
        {
            Player player = _db.Find<Player>(playerId) ?? throw new Exception("Player not found when leaving game");
            Game game = _db.Find<Game>(gameId) ?? throw new Exception("Game not found when leaving game");
            game.Players.Remove(player);

            await _db.SaveChangesAsync(ct);
        }
        //Timeout and advance turn
        async public Task<bool> TimeoutAndAdvanceAsync(Guid gameId, Guid turnId, CancellationToken ct)
        {
            // in IGameService.TimeoutAndAdvanceAsync
            var affected = await _db.Turns
                .Where(t => t.Id == turnId && t.EndedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.EndedAt, _ => DateTime.UtcNow)
                    .SetProperty(t => t.EndReason, _ => TurnEndReason.TIMEOUT), ct);

            if (affected == 0) return false; // already processed elsewhere

            // now load fresh state and advance
            var game = await _db.Games
                .AsTracking()
                .Include(g => g.Players)
                .Include(g => g.Rounds.OrderByDescending(r => r.Index))
                .Include(g => g.Turns.OrderByDescending(t => t.Index))
                .Include(g => g.Effects)
                .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) return false;

            var round = game.Rounds.FirstOrDefault();
            var turn = game.Turns.FirstOrDefault(t => t.Id == turnId);
            if (round is null || turn is null) return false;

            // set player as inactive, as player has lost the round.
            var player = game.Players.Single(p => p.Seat == turn.Seat).IsActive = false;
            StartTurn(game, round, prevAcceptedLetter: turn.Word?.LastOrDefault(), ct);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
