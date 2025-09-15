using Microsoft.AspNetCore.SignalR;
using wuno.infrastructure;
using Wuno.Api.Hubs;
using Wuno.Application.Games;


namespace Wuno.Api.Background
{
    public class TurnSweeper : BackgroundService
    {
        private readonly AppDbContext _db;
        private readonly IServiceScopeFactory _sf;
        private readonly IHubContext<GameHub> _hub;
        private readonly IGameService _svc;

        public TurnSweeper(AppDbContext db,IServiceScopeFactory sf, IHubContext<GameHub> hub, IGameService svc)
        {
            _db = db;
            _sf = sf;
            _hub = hub;
            _svc = svc;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // find overdue turns (SQL DateDiff or computed EndAt)
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
