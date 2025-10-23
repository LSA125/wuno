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
            _tracker.Add(Context.ConnectionId, gameId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}", ct);
        }
        public async Task LeaveGame(Guid gameid, CancellationToken ct)
        {
            _tracker.Remove(Context.ConnectionId, gameid);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game:{gameid}", ct);
        }
        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            _tracker.GetGroups(Context.ConnectionId).ToList().ForEach(async group =>
            {
                List<PlayerState> players = await _svc.GetPlayersAsync(group, CancellationToken.None);
                await Clients.Group($"game:{group}").SendAsync("PlayerDisconnected", Context.ConnectionId);
            });
            _tracker.Clear(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        public async Task Ready(Guid gameId, int seat, bool isReady, CancellationToken ct)
        {
            await _svc.ReadyAsync(gameId, seat, isReady, ct);
            if (_svc.AreAllPlayersReadyAsync(gameId, ct).Result)
            {
                TurnState turn = await _svc.StartMatchAsync(gameId, ct);
                await _hub.Clients.Group($"game:{gameId}").SendAsync("MatchStarted", _svc.GetGameStateAsync(gameId, ct),ct);
                var _ = Task.Delay(turn.DurationSec * 1000, ct).ContinueWith(async _ =>
                {
                    if (await _svc.TimeoutAndAdvanceAsync(gameId, turn.TurnId, ct))
                    {
                        var state = await _svc.GetGameStateAsync(gameId, ct);
                        await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
                    }
                }, ct);
            }
            else
            {
                await _hub.Clients.Group($"game:{gameId}").SendAsync("PlayerState", _svc.GetPlayersAsync(gameId,ct), ct);
            }
        }
        public async Task SubmitWord(Guid gameId, int seat, string word, CancellationToken ct)
        {
            SubmitWordResponse res = await _svc.SubmitWordAsync(gameId, new SubmitWordRequest(seat, word), ct);
            if (res.Ok)
            {
                if (_svc.IsRoundEndAsync(gameId).Result)
                var state = await _svc.GetGameStateAsync(gameId, ct);
                await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
            }
            else
            {
                await Clients.Caller.SendAsync("WordRejected", res.Reason, ct);
            }
        }
        public async Task WordChanged(string word, CancellationToken ct)
        {
            await Clients.Caller.SendAsync("WordChanged", word, ct);
        }
    }
}
