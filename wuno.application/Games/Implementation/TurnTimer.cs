using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Games.Util;

namespace Wuno.Application.Games.Implementation
{
    public sealed class TurnTimer : ITurnTimer
    {
        private readonly IServiceScopeFactory _sf;
        private readonly ILogger<TurnTimer> _logger;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _map = new();
        
        public TurnTimer(IServiceScopeFactory sf, ILogger<TurnTimer> logger)
        {
            _sf = sf;
            _logger = logger;
        }
        
        public bool Schedule(Guid gameId, Guid turnId, DateTime dueAtUTC, Func<GameState, Task> Broadcast)
        {
            var delay = dueAtUTC - DateTime.UtcNow;
            if(delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            var cts = new CancellationTokenSource();
            if (_map.TryRemove(turnId, out var existing))
            {
                try { existing.Cancel(); }
                finally { existing.Dispose(); }
            }
            _map[turnId] = cts;
            
            _logger.LogDebug("Scheduling timer for turn {TurnId} in game {GameId}, due in {Delay}ms", 
                turnId, gameId, delay.TotalMilliseconds);
            
            _ = RunTimerAsync(gameId, turnId, delay, Broadcast, cts);
            return true;
        }

        private async Task RunTimerAsync(Guid gameId, Guid turnId, TimeSpan delay, Func<GameState, Task> Broadcast, CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(delay, cts.Token);
                if(cts.IsCancellationRequested)
                {
                    _logger.LogDebug("Timer for turn {TurnId} was cancelled during delay", turnId);
                    return;
                }

                _logger.LogInformation("Timer fired for turn {TurnId} in game {GameId}", turnId, gameId);

                using var scope = _sf.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IGameService>();
                GameState? state = await svc.TimeoutAndAdvanceAsync(gameId, turnId, CancellationToken.None);
                
                if (state is null)
                {
                    _logger.LogDebug("TimeoutAndAdvanceAsync returned null for turn {TurnId} - turn may have already ended", turnId);
                    return;
                }
                
                _logger.LogInformation("Turn {TurnId} timed out, advanced to turn {NextTurnId}", 
                    turnId, state.CurrentTurn?.TurnId);
                
                // Call broadcast - let it handle its own exceptions
                // The broadcast callback is responsible for scheduling the next timer
                await Broadcast(state);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Timer for turn {TurnId} was cancelled", turnId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Timer failed for turn {TurnId} in game {GameId}", turnId, gameId);
            }
            finally
            {
                _map.TryRemove(turnId, out _);
                cts.Dispose();
            }
        }

        public void Cancel(Guid turnId)
        {
            if (_map.TryRemove(turnId, out var cts))
            {
                _logger.LogDebug("Cancelling timer for turn {TurnId}", turnId);
                try { cts.Cancel(); } finally { cts.Dispose(); }
            }
        }
    }
}

