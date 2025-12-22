using System;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Games.Util;
using Wuno.Api.Services;

namespace Wuno.Api.Hubs
{
    public class GameHub : Hub
    {
        private readonly IGameService _svc;
        private readonly IHubContext<GameHub> _hub;
        private readonly IGroupTracker _tracker;
        private readonly ITypingGate _typingGate;
        private readonly ITurnTimer _turnTimer;
        private readonly ITokenService _tokenService;
        private static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        public GameHub(IGameService svc, IHubContext<GameHub> hub, IGroupTracker tracker, ITypingGate typingGate, ITurnTimer turnTimer, ITokenService tokenService)
        {
            _svc = svc;
            _hub = hub;
            _tracker = tracker;
            _typingGate = typingGate;
            _turnTimer = turnTimer;
            _tokenService = tokenService;
        }
        private PlayerSession RequireSession()
        {
            if (!_tracker.TryGet(Context.ConnectionId, out var ps))
                throw new HubException("Not joined.");
            return ps;
        }
        
        /// <summary>
        /// Gets the user ID from cookie claims or access token in query string.
        /// </summary>
        private Guid? GetUserId()
        {
            // 1. Try cookie-based auth (works on desktop, sometimes on mobile)
            var sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(sub, out var userId))
                return userId;
            
            // 2. Try access token from query string (mobile fallback)
            var httpContext = Context.GetHttpContext();
            var accessToken = httpContext?.Request.Query["access_token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(accessToken))
            {
                return _tokenService.GetUserIdFromToken(accessToken);
            }
            
            return null;
        }
        
        public async Task ConnectToGame(string gameCode)
        {
            var ct = Context.ConnectionAborted;
            var userId = GetUserId();
            if (!userId.HasValue)
            {
                await Clients.Caller.SendAsync("ConnectionFailed", "Please pick a guest name first.");
                return;
            }
            try
            {
                Guid gameId = await _svc.GetGameId(gameCode, ct);
                JoinGameResponse res = await _svc.JoinGameAsync(gameId, userId.Value, ct);
                int seat = res.State.Players.First(p => p.PlayerId == res.PlayerId).Seat;
                PlayerSession ps = new(gameId, res.PlayerId, seat, userId.Value);

                _tracker.Add(Context.ConnectionId, ps);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{gameId}", ct);

                await Clients.Caller.SendAsync("ConnectedToGame", res, ct);
                var history = await _svc.GetRecentWordHistoryAsync(gameId, ct);
                await Clients.Caller.SendAsync("RecentWordHistory", history, ct);
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
            var result = await _svc.LeaveGameAsync(ps.UserId, ct);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game:{gameId}", ct);
            
            if (result.GameEnded)
            {
                // Game ended - notify remaining players with MatchEnded
                await Clients.Group($"game:{gameId}").SendAsync("MatchEnded", result.FinalState, ct);
            }
            else
            {
                // Game continues - just update player list
                await Clients.Group($"game:{gameId}").SendAsync("PlayersUpdated", result.RemainingPlayers, ct);
            }
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Try to remove from tracker - if not found, nothing to clean up
            if (!_tracker.Remove(Context.ConnectionId, out var ps, out _))
            {
                await base.OnDisconnectedAsync(exception);
                return;
            }

            var ct = Context.ConnectionAborted;
            try
            {
                var res = await _svc.DisconnectProtocolAsync(ps.PlayerId, ct);
                // Skip broadcast if player already left (beacon already handled it)
                if (res.gameId != Guid.Empty)
                {
                    await Clients.Group($"game:{ps.GameId}").SendAsync("PlayersUpdated", res.players, ct);
                }
            }
            catch
            {
                // Best effort - don't fail the disconnect
            }
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
                    await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", state, ct);
                    _turnTimer.Schedule(gameId, turn.TurnId, EnsureUtc(turn.DueAt), BroadcastAfterTimeout);
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

            ProcessTurnOutcome outcome = await _svc.ProcessTurnAsync(gameId, roundId, turnId, ps.PlayerId, ps.Seat, word, ct);
            if (!outcome.Ok)
            {
                await Clients.Caller.SendAsync("WordRejected", outcome.Reason, ct);
                return;
            }
            _turnTimer.Cancel(turnId);
            
            // Only schedule next turn timer if the game is still active
            if (outcome.State!.Status != wuno.domain.GameStatus.FINISHED)
            {
                _turnTimer.Schedule(gameId, outcome.State.CurrentTurn.TurnId, EnsureUtc(outcome.State.CurrentTurn.DueAt), BroadcastAfterTimeout);
            }
            
            if (outcome.CompletedTurn is not null)
            {
                await _hub.Clients.Group($"game:{gameId}").SendAsync("WordHistoryAppended", outcome.CompletedTurn, ct);
            }
            await _hub.Clients.Group($"game:{gameId}").SendAsync("GameUpdated", outcome.State, ct);
        }
        public async Task RequestRecentWordHistory(Guid gameId)
        {
            var ct = Context.ConnectionAborted;
            var ps = RequireSession();
            if (ps.GameId != gameId) throw new HubException("Wrong game context.");
            var history = await _svc.GetRecentWordHistoryAsync(gameId, ct);
            await Clients.Caller.SendAsync("RecentWordHistory", history, ct);
        }
        public async Task WordChanged(string word)
        {
            var ct = Context.ConnectionAborted;
            var ps = RequireSession();
            if (ps.Seat != await _svc.GetCurrentSeatAsync(ps.GameId, ct))
            {
                return;
            }
            if (!_typingGate.tryAllow(Context.ConnectionId, TimeSpan.FromMilliseconds(50)))
            {
                return;
            }
            await Clients.OthersInGroup($"game:{ps.GameId}").SendAsync("WordChanged", word, ct);
        }

        private async Task BroadcastAfterTimeout(GameState state)
        {
            // Schedule next timer FIRST to ensure chain never breaks, even if broadcast fails
            if (state.Status != wuno.domain.GameStatus.FINISHED && state.CurrentTurn != null)
            {
                _turnTimer.Schedule(state.GameId, state.CurrentTurn.TurnId, EnsureUtc(state.CurrentTurn.DueAt), BroadcastAfterTimeout);
            }
            
            // Now attempt broadcast - if this fails, the timer chain is already scheduled
            try
            {
                await _hub.Clients.Group($"game:{state.GameId}").SendAsync("GameUpdated", state);
            }
            catch (Exception)
            {
                // Broadcast failed but timer is already scheduled - game state is saved in DB
                // Clients will get updated state on next poll or turn
            }
        }
    }
}