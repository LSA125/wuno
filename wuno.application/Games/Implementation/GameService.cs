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
                p.RemainingTime = Constants.INITIAL_REMAINING_TIME_SEC;
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
                IsPublic = req.IsPublic,
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
        private TurnState BuildTurnState(Turn turn)
        {
            return State.TurnToState(turn);
        }
        private async Task<TurnState> CreateTurnAsync(Game game, Round round, Player nextPlayer, DateTime now, CancellationToken ct, Turn? previousTurn = null, int previousScore = 0)
        {
            var previous = previousTurn ?? game.CurrentTurn;
            Turn newTurn;

            if (previous is null)
            {
                // First turn of round - use first turn max time
                int tMax = EffectsLogic.CalculateMaxTime(nextPlayer.TurnsPlayedThisRound, isFirstTurn: true);
                int duration = Math.Max(Math.Min(Constants.INITIAL_REMAINING_TIME_SEC, tMax), Constants.MIN_ACTUAL_TIME_SEC);

                newTurn = new Turn
                {
                    GameId = game.Id,
                    RoundId = round.Id,
                    Index = 0,
                    Seat = nextPlayer.Seat,
                    MinLen = 0,
                    StartedAt = now,
                    DueAt = now.AddSeconds(duration)
                };
            }
            else
            {
                int personalTurns = nextPlayer.TurnsPlayedThisRound;

                // Calculate opponent debuffs from the previous turn
                double opponentTimeDebuff = 0;
                int opponentMinLenDebuff = 0;
                if (previousScore > 0)
                {
                    // Previous player's excess becomes debuff for this player
                    int prevPlayerTMax = EffectsLogic.CalculateMaxTime(personalTurns, isFirstTurn: false);
                    int prevLRequired = EffectsLogic.CalculateMinLength(personalTurns, 0);
                    (opponentTimeDebuff, opponentMinLenDebuff) = EffectsLogic.CalculateOpponentDebuffs(
                        Constants.INITIAL_REMAINING_TIME_SEC, previousScore, prevPlayerTMax, prevLRequired);
                }

                // Calculate min length with opponent debuff
                int minLen = EffectsLogic.CalculateMinLength(personalTurns, 0) + opponentMinLenDebuff;

                // Calculate turn duration
                int tMax = EffectsLogic.CalculateMaxTime(personalTurns, isFirstTurn: false);
                int duration = EffectsLogic.CalculateActualTime(
                    Constants.INITIAL_REMAINING_TIME_SEC, 0, opponentTimeDebuff, tMax);

                newTurn = new Turn
                {
                    GameId = game.Id,
                    RoundId = round.Id,
                    Index = await _db.Turns.CountAsync(t => t.GameId == game.Id, ct),
                    Seat = nextPlayer.Seat,
                    MinLen = Math.Max(0, minLen),
                    StartedAt = now,
                    DueAt = now.AddSeconds(duration)
                };
            }

            _db.Turns.Add(newTurn);
            game.CurSeat = nextPlayer.Seat;
            game.CurrentTurn = newTurn;

            return State.TurnToState(newTurn);
        }
        public async Task<ProcessTurnOutcome> ProcessTurnAsync(Guid gameId, Guid roundId, Guid turnId, Guid playerId, int seat, string word, CancellationToken ct)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
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
                    return new ProcessTurnOutcome(false, "Not found", null, null);
                }

                Turn currentTurn = game.CurrentTurn;
                if (currentTurn.EndedAt != null)
                {
                    return new ProcessTurnOutcome(false, "Turn already processed", null, null);
                }

                Player? player = game.Players.SingleOrDefault(p => p.Id == playerId);
                if (player is null || player.Seat != seat || currentTurn.Seat != seat)
                {
                    return new ProcessTurnOutcome(false, "Not your turn", null, null);
                }

                int personalIndex = player.TurnsPlayedThisRound;
                bool timedOut = now > currentTurn.DueAt;
                TurnHistoryState? completedTurn = null;
                int wordScore = 0;

                if (timedOut)
                {
                    currentTurn.EndedAt = now;
                    currentTurn.EndReason = TurnEndReason.TIMEOUT;
                    player.IsActive = false;
                    player.TurnsPlayedThisRound += 1;
                    // Reset remaining time on timeout
                    player.RemainingTime = 0;
                }
                else
                {
                    var w = word;
                    if (!_wl.IsWord(w)) return new ProcessTurnOutcome(false, "Not a valid word", null, null);
                    if (w.Length < currentTurn.MinLen) return new ProcessTurnOutcome(false, $"Word too short (min {currentTurn.MinLen})", null, null);
                    if (!game.LastWord.IsNullOrEmpty()
                        && w.First() != game.LastWord!.Last())
                    {
                        return new ProcessTurnOutcome(false, $"Word must start with '{game.LastWord!.Last()}'", null, null);
                    }
                    bool playedThisRound = await _db.Turns.AnyAsync(t => t.GameId == gameId && t.RoundId == roundId && t.Word == w, ct);
                    if (playedThisRound) return new ProcessTurnOutcome(false, "Word already played this round", null, null);

                    // Calculate score based on reverse match with opponent's last word
                    wordScore = LetterScoring.CalculateScore(w, game.LastWord);

                    // Calculate remaining time: actual time left + bonus from score
                    double actualTimeRemaining = Math.Max(0, (currentTurn.DueAt - now).TotalSeconds);
                    var (timeBonus, _) = EffectsLogic.CalculateBonuses(wordScore);
                    player.RemainingTime = actualTimeRemaining + timeBonus;

                    currentTurn.Word = w;
                    currentTurn.Score = wordScore;
                    currentTurn.EndedAt = now;
                    currentTurn.EndReason = TurnEndReason.END;
                    player.LastWord = w;
                    player.TurnsPlayedThisRound += 1;
                    game.LastWord = w;
                    completedTurn = new TurnHistoryState(currentTurn.Id, currentTurn.Index, seat, w, currentTurn.MinLen, wordScore);
                }

                bool roundEnded = game.Players.Count(p => p.IsTaken && p.IsActive) <= 1;
                bool matchEnded = false;

                TurnState? currentTurnState = null;

                if (roundEnded)
                {
                    Round round = game.CurrentRound;
                    Player? winner = game.Players.FirstOrDefault(p => p.IsActive && p.IsTaken);
                    game.LastWord = null;
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
                            Index = await _db.Rounds.CountAsync(r => r.GameId == game.Id, ct),
                            StartedAt = now
                        };
                        _db.Rounds.Add(nextRound);
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
                    currentTurnState = await CreateTurnAsync(game, game.CurrentRound!, nextPlayer, now, ct, currentTurn, wordScore);
                }

                if (currentTurnState is null)
                {
                    currentTurnState = BuildTurnState(currentTurn);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                List<PlayerState> players = [.. game.Players
                    .Where(p => p.IsTaken)
                    .Select(State.PlayerToState)];
                RoundState roundState = State.RoundToState(game.CurrentRound!);
                GameState state = State.GameToState(game, players, roundState, currentTurnState);

                return new ProcessTurnOutcome(true, null, state, completedTurn);
            });
        }
        public async Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct)
        {
            var players = await _db.Players
              .Where(p => p.GameId == gameId && p.IsTaken)
              .OrderBy(p => p.Seat)
              .AsNoTracking()
              .ToListAsync(ct);
            return players.Select(State.PlayerToState).ToList();
        }
        public async Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
              .AsNoTracking()
              .Include(g => g.Players)
              .Include(g => g.CurrentTurn)
              .Include(g => g.CurrentRound)
              .FirstOrDefaultAsync(g => g.Id == gameId, ct);
            if (game is null) throw new Exception("Game not found");
            List<PlayerState> players = game.Players
                .Where(p => p.IsTaken)
                .Select(p => State.PlayerToState(p))
                .ToList();
            if (game.CurrentTurn is not null && game.CurrentRound is not null)
            {
                TurnState turnState = BuildTurnState(game.CurrentTurn);
                return State.GameToState(game,
                    players,
                    State.RoundToState(game.CurrentRound),
                    turnState);
            }
            else
            {
                return new GameState(
                    game.Id,
                    game.Status,
                    game.CurSeat,
                    game.Direction,
                    game.TargetWins,
                    game.LastWord,
                    players,
                    null,
                    null);
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

        public async Task<List<TurnHistoryState>> GetRecentWordHistoryAsync(Guid gameId, CancellationToken ct)
        {
            var game = await _db.Games
                .AsNoTracking()
                .Include(g => g.CurrentRound)
                .FirstOrDefaultAsync(g => g.Id == gameId, ct);

            if (game?.CurrentRound is null)
            {
                return [];
            }

            Guid roundId = game.CurrentRound.Id;

            var turns = await _db.Turns
                .AsNoTracking()
                .Where(t => t.GameId == gameId && t.RoundId == roundId && t.Word != null)
                .OrderBy(t => t.Index)
                .ToListAsync(ct);

            return turns.Select(t => new TurnHistoryState(t.Id, t.Index, t.Seat, t.Word!, t.MinLen, t.Score)).ToList();
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
            else if (game.Status != GameStatus.WAITING && game.Status != GameStatus.ACTIVE) throw new Exception("Game not joinable");
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
        async public Task<Guid?> LeaveGameAsync(Guid userId, CancellationToken ct)
        {
            var user = await _db.Users
                .Include(u => u.ActivePlayer)
                .FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw new Exception("User not found when leaving game");
            
            var player = user.ActivePlayer;
            Guid? gameId = null;
            
            if (player != null)
            {
                gameId = player.GameId;
                
                // Reset player slot so another player can take it
                player.IsConnected = false;
                player.IsTaken = false;
                player.IsActive = false;
                player.Name = "";
                player.IconUrl = null;
                player.RoundWins = 0;
                player.LastWord = null;
                player.TurnsPlayedThisRound = 0;
                player.RemainingTime = Constants.INITIAL_REMAINING_TIME_SEC;
                player.UserId = null;
                
                // Check if there are any taken players left
                var remainingPlayers = await _db.Players
                    .CountAsync(p => p.GameId == gameId && p.IsTaken && p.Id != player.Id, ct);
                
                if (remainingPlayers <= 1)
                {
                    // 0 or 1 player left - force end the game
                    var game = await _db.Games.FindAsync([gameId], ct);
                    if (game != null && game.Status != GameStatus.FINISHED)
                    {
                        game.Status = GameStatus.FINISHED;
                        // Cancel any active turns
                        var activeTurns = await _db.Turns
                            .Where(t => t.GameId == gameId && t.EndedAt == null)
                            .ToListAsync(ct);
                        foreach (var turn in activeTurns)
                        {
                            turn.EndedAt = DateTime.UtcNow;
                            turn.EndReason = TurnEndReason.END;
                            _tt.Cancel(turn.Id);
                        }
                    }
                }
            }
            user.ActivePlayer = null;

            await _db.SaveChangesAsync(ct);
            return gameId;
        }
        public async Task<GameState?> TimeoutAndAdvanceAsync(Guid gameId, Guid turnId, CancellationToken ct)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
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
                    game.LastWord = null;
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
                            Index = await _db.Rounds.CountAsync(r => r.GameId == game.Id, ct),
                            StartedAt = now
                        };

                        _db.Rounds.Add(nextRound);
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
                    currentTurnState = BuildTurnState(currentTurn);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                List<PlayerState> players = [.. game.Players
                    .Where(p => p.IsTaken)
                    .Select(State.PlayerToState)];
                RoundState roundState = State.RoundToState(game.CurrentRound!);
                GameState state = State.GameToState(game, players, roundState, currentTurnState);

                return state;
            });
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

        public async Task<MatchmakingResponse> FindOrCreatePublicGameAsync(CancellationToken ct)
        {
            // Find a public game with open slots
            var publicGame = await _db.Games
                .Include(g => g.Players)
                .Where(g => g.IsPublic && (g.Status == GameStatus.WAITING || g.Status == GameStatus.ACTIVE))
                .Where(g => g.Players.Any(p => !p.IsTaken))
                .OrderBy(g => g.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (publicGame != null)
            {
                return new MatchmakingResponse(true, publicGame.Code, false);
            }

            // No public game found, create one with defaults
            var newGame = await StartNewGameAsync(new NewGameRequest(
                PlayerCount: 4,
                TargetWins: 3,
                IsPublic: true
            ), ct);

            return new MatchmakingResponse(true, newGame.GameCode, true);
        }
    }
}