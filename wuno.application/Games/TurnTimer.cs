using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games
{
    public sealed class TurnTimer : ITurnTimer
    {
        private readonly IServiceScopeFactory _sf;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _map = new();
        public TurnTimer(IServiceScopeFactory sf)
        {
            _sf = sf;
        }
        public bool Schedule(Guid gameId, Guid turnId, DateTime dueAtUTC, Func<Guid, Guid, Task> Broadcast)
        {
            var delay = dueAtUTC - DateTime.UtcNow;
            if(delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            var cts = new CancellationTokenSource();
            if(!_map.TryAdd(turnId, cts))
            {
                cts.Dispose();
                return false;
            }
            _ = RunTimerAsync(gameId, turnId, delay, Broadcast, cts);
            return true;
        }

        private async Task RunTimerAsync(Guid gameId, Guid turnId, TimeSpan delay, Func<Guid, Guid, Task> Broadcast, CancellationTokenSource cts)
        {
            try
            {
                await Task.Delay(delay, cts.Token);
                if(cts.IsCancellationRequested)
                    return;

                using var scope = _sf.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IGameService>();

                if (await svc.TimeoutAndAdvanceAsync(gameId, turnId, CancellationToken.None))
                    await Broadcast(gameId, turnId);
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
