using Microsoft.EntityFrameworkCore;
using wuno.domain;
using wuno.infrastructure;
using Wuno.Application.Games.Util;

namespace Wuno.Application.Users
{
    public sealed class StatsService(AppDbContext db) : IStatsService
    {
        private readonly AppDbContext _db = db;

        public async Task<UserStatsResponse> GetUserStatsAsync(Guid userId, CancellationToken ct)
        {
            // Get all player records for this user
            var playerIds = await _db.Players
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (playerIds.Count == 0)
            {
                return new UserStatsResponse(
                    Ok: true,
                    GamesPlayed: 0,
                    GamesWon: 0,
                    WinRate: 0,
                    RoundsWon: 0,
                    HighestSingleRoundScore: 0,
                    TopWords: new List<TopWordEntry>(),
                    TotalWordsPlayed: 0,
                    AverageWordLength: 0,
                    LongestWord: null
                );
            }

            // Get player info for this user across all games
            var userPlayerIds = await _db.Players
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync(ct);
            
            var userPlayerIdSet = userPlayerIds.ToHashSet();

            // Get all games the user participated in (finished games only for accurate stats)
            var gamesPlayed = await _db.Games
                .Where(g => g.Status == GameStatus.FINISHED)
                .Where(g => g.Players.Any(p => p.UserId == userId))
                .CountAsync(ct);

            // Count games won using Game.WinnerId directly
            var gamesWon = await _db.Games
                .Where(g => g.Status == GameStatus.FINISHED && g.WinnerId != null)
                .Where(g => userPlayerIdSet.Contains(g.WinnerId!.Value))
                .CountAsync(ct);

            // Total rounds won using Round.WinnerId
            var roundsWon = await _db.Rounds
                .Where(r => r.WinnerId != null && userPlayerIdSet.Contains(r.WinnerId.Value))
                .CountAsync(ct);

            // Get all turns (words) played by this user
            var userTurns = await _db.Turns
                .Where(t => t.EndReason == TurnEndReason.END && t.Word != null)
                .Where(t => _db.Players
                    .Where(p => p.UserId == userId)
                    .Any(p => p.GameId == t.GameId && p.Seat == t.Seat))
                .Select(t => new { t.Word, t.Score })
                .ToListAsync(ct);

            var totalWordsPlayed = userTurns.Count;
            var highestScore = userTurns.Count > 0 ? userTurns.Max(t => t.Score) : 0;
            var averageWordLength = userTurns.Count > 0 
                ? userTurns.Average(t => t.Word?.Length ?? 0) 
                : 0;
            var longestWord = userTurns
                .OrderByDescending(t => t.Word?.Length ?? 0)
                .Select(t => t.Word)
                .FirstOrDefault();

            // Top 3 words by score
            var topWords = userTurns
                .Where(t => t.Word != null)
                .GroupBy(t => t.Word!.ToUpperInvariant())
                .Select(g => new TopWordEntry(g.First().Word!, g.Max(t => t.Score)))
                .OrderByDescending(w => w.Score)
                .Take(3)
                .ToList();

            var winRate = gamesPlayed > 0 ? (double)gamesWon / gamesPlayed * 100 : 0;

            return new UserStatsResponse(
                Ok: true,
                GamesPlayed: gamesPlayed,
                GamesWon: gamesWon,
                WinRate: Math.Round(winRate, 1),
                RoundsWon: roundsWon,
                HighestSingleRoundScore: highestScore,
                TopWords: topWords,
                TotalWordsPlayed: totalWordsPlayed,
                AverageWordLength: Math.Round(averageWordLength, 1),
                LongestWord: longestWord
            );
        }

        public async Task<InGameStatsResponse> GetInGameStatsAsync(Guid userId, CancellationToken ct)
        {
            // Find all player IDs for this user
            var userPlayerIds = await _db.Players
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (userPlayerIds.Count == 0)
            {
                return new InGameStatsResponse(
                    TotalWins: 0,
                    GamesPlayed: 0,
                    WinRate: 0,
                    HighestScore: 0,
                    TopWords: new List<TopWordEntry>()
                );
            }

            var userPlayerIdSet = userPlayerIds.ToHashSet();

            // Count games played (finished games only)
            var gamesPlayed = await _db.Games
                .Where(g => g.Status == GameStatus.FINISHED)
                .Where(g => g.Players.Any(p => p.UserId == userId))
                .CountAsync(ct);

            // Count games won using Game.WinnerId directly
            var gamesWon = await _db.Games
                .Where(g => g.Status == GameStatus.FINISHED && g.WinnerId != null)
                .Where(g => userPlayerIdSet.Contains(g.WinnerId!.Value))
                .CountAsync(ct);

            var userTurns = await _db.Turns
                .Where(t => t.EndReason == TurnEndReason.END && t.Word != null)
                .Where(t => _db.Players
                    .Where(p => p.UserId == userId)
                    .Any(p => p.GameId == t.GameId && p.Seat == t.Seat))
                .Select(t => new { t.Word, t.Score })
                .ToListAsync(ct);

            var highestScore = userTurns.Count > 0 ? userTurns.Max(t => t.Score) : 0;

            var topWords = userTurns
                .Where(t => t.Word != null)
                .GroupBy(t => t.Word!.ToUpperInvariant())
                .Select(g => new TopWordEntry(g.First().Word!, g.Max(t => t.Score)))
                .OrderByDescending(w => w.Score)
                .Take(3)
                .ToList();

            var winRate = gamesPlayed > 0 ? (double)gamesWon / gamesPlayed * 100 : 0;

            return new InGameStatsResponse(
                TotalWins: gamesWon,
                GamesPlayed: gamesPlayed,
                WinRate: Math.Round(winRate, 1),
                HighestScore: highestScore,
                TopWords: topWords
            );
        }
    }
}
