using Microsoft.AspNetCore.SignalR;
using Wuno.Api.Hubs;
using Wuno.Application.Games;

namespace Wuno.Api.Background
{
    public class TurnSweeper : BackgroundService
    {
        private readonly IServiceScopeFactory _sf;
        private readonly IHubContext<GameHub> _hub;
        private readonly IGameService _svc;

        public TurnSweeper(IServiceScopeFactory sf, IHubContext<GameHub> hub, IGameService svc)
        {
            _sf = sf;
            _hub = hub;
            _svc = svc;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // find overdue turns (SQL DateDiff or computed EndAt)
                var overdue = await FindOverdueAsync(ct);
                foreach (var (gameId, turnId) in overdue)
                {
                    await _svc.TimeoutAndAdvanceAsync(gameId, turnId, ct);   // application logic
                    var state = await _svc.GetGameStateAsync(gameId, ct);
                    await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
                }
                await Task.Delay(500, ct);
            }
        }
    }
}
