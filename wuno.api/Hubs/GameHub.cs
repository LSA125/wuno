using Microsoft.AspNetCore.SignalR;
using System.Runtime.InteropServices;
using wuno.domain;
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
            Guid gameId = await _svc.GetGameId(gameCode, CancellationToken.None);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}");
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            // Note: We don't know what groups the connection was in, so we can't remove it from specific groups.
            // SignalR handles this automatically when the connection is closed, so no action is needed here.
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Ready(Guid gameId, int seat, bool isReady, CancellationToken ct)
        {
            await _svc.ReadyAsync(gameId, seat, isReady, ct);
            List<Player> players = await _svc.GetPlayers(gameId, ct);
            await _hub.Clients.Group($"game:{gameId}").SendAsync("PlayerReady", players, ct);
        }

        public async Task SubmitWord(Guid gameId, int seat, string word, CancellationToken ct)
        {
            var ok = await _svc.SubmitWordAsync(gameId, new SubmitWordRequest(seat, word), ct);
            // Regardless of ok/err, send the fresh state so clients stay in sync:
            var state = await _svc.GetGameStateAsync(gameId, ct);
            await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
        }
    }
}
