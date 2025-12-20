using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using wuno.domain;
using wuno.infrastructure;
using Wuno.Application.Games.Inheritance;

namespace Wuno.Api.Services
{
    /// <summary>
    /// Background service that recovers orphaned turns after server restart.
    /// When the server restarts, the in-memory turn timer map is cleared, causing
    /// active turns to never timeout. This service periodically checks for expired
    /// turns and processes them.
    /// </summary>
    public sealed class ExpiredTurnRecoveryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiredTurnRecoveryService> _logger;
        private readonly IHubContext<Hubs.GameHub> _hubContext;
        private readonly ITurnTimer _turnTimer;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);  // Check every 10 seconds
        private readonly TimeSpan _graceBuffer = TimeSpan.FromSeconds(2);  // Buffer before considering a turn expired

        public ExpiredTurnRecoveryService(
            IServiceScopeFactory scopeFactory, 
            ILogger<ExpiredTurnRecoveryService> logger,
            IHubContext<Hubs.GameHub> hubContext,
            ITurnTimer turnTimer)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext;
            _turnTimer = turnTimer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a bit for app startup
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            
            _logger.LogInformation("ExpiredTurnRecoveryService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RecoverExpiredTurnsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during expired turn recovery");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task RecoverExpiredTurnsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

            var now = DateTime.UtcNow;
            var expiredThreshold = now - _graceBuffer;  // Only process turns that are truly expired

            // Find active games with expired turns that haven't ended
            var expiredTurns = await db.Games
                .Include(g => g.CurrentTurn)
                .Where(g => g.Status == GameStatus.ACTIVE)
                .Where(g => g.CurrentTurn != null && g.CurrentTurn.EndedAt == null)
                .Where(g => g.CurrentTurn!.DueAt < expiredThreshold)
                .Select(g => new { GameId = g.Id, TurnId = g.CurrentTurn!.Id })
                .ToListAsync(ct);

            foreach (var expired in expiredTurns)
            {
                try
                {
                    _logger.LogWarning("Recovering expired turn {TurnId} for game {GameId}", expired.TurnId, expired.GameId);
                    
                    var state = await gameService.TimeoutAndAdvanceAsync(expired.GameId, expired.TurnId, ct);
                    
                    if (state != null)
                    {
                        // Broadcast the updated state
                        await _hubContext.Clients.Group($"game:{expired.GameId}").SendAsync("GameUpdated", state, ct);
                        
                        // Schedule the next turn timer if game is still active
                        if (state.Status != GameStatus.FINISHED)
                        {
                            var dueAt = DateTime.SpecifyKind(state.CurrentTurn.DueAt, DateTimeKind.Utc);
                            _turnTimer.Schedule(state.GameId, state.CurrentTurn.TurnId, dueAt, async (s) =>
                            {
                                await _hubContext.Clients.Group($"game:{s.GameId}").SendAsync("GameUpdated", s);
                            });
                        }
                        
                        _logger.LogInformation("Recovered turn {TurnId} and advanced game {GameId}", expired.TurnId, expired.GameId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recover turn {TurnId} for game {GameId}", expired.TurnId, expired.GameId);
                }
            }
        }
    }
}
