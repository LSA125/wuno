using Microsoft.AspNetCore.SignalR;
using wuno.infrastructure;
using Wuno.Api.Hubs;
using Wuno.Application.Games;


namespace Wuno.Api.Background
{
    public class TurnSweeper : BackgroundService
    {
        private readonly IServiceScopeFactory _sf;
        private readonly IHubContext<GameHub> _hub;

        public TurnSweeper(IServiceScopeFactory sf, IHubContext<GameHub> hub)
        {
            _sf = sf;
            _hub = hub;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                //create scope to get new db context
                var scope = _sf.CreateScope();
                var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var _svc = scope.ServiceProvider.GetService<IGameService>();
                // find overdue turns (SQL DateDiff or computed EndAt)
                if(_svc is null) throw new Exception("GameService not available in TurnSweeper");
                var overdue = await _svc.FindOverdueAsync(_db, ct);
                foreach (var (gameId, turnId) in overdue)
                {
                    await _svc.TimeoutAndAdvanceAsync(_db, gameId, turnId, ct);   // application logic
                    var state = await _svc.GetGameStateAsync(gameId, ct);
                    await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
                }
                await Task.Delay(500, ct);
            }
        }
    }
}
