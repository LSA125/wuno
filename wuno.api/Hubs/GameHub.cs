using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Wuno.Application.Games;
using Wuno.Application.Games.Inheritance;

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
        private PlayerSession RequireSession()
        {
            if (!_tracker.TryGet(Context.ConnectionId, out var ps))
                throw new HubException("Not joined.");
            return ps;
        }
        public async Task ConnectToGame(string gameCode)
        {
            var ct = Context.ConnectionAborted;
            var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out var userId))
            {
                await Clients.Caller.SendAsync("ConnectionFailed", "Please pick a guest name first.");
                return;
            }
            try
            {
                Guid gameId = await _svc.GetGameId(gameCode, ct);
                JoinGameResponse res = await _svc.JoinGameAsync(gameId, userId, ct);
                int seat = res.State.Players.First(p => p.PlayerId == res.PlayerId).Seat;
                PlayerSession ps = new(gameId, res.PlayerId, seat, userId);

                _tracker.Add(Context.ConnectionId, ps);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}", ct);

                await Clients.Caller.SendAsync("ConnectedToGame", res, ct);
                await Clients.Group($"game:{gameId}").SendAsync("PlayersUpdated", res.State.Players, ct);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ConnectionFailed", ex.Message, ct);
            }
        }
        public async Task LeaveGame(Guid gameId)
        {
            var ct = Context.ConnectionAborted;
            var ps = RequireSession();
            var playerId = ps.PlayerId;
            await _svc.LeaveGameAsync(ps.UserId, ct);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game:{gameId}", ct);
            var players = await _svc.GetPlayersAsync(gameId, ct);
            if(players.Count == 0)
            {
                await _svc.ForceEndGame(gameId, ct);
                return;
            }
            await Clients.Group($"game:{gameId}").SendAsync("PlayersUpdated", players, ct);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var ps = RequireSession();
            var ct = Context.ConnectionAborted;
            var res = await _svc.DisconnectProtocolAsync(ps.PlayerId, ct);
            await Clients.Group($"game:{ps.GameId}").SendAsync("PlayersUpdated", res.players, ct);
            await base.OnDisconnectedAsync(exception);
        }
        public async Task Ready(Guid gameId, bool isReady)
        {
            var ct = Context.ConnectionAborted;
            var ps = RequireSession();
            await _svc.ReadyAsync(ps.GameId, ps.Seat, isReady, ct);

            if (await _svc.AreAllPlayersReadyAsync(gameId, ct))
            {
                try
                {
                    if (!await _svc.MarkMatchAsStartedAsync(gameId, ct)) return;
                    var timer = 3000;
                    await _hub.Clients.Group($"game:{gameId}").SendAsync("AllPlayersReady", timer, ct);
                    await Task.Delay(timer, ct);
                    TurnState turn = await _svc.StartMatchAsync(gameId, ct);
                    var state = await _svc.GetGameStateAsync(gameId, ct);
                    await _hub.Clients.Group($"game:{gameId}").SendAsync("MatchStarted", state, ct);
                    await BroadcastTurnStartedAsync(gameId, state.CurrentTurn, ct);
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
        public async Task SubmitWord(Guid gameId, Guid roundId, Guid turnId, string word)
        {
            var ct = Context.ConnectionAborted;
            var ps = RequireSession();
            SubmitWordResponse res = await _svc.SubmitWordAsync(gameId, roundId, turnId, new SubmitWordRequest(ps.Seat, word), ct);
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
            }

            var state = await _svc.GetGameStateAsync(gameId, ct);
            await BroadcastTurnStartedAsync(gameId, state.CurrentTurn, ct);

            await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
        }
        public async Task WordChanged(string word)
        {
            var ct = Context.ConnectionAborted;
            var ps = RequireSession();
            if (ps.Seat != await _svc.GetCurrentSeatAsync(ps.GameId, ct))
            {
                return;
            }
            if (!_typingGate.tryAllow(Context.ConnectionId, TimeSpan.FromMilliseconds(100)))
            {
                return;
            }
            await Clients.OthersInGroup($"game:{ps.GameId}").SendAsync("WordChanged", word, ct);
        }

        private async Task BroadcastAfterTimeout(Guid gameId, Guid turnId)
        {
            var state = await _svc.GetGameStateAsync(gameId, CancellationToken.None);
            await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state);
            await BroadcastTurnStartedAsync(gameId, state.CurrentTurn);
        }
        private Task BroadcastTurnStartedAsync(Guid gameId, TurnState? turn, CancellationToken ct = default)
        {
            if (turn is null) return Task.CompletedTask;
            return _hub.Clients.Group($"game:{gameId}").SendAsync("TurnStarted", turn, ct);
        }
    }
}