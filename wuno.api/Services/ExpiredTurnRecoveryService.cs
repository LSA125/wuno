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
            
            _logger.LogInformation("ExpiredTurnRecoveryService running one-time startup recovery");

            try
            {
                await RecoverExpiredTurnsAsync(stoppingToken);
                _logger.LogInformation("ExpiredTurnRecoveryService completed startup recovery");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during expired turn recovery");
            }
            
            // Service completes after one-time recovery - the in-memory TurnTimer handles ongoing expirations
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
                        // Schedule the next turn timer FIRST if game is still active
                        // This ensures the timer chain never breaks, even if broadcast fails
                        if (state.Status != GameStatus.FINISHED && state.CurrentTurn != null)
                        {
                            var dueAt = DateTime.SpecifyKind(state.CurrentTurn.DueAt, DateTimeKind.Utc);
                            _turnTimer.Schedule(state.GameId, state.CurrentTurn.TurnId, dueAt, BroadcastAfterTimeoutAsync);
                        }
                        
                        // Now broadcast - if this fails, timer is already scheduled
                        try
                        {
                            await _hubContext.Clients.Group($"game:{expired.GameId}").SendAsync("GameUpdated", state, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Broadcast failed for game {GameId}", expired.GameId);
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
        
        /// <summary>
        /// Callback for turn timer - schedules the next timer before broadcasting.
        /// </summary>
        private async Task BroadcastAfterTimeoutAsync(Wuno.Application.Games.Util.GameState state)
        {
            // Schedule next timer FIRST to ensure chain never breaks
            if (state.Status != GameStatus.FINISHED && state.CurrentTurn != null)
            {
                var dueAt = DateTime.SpecifyKind(state.CurrentTurn.DueAt, DateTimeKind.Utc);
                _turnTimer.Schedule(state.GameId, state.CurrentTurn.TurnId, dueAt, BroadcastAfterTimeoutAsync);
            }
            
            // Attempt broadcast - if this fails, timer chain is already scheduled
            try
            {
                await _hubContext.Clients.Group($"game:{state.GameId}").SendAsync("GameUpdated", state);
            }
            catch (Exception)
            {
                // Broadcast failed but timer is already scheduled
            }
        }
    }
}
