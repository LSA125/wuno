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
        private readonly IWordList _wl;
        public GameService(AppDbContext db, IWordList wl) { _db = db; _wl = wl; }

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
            return game.Players.Where(p => p.IsConnected).All(p => p.IsActive);
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
            TurnState newTurn = await StartRoundAsync(gameId, ct);
            game.Status = GameStatus.ACTIVE;
            await _db.SaveChangesAsync(ct);
            return newTurn;
        }
        public async Task<bool> IsMatchEndAsync(Guid gameId, CancellationToken ct)
        {
            return await _db.Games.AnyAsync(g => g.Id == gameId && g.Players.Any(p => p.RoundWins >= g.TargetWins), ct);
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
            //select all active players
            List<Player> activePlayers = round.Game.Players.Where(p => p.IsActive).ToList();
            round.Active = false;
            round.EndedAt = DateTime.UtcNow;
            round.WinnerId = activePlayers.Single().Id;
            var winner = round.Game.Players.Single(p => p.Id == round.WinnerId);
            winner.RoundWins += 1;
            await _db.SaveChangesAsync(ct);
        }
        public async Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct)
        {
            return await _db.Players
              .Where(p => p.GameId == gameId)
              .OrderBy(p => p.Seat)
              .AsNoTracking()
              .Select(p => new PlayerState(p.Id, p.Seat, p.IsActive, p.IsConnected, p.IsHost, p.Name, p.IconUrl, p.RoundWins, p.LastWord))
              .ToListAsync(ct);
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
                                    turn.DurationSec, turn.DueAt, turn.MinLen, turn.FreeStart, turn.Require2Vowels);
        }
        public async Task<(Guid turnId, int seat, DateTime dueAt)> GetCurrentTurnInfoAsync(Guid gameId, CancellationToken ct)
        {
            var dto = await _db.Turns
                .AsNoTracking()
                .Where(t => t.GameId == gameId && t.EndedAt == null)
                .OrderByDescending(t => t.Index)
                .Select(t => new { t.Id, t.Seat, t.DueAt }) // SQL-friendly
                .FirstOrDefaultAsync(ct) ?? throw new Exception("No active turn found");

            return new(dto.Id, dto.Seat, dto.DueAt);
        }
        public async Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct)
        {
            return await _db.Games.Where(g => g.Id == gameId)
              .Select(g => new GameState(
                  g.Id,
                  g.Status,
                  g.NextSeat,
                  g.Direction,
                  g.TargetWins,
                  g.Players.OrderBy(p => p.Seat).Select(p => new PlayerState(p.Id, p.Seat, p.IsActive, p.IsConnected, p.IsHost, p.Name, p.IconUrl, p.RoundWins, p.LastWord)).ToList(),
                  g.Rounds.OrderByDescending(r => r.Index).Select(r => new RoundState(r.Id, r.Index, r.Active, r.WinnerId, r.StartedAt, r.EndedAt)).FirstOrDefault()!,
                  g.Turns.Where(t => t.EndedAt == null).OrderByDescending(t => t.Index).Select(t => new TurnState(t.Id, t.Index, t.Seat, t.StartedAt, t.DurationSec, t.DueAt, t.MinLen, t.FreeStart, t.Require2Vowels)).FirstOrDefault()!
              ))
              .AsNoTracking()
              .FirstOrDefaultAsync(ct) ?? throw new Exception("Game not found");
        }

        public async Task<SubmitWordResponse> SubmitWordAsync(Guid gameId, Guid roundId, Guid turnId, SubmitWordRequest req, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // Load minimal state first
            var turnDto = await _db.Turns
                .Where(t => t.Id == turnId && t.GameId == gameId)
                .Select(t => new { t.Id, t.GameId, t.RoundId, t.Seat, t.StartedAt, t.DurationSec, t.EndedAt })
                .FirstOrDefaultAsync(ct);

            if (turnDto is null) return new(false, "Not found");
            if (turnDto.RoundId != roundId) return new(false, "Mismatched round/turn");
            if (turnDto.Seat != req.Seat) return new(false, "Not your turn");

            // timeout check first (no race yet)
            if ((now - turnDto.StartedAt).TotalSeconds >= turnDto.DurationSec)
            {
                var timedOut = await _db.Turns
                    .Where(t => t.Id == turnId && t.EndedAt == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.EndedAt, _ => now)
                        .SetProperty(t => t.EndReason, _ => TurnEndReason.TIMEOUT), ct);

                if (timedOut == 0) return new(false, "Turn already processed");

                // advance in a transaction
                using var tx = await _db.Database.BeginTransactionAsync(ct);
                var game = await _db.Games
                    .AsTracking()
                    .Include(g => g.Players)
                    .Include(g => g.Rounds.OrderByDescending(r => r.Index))
                    .Include(g => g.Turns.OrderByDescending(t => t.Index))
                    .Include(g => g.Effects)
                    .FirstAsync(g => g.Id == gameId, ct);

                var round = game.Rounds.First();
                var endedTurn = game.Turns.First(t => t.Id == turnId);
                StartTurn(game, round, prevAcceptedLetter: endedTurn.Word?.LastOrDefault(), ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return new(false, "Timeout");
            }

            // validate request word (pure function calls)
            var w = req.Word ?? "";
            if (!_wl.IsWord(w)) return new(false, "Not a valid word");
            // ... other validations ...

            // atomically accept the word (gate)
            var accepted = await _db.Turns
                .Where(t => t.Id == turnId && t.EndedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Word, _ => w)
                    .SetProperty(t => t.WordLen, _ => w.Length)
                    .SetProperty(t => t.EndedAt, _ => now)
                    .SetProperty(t => t.EndReason, _ => TurnEndReason.END), ct);

            if (accepted == 0) return new(false, "Turn already processed");

            // advance in a transaction
            using (var tx = await _db.Database.BeginTransactionAsync(ct))
            {
                var game = await _db.Games
                    .AsTracking()
                    .Include(g => g.Players)
                    .Include(g => g.Rounds.OrderByDescending(r => r.Index))
                    .Include(g => g.Turns.OrderByDescending(t => t.Index))
                    .Include(g => g.Effects)
                    .FirstAsync(g => g.Id == gameId, ct);

                var round = game.Rounds.First();
                var me = game.Players.Single(p => p.Seat == req.Seat);
                me.LastWord = w;

                // compute effects & create next turn deterministically
                // …
                var nextLetter = EffectsLogic.NextStartLetterFrom(w);
                StartTurn(game, round, nextLetter, ct);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            return new(true, null);
        }

        static int NextSeat(int n, int seat, int dir) => ((seat - 1 + dir + n) % n) + 1;
        static int PrevSeat(int n, int seat, int dir) => ((seat - 1 - dir + n) % n) + 1;
        public TurnState StartTurn(Game game, Round round, char? prevAcceptedLetter, CancellationToken ct)
        {
            var prev = game.Turns.OrderByDescending(t => t.Index).FirstOrDefault(); // latest
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
                    StartedAt = DateTime.UtcNow,
                    DueAt = DateTime.UtcNow.AddSeconds(Constants.DEFAULT_TURN_DUR_SEC)
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
                    StartedAt = DateTime.UtcNow,
                    DueAt = DateTime.UtcNow.AddSeconds(applied.DurationSec)
                };
                game.Turns.Add(newTurn); // we keep latest at [0] in memory; EF will save anyway
                game.NextSeat = seat;
            }
            Turn turn1 = game.Turns.OrderByDescending(t => t.Index).FirstOrDefault() ?? throw new Exception("Failed to create new turn");
            return new TurnState(turn1.Id, turn1.Index, turn1.Seat, turn1.StartedAt,
                                    turn1.DurationSec, turn1.DueAt, turn1.MinLen, turn1.FreeStart, turn1.Require2Vowels);
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
                GameState? gameState = await GetGameStateAsync(gameId, ct);
                if (gameState is null) throw new Exception("Failed to get game state after joining");
                return new JoinGameResponse(gameId, gameState);
            }
            else if (game.Status != GameStatus.WAITING) throw new Exception("Game not joinable");
            var inactive = game.Players.FirstOrDefault(p => !p.IsConnected);
            if (inactive is null) throw new Exception("Game full");
            inactive.IsActive = true;
            inactive.IsConnected = true;
            inactive.Name = user.Name;
            inactive.IconUrl = user.IconUrl;
            await _db.SaveChangesAsync(ct);
            GameState? state = await GetGameStateAsync(gameId, ct) as GameState;
            if (state is null) throw new Exception("Failed to get game state after joining");
            return new JoinGameResponse(inactive.Id, state);
        }
        async public Task<(Guid gameId, List<PlayerState> players)> DisconnectProtocolAsync(Guid playerId, CancellationToken ct)
        {
            //mark player as disconnected
            Player? player = await _db.FindAsync<Player>(playerId);
            if (player is null) throw new Exception("Player not found when disconnecting");
            player.IsConnected = false;
            await _db.SaveChangesAsync(ct);
            return (player.GameId, await GetPlayersAsync(player.GameId, ct));
        }
        async public Task LeaveGameAsync(Guid gameId, Guid playerId, CancellationToken ct)
        {
            Player player = _db.Find<Player>(playerId) ?? throw new Exception("Player not found when leaving game");
            Game game = _db.Find<Game>(gameId) ?? throw new Exception("Game not found when leaving game");
            game.Players.Remove(player);

            await _db.SaveChangesAsync(ct);
        }
        //Timeout and advance turn
        public async Task<bool> TimeoutAndAdvanceAsync(Guid gameId, Guid turnId, CancellationToken ct)
        {
            var affected = await _db.Turns
                .Where(t => t.Id == turnId && t.EndedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.EndedAt, _ => DateTime.UtcNow)
                    .SetProperty(t => t.EndReason, _ => TurnEndReason.TIMEOUT), ct);

            if (affected == 0) return false;

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            var game = await _db.Games.AsTracking()
                .Include(g => g.Players)
                .Include(g => g.Rounds.OrderByDescending(r => r.Index))
                .Include(g => g.Turns.OrderByDescending(t => t.Index))
                .Include(g => g.Effects)
                .FirstAsync(g => g.Id == gameId, ct);

            var round = game.Rounds.First();
            var turn = game.Turns.First(t => t.Id == turnId);

            var player = game.Players.Single(p => p.Seat == turn.Seat);
            player.IsActive = false;

            StartTurn(game, round, prevAcceptedLetter: turn.Word?.LastOrDefault(), ct);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }

    }
}
