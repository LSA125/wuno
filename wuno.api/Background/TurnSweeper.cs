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
        private readonly ILogger<TurnSweeper> _logger;

        public TurnSweeper(IServiceScopeFactory sf, IHubContext<GameHub> hub, ILogger<TurnSweeper> logger)
        {
            _sf = sf;
            _hub = hub;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _sf.CreateAsyncScope();
                    var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var _svc = scope.ServiceProvider.GetRequiredService<IGameService>();

                    _logger.LogInformation("TurnSweeper checking for overdue turns at: {time}", DateTimeOffset.Now);
                    var overdue = await _svc.FindOverdueAsync(_db, ct);
                    foreach (var (gameId, turnId) in overdue)
                    {
                        _logger.LogInformation("TurnSweeper advancing game {gameId} turn {turnId} at: {time}", gameId, turnId, DateTimeOffset.Now);
                        await _svc.TimeoutAndAdvanceAsync(_db, gameId, turnId, ct);
                        _logger.LogInformation("TurnSweeper broadcasting new game state for {gameId} at: {time}", gameId, DateTimeOffset.Now);
                        var state = await _svc.GetGameStateAsync(gameId, ct);
                        await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in TurnSweeper loop");
                }
                await Task.Delay(10000, ct);
            }
        }
    }
}
