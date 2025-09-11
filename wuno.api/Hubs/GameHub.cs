using Microsoft.AspNetCore.SignalR;
using Wuno.Application.Games;

namespace Wuno.Api.Hubs
{
    public class GameHub : Hub
    {
        private readonly IGameService _svc;
        private readonly IHubContext<GameHub> _hub;

        public GameHub(IGameService svc, IHubContext<GameHub> hub)
        {
            _svc = svc;
            _hub = hub;
        }

        public Task JoinGame(string gameId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}");
        }

        public async Task SubmitWord(string gameId, int seat, string word, CancellationToken ct)
        {
            var ok = await _svc.SubmitWordAsync(Guid.Parse(gameId), new SubmitWordRequest(seat, word), ct);
            // Regardless of ok/err, send the fresh state so clients stay in sync:
            var state = await _svc.GetGameStateAsync(Guid.Parse(gameId), ct);
            await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
        }
    }
}
