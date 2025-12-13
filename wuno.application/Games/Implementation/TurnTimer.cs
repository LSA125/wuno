using Microsoft.Extensions.DependencyInjection;
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
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _map = new();
        public TurnTimer(IServiceScopeFactory sf)
        {
            _sf = sf;
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
            _ = RunTimerAsync(gameId, turnId, delay, Broadcast, cts);
            return true;
        }

        private async Task RunTimerAsync(Guid gameId, Guid turnId, TimeSpan delay, Func<GameState, Task> Broadcast, CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(delay, cts.Token);
                if(cts.IsCancellationRequested)
                    return;

                using var scope = _sf.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IGameService>();
                GameState? state = await svc.TimeoutAndAdvanceAsync(gameId, turnId, CancellationToken.None);
                if (state is not null)
                {
                    await Broadcast(state);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                _map.TryRemove(turnId, out _);
                cts.Dispose();
            }
        }

        public void Cancel(Guid turnId)
        {
            if (_map.TryRemove(turnId, out var cts))
                try { cts.Cancel(); } finally { cts.Dispose(); }
        }
    }
}
