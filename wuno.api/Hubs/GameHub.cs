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
        private readonly IGroupTracker _tracker;

        public GameHub(IGameService svc, IHubContext<GameHub> hub, IGroupTracker tracker)
        {
            _svc = svc;
            _hub = hub;
            _tracker = tracker;
        }

        public async Task ConnectToGame(string gameCode, CancellationToken ct)
        {
            Guid gameId = await _svc.GetGameId(gameCode, ct);
            _tracker.Add(Context.ConnectionId, $"game:{gameId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}", ct);
        }
        public async Task LeaveGame(Guid gameid, CancellationToken ct)
        {
            _tracker.Remove(Context.ConnectionId, $"game:{gameid}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game:{gameid}", ct);
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            _tracker.GetGroups(Context.ConnectionId).ToList().ForEach(async group =>
            {
                await Clients.Group(group).SendAsync("PlayerDisconnected", Context.ConnectionId);
            });
            _tracker.Clear(Context.ConnectionId);
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
