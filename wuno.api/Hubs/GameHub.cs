using Microsoft.AspNetCore.SignalR;
using System.Runtime.InteropServices;
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

        public async Task ConnectToGame(string gameCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameCode}");
        }

        public async Task SubmitWord(string gameCode, int seat, string word, CancellationToken ct)
        {
            var ok = await _svc.SubmitWordAsync(Guid.Parse(gameCode), new SubmitWordRequest(seat, word), ct);
            // Regardless of ok/err, send the fresh state so clients stay in sync:
            var state = await _svc.GetGameStateAsync(Guid.Parse(gameCode), ct);
            await _hub.Clients.Group($"game:{gameCode}").SendAsync("GameUpdated", state, ct);
        }
    }
}
