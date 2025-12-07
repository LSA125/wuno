using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.domain.Rules;
using wuno.infrastructure;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Games.Util;
using Wuno.Domain.Rules;

namespace Wuno.Application.Games.Implementation
{
    public sealed class GameService(AppDbContext db, IWordList wl, ICodeGeneratorService cg, ITurnTimer tt) : IGameService
    {
        private readonly AppDbContext _db = db;
        private readonly IWordList _wl = wl;
        private readonly ICodeGeneratorService _cg = cg;
        private readonly ITurnTimer _tt = tt;

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
            if (game.Status != GameStatus.WAITING) throw new Exception("Game already started");
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
            return game.Players.All(p => p.IsActive || !p.IsTaken) && game.Players.Count(p => p.IsActive) >= 2;
        }
        private void ResetPlayers(List<Player> players)
        {
            foreach (var p in players)
            {
                p.IsActive = p.IsTaken;
                p.LastWord = null;
                p.TurnsPlayedThisRound = 0;
            }
        }
        public async Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct)
        {
            var n = Math.Clamp(req.PlayerCount, Constants.MIN_PLAYERS, Constants.MAX_PLAYERS);
            var game = new Game
            {
                TargetWins = Math.Clamp(req.TargetWins, Constants.MIN_TARGET_WINS, Constants.MAX_TARGET_WINS),
                CurSeat = 1,
                Status = GameStatus.WAITING,
            };
            // try generate unique code
            string code;
            int attempts = 0;
            do
            {
                code = _cg.GenerateCode();
                attempts++;
            } while (await _db.Games.AnyAsync(g => g.Code == code && g.Status != GameStatus.FINISHED, ct) && attempts < 100);
            if (attempts >= 100) throw new Exception("Failed to generate unique game code");
            game.Code = code;

            for (int i = 1; i <= n; i++) game.Players.Add(new Player { Seat = i, GameId = game.Id, IsActive = false, IsConnected = false, IsTaken = false });

            _db.Games.Add(game);
            await _db.SaveChangesAsync(ct);

            return new NewGameResponse(game.Code, n, game.TargetWins);
        }
        public async Task<bool> MarkMatchAsStartedAsync(Guid gameId, CancellationToken ct)
        {
            var affected = await _db.Games
                .Where(g => g.Id == gameId && g.Status == GameStatus.WAITING)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(g => g.Status, _ => GameStatus.ACTIVE), ct);
            return affected > 0;
        }
        public async Task<TurnState> StartMatchAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
                .Include(g => g.Players)
                .FirstAsync(g => g.Id == gameId, ct);

            ResetPlayers(game.Players);
            game.Status = GameStatus.ACTIVE;

            int roundIndex = await _db.Rounds
                .CountAsync(r => r.GameId == gameId, ct);

            var round = new Round
            {
                GameId = game.Id,
                Index = roundIndex,
                StartedAt = DateTime.UtcNow
            };

            _db.Rounds.Add(round);
            game.CurrentRound = round;

            var firstPlayer = FindNextValidPlayer(game.Players, game.CurSeat)
                ?? throw new Exception("No valid player on first round");

            TurnState firstTurn = await CreateTurnAsync(game, round, firstPlayer, DateTime.UtcNow, ct);

            await _db.SaveChangesAsync(ct);
            return firstTurn;
        }
        private TurnState BuildTurnState(Game game, Turn turn, Player player, int personalIndex)
        {
            var effects = game.Effects
                .Where(e => e.RoundId == turn.RoundId && e.TargetSeat == turn.Seat && e.AppliesOnTurn == personalIndex)
                .Select(e => new EffectState(e.Type, e.Value))
                .ToList();

            return State.TurnToState(turn, effects);
        }
        private async Task<TurnState> CreateTurnAsync(Game game, Round round, Player nextPlayer, DateTime now, CancellationToken ct)
        {
            var previous = game.CurrentTurn;
            Turn newTurn;
            List<EffectState> effects = [];

            if (previous is null)
            {
                newTurn = new Turn
                {
                    GameId = game.Id,
                    RoundId = round.Id,
                    Index = 0,
                    Seat = nextPlayer.Seat,
                    FreeStart = true,
                    MinLen = Constants.DEFAULT_START_LEN,
                    StartedAt = now,
                    DueAt = now.AddSeconds(Constants.DEFAULT_TURN_DUR_SEC)
                };
            }
            else
            {
                var personalIndex = nextPlayer.TurnsPlayedThisRound;
                effects = await _db.Effects
                    .AsNoTracking()
                    .Where(e => e.GameId == game.Id &&
                                e.RoundId == round.Id &&
                                e.TargetSeat == nextPlayer.Seat &&
                                e.AppliesOnTurn == personalIndex)
                    .Select(e => new EffectState(e.Type, e.Value))
                    .ToListAsync(ct);

                var baseC = Constraints.Base(previous.Word?.LastOrDefault(), personalIndex, nextPlayer.LastWord);
                Constraints applied = EffectsLogic.Apply(baseC, effects);

                newTurn = new Turn
                {
                    GameId = game.Id,
                    RoundId = round.Id,
                    Index = game.Turns.Count,
                    Seat = nextPlayer.Seat,
                    FreeStart = applied.FreeStart,
                    MinLen = applied.MinLen,
                    StartedAt = now,
                    DueAt = now.AddSeconds(applied.DurationSec)
                };
            }

            _db.Turns.Add(newTurn);
            game.CurSeat = nextPlayer.Seat;
            game.CurrentTurn = newTurn;

            return State.TurnToState(newTurn, effects);
        }
        public async Task<ProcessTurnOutcome> ProcessTurnAsync(Guid gameId, Guid roundId, Guid turnId, Guid playerId, int seat, string word, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            await using IDbContextTransaction tx = await _db.Database.BeginTransactionAsync(ct);

            Game? game = await _db.Games
                .Include(g => g.Players)
                .Include(g => g.CurrentRound)
                .Include(g => g.CurrentTurn)
                .FirstOrDefaultAsync(g => g.Id == gameId, ct);

            if (game is null || game.CurrentTurn is null || game.CurrentRound is null || game.CurrentTurn.Id != turnId || game.CurrentRound.Id != roundId)
            {
                return new(false, "Not found", null);
            }

            Turn currentTurn = game.CurrentTurn;
            if (currentTurn.EndedAt != null)
            {
                return new(false, "Turn already processed", null);
            }

            Player? player = game.Players.SingleOrDefault(p => p.Id == playerId);
            if (player is null || player.Seat != seat || currentTurn.Seat != seat)
            {
                return new(false, "Not your turn", null);
            }

            int personalIndex = player.TurnsPlayedThisRound;
            bool timedOut = now > currentTurn.DueAt;

            if (timedOut)
            {
                currentTurn.EndedAt = now;
                currentTurn.EndReason = TurnEndReason.TIMEOUT;
                player.IsActive = false;
                player.TurnsPlayedThisRound += 1;
            }
            else
            {
                var w = word;
                if (!_wl.IsWord(w)) return new(false, "Not a valid word", null);
                if (w.Length < currentTurn.MinLen) return new(false, $"Word too short (min {currentTurn.MinLen})", null);
                if (!currentTurn.FreeStart
                    && !game.LastWord.IsNullOrEmpty()
                    && w.First() == game.LastWord!.Last())
                {
                    return new(false, $"Word must start with '{game.LastWord!.Last()}'", null);
                }

                bool playedThisRound = await _db.Turns.AnyAsync(t => t.GameId == gameId && t.RoundId == roundId && t.Word == w, ct);
                if (playedThisRound) return new(false, "Word already played this round", null);

                currentTurn.Word = w;
                currentTurn.EndedAt = now;
                currentTurn.EndReason = TurnEndReason.END;
                player.LastWord = w;
                player.TurnsPlayedThisRound += 1;
                game.LastWord = w;
            }

            bool roundEnded = game.Players.Count(p => p.IsTaken && p.IsActive) <= 1;
            bool matchEnded = false;

            TurnState? currentTurnState = null;

            if (roundEnded)
            {
                Round round = game.CurrentRound;
                Player? winner = game.Players.FirstOrDefault(p => p.IsActive && p.IsTaken);
                round.EndedAt = now;
                round.WinnerId = winner?.Id;
                if (winner is not null)
                {
                    winner.RoundWins += 1;
                    matchEnded = winner.RoundWins >= game.TargetWins || game.Players.Count(p => p.IsTaken) <= 1; ;
                }

                if (matchEnded)
                {
                    game.Status = GameStatus.FINISHED;
                }
                else
                {
                    ResetPlayers(game.Players);
                    int startSeat = winner?.Seat ?? 1;
                    Round nextRound = new()
                    {
                        GameId = game.Id,
                        Index = game.Rounds.Count,
                        StartedAt = now
                    };

                    game.Rounds.Add(nextRound);
                    game.CurrentRound = nextRound;

                    Player? firstPlayer = FindNextValidPlayer(game.Players, startSeat - 1)
                        ?? throw new Exception("No valid player to start round");

                    currentTurnState = await CreateTurnAsync(game, nextRound, firstPlayer, now, ct);
                }
            }

            if (!roundEnded)
            {
                Player? nextPlayer = FindNextValidPlayer(game.Players, game.CurSeat)
                    ?? throw new Exception("No valid player to start turn");
                currentTurnState = await CreateTurnAsync(game, game.CurrentRound!, nextPlayer, now, ct);
            }

            if (currentTurnState is null)
            {
                currentTurnState = BuildTurnState(game, currentTurn, player, Math.Max(0, personalIndex));
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            List<PlayerState> players = [.. game.Players
                .Where(p => p.IsTaken)
                .Select(State.PlayerToState)];
            RoundState roundState = State.RoundToState(game.CurrentRound!);
            GameState state = State.GameToState(game, players, roundState, currentTurnState);

            return new(true, null, state);
        }
        public async Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct)
        {
            return await _db.Players
              .Where(p => p.GameId == gameId && p.IsTaken)
              .OrderBy(p => p.Seat)
              .AsNoTracking()
              .Select(p => new PlayerState(p.Id, p.Seat, p.IsActive, p.IsConnected, p.Name, p.IconUrl, p.RoundWins, p.LastWord))
              .ToListAsync(ct);
        }
        public async Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
              .AsNoTracking()
              .Include(g => g.Players)
              .Include(g => g.CurrentTurn)
              .Include(g => g.CurrentRound)
              .Include(g => g.Effects)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) throw new Exception("Game not found");
            List<PlayerState> players = game.Players
                .Where(p => p.IsTaken)
                .Select(p => State.PlayerToState(p))
                .ToList();
            if (game.CurrentTurn is not null && game.CurrentRound is not null)
            {
                Player currentPlayer = game.Players
                    .FirstOrDefault(p => p.Seat == game.CurrentTurn!.Seat)
                    ?? throw new Exception("Current turn player not found");

                TurnState turnState = BuildTurnState(game, game.CurrentTurn, currentPlayer, currentPlayer.TurnsPlayedThisRound);
                return State.GameToState(game,
                    players,
                    State.RoundToState(game.CurrentRound),
                    turnState);
            }
            else
            {
                return new GameState(game.Id, game.Status, game.CurSeat, game.TargetWins, 0, players, null, null);
            }
        }
        public async Task<GameCodeResponse> GetUserActiveGameCodeAsync(Guid userId, CancellationToken ct)
        {
            string? activeGameCode = await _db.Users.
                Where(u => u.Id == userId).
                Select(u => u.ActivePlayer).
                Where(p => p != null).
                Select(p => p!.Game).
                Where(g => g.Status == GameStatus.ACTIVE || g.Status == GameStatus.WAITING).
                Select(g => g.Code).
                FirstOrDefaultAsync(ct);
            if (activeGameCode is null)
            {
                return new GameCodeResponse(true, false, null);
            }
            return new GameCodeResponse(true, true, activeGameCode);
        }
        public async Task<int> GetCurrentSeatAsync(Guid gameId, CancellationToken ct)
        {
            var seat = await _db.Turns
              .AsNoTracking()
              .Where(t => t.GameId == gameId && t.EndedAt == null)
              .OrderByDescending(t => t.Index)
              .Select(t => t.Seat)
              .FirstOrDefaultAsync(ct);
            if (seat == 0) throw new Exception("No active turn found");
            return seat;
        }
        private Player? FindNextValidPlayer(IReadOnlyList<Player> players, int curSeat)
        {
            Player? candidate = null;
            int candidateSeat = int.MaxValue;

            if (players.Count == 0)
                return null;

            foreach (var p in players)
            {
                if (!p.IsTaken || !p.IsActive)
                    continue;

                if (p.Seat > curSeat && p.Seat < candidateSeat)
                {
                    candidateSeat = p.Seat;
                    candidate = p;
                }
            }

            if (candidate != null)
                return candidate;

            candidate = null;
            candidateSeat = int.MaxValue;

            foreach (var p in players)
            {
                if (!p.IsTaken || !p.IsActive)
                    continue;

                if (p.Seat < candidateSeat)
                {
                    candidateSeat = p.Seat;
                    candidate = p;
                }
            }

            return candidate;
        }

        async public Task<JoinGameResponse> JoinGameAsync(Guid gameId, Guid userId, CancellationToken ct)
        {
            var game = await _db.Games
              .Include(g => g.Players)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct) ?? throw new Exception("Game not found");
            //check for reconnect
            User? user = _db.Users.Include(u => u.ActivePlayer).FirstOrDefault(u => u.Id == userId) ?? throw new Exception("User does not exist when joining game");
            Player? player = user.ActivePlayer;
            //if player is already in a game
            if (player is not null && player.GameId == gameId && player.IsTaken)
            {
                player.IsConnected = true;
                await _db.SaveChangesAsync(ct);
                GameState? gameState = await GetGameStateAsync(gameId, ct) ?? throw new Exception("Failed to get game state after joining");
                return new JoinGameResponse(player.Id, gameState);
            }
            else if (game.Status != GameStatus.WAITING) throw new Exception("Game not joinable");
            Player? inactive = game.Players.FirstOrDefault(p => !p.IsConnected && !p.IsTaken);
            if (inactive is null) throw new Exception("Game full");
            inactive.IsActive = false;
            inactive.IsConnected = true;
            inactive.IsTaken = true;
            inactive.Name = user.Name ?? "Anonymous";
            inactive.IconUrl = user.IconUrl;
            user.ActivePlayer = inactive;
            await _db.SaveChangesAsync(ct);
            GameState? state = await GetGameStateAsync(gameId, ct);
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
        async public Task LeaveGameAsync(Guid userId, CancellationToken ct)
        {
            var user = await _db.Users
                .Include(u => u.ActivePlayer)
                .FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw new Exception("User not found when leaving game");
            user.ActivePlayer!.IsConnected = false;
            user.ActivePlayer.IsTaken = false;
            user.ActivePlayer = null;

            await _db.SaveChangesAsync(ct);
        }
        public async Task<GameState?> TimeoutAndAdvanceAsync(Guid gameId, Guid turnId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            await using IDbContextTransaction tx = await _db.Database.BeginTransactionAsync(ct);

            Game? game = await _db.Games
                .Include(g => g.Players)
                .Include(g => g.CurrentRound)
                .Include(g => g.CurrentTurn)
                .FirstOrDefaultAsync(g => g.Id == gameId, ct);

            if (game is null || game.CurrentTurn is null || game.CurrentRound is null || game.CurrentTurn.Id != turnId)
            {
                return null;
            }

            Turn currentTurn = game.CurrentTurn;
            if (currentTurn.EndedAt != null)
            {
                return null;
            }

            Player? player = game.Players.SingleOrDefault(p => p.Seat == currentTurn.Seat);
            if (player is null)
            {
                return null;
            }

            int personalIndex = player.TurnsPlayedThisRound;
            bool timedOut = now >= currentTurn.DueAt;

            if (timedOut)
            {
                currentTurn.EndedAt = now;
                currentTurn.EndReason = TurnEndReason.TIMEOUT;
                player.IsActive = false;
                player.TurnsPlayedThisRound += 1;
            }
            else
            {
                return null;
            }

            bool roundEnded = game.Players.Count(p => p.IsTaken && p.IsActive) <= 1;
            bool matchEnded = false;

            TurnState? currentTurnState = null;

            if (roundEnded)
            {
                Round round = game.CurrentRound;
                Player? winner = game.Players.FirstOrDefault(p => p.IsActive && p.IsTaken);
                round.EndedAt = now;
                round.WinnerId = winner?.Id;
                if (winner is not null)
                {
                    winner.RoundWins += 1;
                    matchEnded = winner.RoundWins >= game.TargetWins || game.Players.Count(p => p.IsTaken) <= 1;
                }

                if (matchEnded)
                {
                    game.Status = GameStatus.FINISHED;
                }
                else
                {
                    ResetPlayers(game.Players);
                    int startSeat = winner?.Seat ?? 1;
                    Round nextRound = new()
                    {
                        GameId = game.Id,
                        Index = game.Rounds.Count,
                        StartedAt = now
                    };

                    game.Rounds.Add(nextRound);
                    game.CurrentRound = nextRound;

                    Player? firstPlayer = FindNextValidPlayer(game.Players, startSeat - 1)
                        ?? throw new Exception("No valid player to start round");

                    currentTurnState = await CreateTurnAsync(game, nextRound, firstPlayer, now, ct);
                }
            }

            if (!roundEnded)
            {
                Player? nextPlayer = FindNextValidPlayer(game.Players, game.CurSeat)
                    ?? throw new Exception("No valid player to start turn");
                currentTurnState = await CreateTurnAsync(game, game.CurrentRound!, nextPlayer, now, ct);
            }

            if (currentTurnState is null)
            {
                currentTurnState = BuildTurnState(game, currentTurn, player, Math.Max(0, personalIndex));
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            List<PlayerState> players = [.. game.Players
                .Where(p => p.IsTaken)
                .Select(State.PlayerToState)];
            RoundState roundState = State.RoundToState(game.CurrentRound!);
            GameState state = State.GameToState(game, players, roundState, currentTurnState);

            return state;
        }

        public async Task ForceEndGame(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            var allTurns = await _db.Turns
              .Where(t => t.GameId == gameId && t.EndedAt == null)
              .ToListAsync(ct);
            if (game is null) throw new Exception("Game not found");
            foreach (var turn in allTurns)
            {
                turn.EndedAt = DateTime.UtcNow;
                turn.EndReason = TurnEndReason.END;
                _tt.Cancel(turn.Id);
            }
            game.Status = GameStatus.FINISHED;
            await _db.SaveChangesAsync(ct);
        }
    }
}