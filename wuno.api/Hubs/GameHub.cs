using Microsoft.AspNetCore.SignalR;
using wuno.infrastructure;
using Wuno.Application.Games;

namespace Wuno.Api.Hubs
{
    public class GameHub : Hub
    {
        private readonly IGameService _svc;
        private readonly IHubContext<GameHub> _hub;
        private readonly IGroupTracker _tracker;
        private readonly ITypingGate _typingGate;
        private readonly ITurnTimer _turnTimer;
        public GameHub(IGameService svc, IHubContext<GameHub> hub, IGroupTracker tracker, ITypingGate typingGate, ITurnTimer turnTimer)
        {
            _svc = svc;
            _hub = hub;
            _tracker = tracker;
            _typingGate = typingGate;
            _turnTimer = turnTimer;
        }
        private async Task<(Guid gameId, Guid playerId, int seat)> GetCallerAsync(CancellationToken ct)
        {
            // playerId from your connection tracker
            var playerId = _tracker.Get(Context.ConnectionId);

            // Look up game & seat server-side
            using var scope = Context.GetHttpContext()!.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var p = await db.Players
                .Where(x => x.Id == playerId)
                .Select(x => new { x.GameId, x.Seat })
                .FirstOrDefaultAsync(ct) ?? throw new HubException("Player not found.");

            return (p.GameId, playerId, p.Seat);
        }
        public async Task ConnectToGame(string gameCode, Guid userId)
        {
            var ct = Context.ConnectionAborted;
            try
            {
                Guid gameId = await _svc.GetGameId(gameCode, ct);
                var res = await _svc.JoinGameAsync(gameId, userId, ct);
                _tracker.Add(Context.ConnectionId, res.PlayerId);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}", ct);
                await Clients.Caller.SendAsync("ConnectedToGame", res, ct);
                await Clients.Group($"game:{gameId}").SendAsync("PlayersUpdated", res.State.Players, ct);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ConnectionFailed", ex.Message, ct);
                return;
            }
        }
        public async Task LeaveGame(Guid gameId, Guid playerId)
        {
            var ct = Context.ConnectionAborted;
            _tracker.Remove(Context.ConnectionId, gameId);
            await _svc.LeaveGameAsync(gameId, playerId, ct);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game:{gameId}", ct);
            var players = await _svc.GetPlayersAsync(gameId, ct);
            await Clients.Group($"game:{gameId}").SendAsync("PlayersUpdated", players, ct);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var tasks = _tracker.Get(Context.ConnectionId).Select(async playerId =>
            {
                var res = await _svc.DisconnectProtocolAsync(playerId, CancellationToken.None);
                await Clients.Group($"game:{res.gameId}").SendAsync("PlayerDisconnected", res.players);
            });

            await Task.WhenAll(tasks);
            _tracker.Clear(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
        public async Task Ready(Guid gameId, int seat, bool isReady)
        {
            var ct = Context.ConnectionAborted;
            await _svc.ReadyAsync(gameId, seat, isReady, ct);

            if (await _svc.AreAllPlayersReadyAsync(gameId, ct))
            {
                try
                {
                    var timer = 3000;
                    await _hub.Clients.Group($"game:{gameId}").SendAsync("AllPlayersReady", timer, ct);
                    Task.Delay(timer, ct).Wait(ct);
                    var turn = await _svc.StartMatchAsync(gameId, ct);
                    var state = await _svc.GetGameStateAsync(gameId, ct);
                    await _hub.Clients.Group($"game:{gameId}").SendAsync("MatchStarted", state, ct);
                    _turnTimer.Schedule(gameId, turn.TurnId, turn.DueAt, BroadcastAfterTimeout);
                }
                catch (Exception ex)
                {
                    await _hub.Clients.Group($"game:{gameId}").SendAsync("Error", ex.Message, ct);
                }
            }
            else
            {
                var players = await _svc.GetPlayersAsync(gameId, ct);
                await _hub.Clients.Group($"game:{gameId}").SendAsync("PlayersUpdated", players, ct);
            }
        }
        public async Task SubmitWord(Guid gameId, Guid roundId, Guid turnId, int seat, string word)
        {
            var ct = Context.ConnectionAborted;
            var res = await _svc.SubmitWordAsync(gameId, roundId, turnId, new SubmitWordRequest(seat, word), ct);
            if (!res.Ok)
            {
                await Clients.Caller.SendAsync("WordRejected", res.Reason, ct);
                return;
            }

            _turnTimer.Cancel(turnId);

            if (await _svc.IsRoundEndAsync(gameId, ct))
            {
                await _svc.EndRoundAsync(gameId, roundId, ct);
                var afterRound = await _svc.GetGameStateAsync(gameId, ct);
                await _hub.Clients.Group($"game:{gameId}").SendAsync("RoundEnded", afterRound, ct);

                await Task.Delay(3000, ct);
                if (await _svc.IsMatchEndAsync(gameId, ct))
                {
                    await _svc.EndMatchAsync(gameId, ct);
                    var ended = await _svc.GetGameStateAsync(gameId, ct);
                    await _hub.Clients.Group($"game:{gameId}").SendAsync("MatchEnded", ended, ct);
                    return;
                }

                var turn = await _svc.StartRoundAsync(gameId, ct);
                var afterNewRound = await _svc.GetGameStateAsync(gameId, ct);
                await _hub.Clients.Group($"game:{gameId}").SendAsync("NewRoundStarted", afterNewRound, ct);
                _turnTimer.Schedule(gameId, turn.TurnId, turn.DueAt, BroadcastAfterTimeout);
            }

            var state = await _svc.GetGameStateAsync(gameId, ct);
            await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
        }
        public async Task WordChanged(string word)
        {
            var ct = Context.ConnectionAborted;
            //check if 50ms have passed since last change
            if (!_typingGate.tryAllow(Context.ConnectionId, TimeSpan.FromMilliseconds(100)))
            {
                return;
            }
            await Clients.OthersInGroup(Context.ConnectionId).SendAsync("WordChanged", word, ct);
        }

        private async Task BroadcastAfterTimeout(Guid gameId, Guid turnId)
        {
            var state = await _svc.GetGameStateAsync(gameId, CancellationToken.None);
            await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state);
        }
    }
}
