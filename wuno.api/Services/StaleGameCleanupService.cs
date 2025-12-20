using Microsoft.EntityFrameworkCore;
using wuno.domain;
using wuno.infrastructure;
using Wuno.Application.Games.Inheritance;

namespace Wuno.Api.Services
{
    /// <summary>
    /// Background service that cleans up stale games where all players are disconnected.
    /// This handles the case where players close their browser without properly leaving the game.
    /// </summary>
    public sealed class StaleGameCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StaleGameCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);  // Run every 30 minutes
        private readonly TimeSpan _staleThreshold = TimeSpan.FromHours(1);  // Games stale for 1+ hour

        public StaleGameCleanupService(IServiceScopeFactory scopeFactory, ILogger<StaleGameCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a bit before first run to let app startup complete
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupStaleGamesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during stale game cleanup");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CleanupStaleGamesAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Find games that are WAITING or ACTIVE where:
            // - All players with IsTaken = true have IsConnected = false
            // - OR there are no taken players at all
            var staleGames = await db.Games
                .Include(g => g.Players)
                .Include(g => g.Turns)
                .Where(g => g.Status == GameStatus.WAITING || g.Status == GameStatus.ACTIVE)
                .Where(g => !g.Players.Any(p => p.IsTaken && p.IsConnected))  // No connected taken players
                .ToListAsync(ct);

            var gamesEnded = 0;
            foreach (var game in staleGames)
            {
                // Force end the game
                game.Status = GameStatus.FINISHED;
                
                // End any active turns
                foreach (var turn in game.Turns.Where(t => t.EndedAt == null))
                {
                    turn.EndedAt = DateTime.UtcNow;
                    turn.EndReason = TurnEndReason.END;
                }
                
                // Reset all player slots
                foreach (var player in game.Players)
                {
                    player.IsConnected = false;
                    player.IsTaken = false;
                    player.IsActive = false;
                    player.Name = "";
                    player.IconUrl = null;
                    player.RoundWins = 0;
                    player.LastWord = null;
                    player.TurnsPlayedThisRound = 0;
                    player.UserId = null;
                }
                
                gamesEnded++;
            }

            if (gamesEnded > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Cleaned up {Count} stale games", gamesEnded);
            }
        }
    }
}
