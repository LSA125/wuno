using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Wuno.Api.Hubs;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Games.Util;
using Wuno.Testing.SignalR;
using Wuno.Api.Services;

namespace Wuno.Api.Tests
{
    public sealed class GameHubTests
    {

        [Fact]
        public async Task SubmitWord_CancelsTimeout()
        {
            var tracker = new InMemoryGroupTracker();
            var turnTimer = new Testing.Fixtures.FakeTurnTimer();
            var clients = new TestHubCallerClients();
            var hubContext = new TestHubContext(clients);
            var initialTurn = Guid.NewGuid();
            var nextTurn = Guid.NewGuid();
            var service = FakeGameService.ForTurn(new ProcessTurnOutcome(true, null, new GameState(Guid.NewGuid(), wuno.domain.GameStatus.ACTIVE, 0, 1, 2, null,
                [new PlayerState(Guid.NewGuid(), null, 0, true, true, "p1", null, 0, null, 30)],
                new RoundState(Guid.NewGuid(), 0, null, DateTime.UtcNow, null),
                new TurnState(nextTurn, 1, 0, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(5), 1, 0)), null));
            var hub = CreateHub(service, tracker, turnTimer, hubContext, clients, userId: Guid.NewGuid());
            tracker.Add(hub.Context.ConnectionId, new PlayerSession(service.LastGameId, Guid.NewGuid(), 0, service.TestUser));
            turnTimer.Schedule(service.LastGameId, initialTurn, DateTime.UtcNow.AddSeconds(1), _ => Task.CompletedTask);

            await hub.SubmitWord(service.LastGameId, Guid.NewGuid(), initialTurn, "word");

            Assert.DoesNotContain(turnTimer.Scheduled.Keys, k => k == initialTurn);
            Assert.Contains(turnTimer.Scheduled.Keys, k => k == nextTurn);
        }

        [Fact]
        public async Task SubmitWord_RejectedOutcomeNotifiesCallerOnly()
        {
            var tracker = new InMemoryGroupTracker();
            var turnTimer = new Testing.Fixtures.FakeTurnTimer();
            var clients = new TestHubCallerClients();
            var hubContext = new TestHubContext(clients);
            var service = FakeGameService.ForTurn(new ProcessTurnOutcome(false, "bad", null, null));
            var hub = CreateHub(service, tracker, turnTimer, hubContext, clients, userId: Guid.NewGuid());
            tracker.Add(hub.Context.ConnectionId, new PlayerSession(service.LastGameId, Guid.NewGuid(), 0, service.TestUser));

            await hub.SubmitWord(service.LastGameId, Guid.NewGuid(), Guid.NewGuid(), "bad");

            Assert.Contains(clients.CallerProxy.Invocations, i => i.Method == "WordRejected");
            Assert.Empty(clients.GetProxyForTarget($"group:{service.LastGameId}").Invocations);
        }

        private static GameHub CreateHub(FakeGameService service, IGroupTracker tracker, Testing.Fixtures.FakeTurnTimer timer, IHubContext<GameHub> hubContext, TestHubCallerClients clients, Guid userId)
        {
            var hub = new GameHub(service, hubContext, tracker, new AllowTypingGate(), timer, new FakeTokenService())
            {
                Context = new TestHubCallerContext("conn-1", new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                ], "cookie")), userId.ToString()),
                Clients = clients,
                Groups = new TestGroupManager()
            };
            return hub;
        }

        private sealed class AllowTypingGate : ITypingGate
        {
            public bool tryAllow(string key, TimeSpan interval) => true;
        }
        
        private sealed class FakeTokenService : ITokenService
        {
            public string GenerateToken(Guid userId, string? name = null, bool isRegistered = false)
                => $"fake-token-{userId}";
            public ClaimsPrincipal? ValidateToken(string token) => null;
            public Guid? GetUserIdFromToken(string token) => null;
        }

        private sealed class InMemoryGroupTracker : IGroupTracker
        {
            private readonly Dictionary<string, PlayerSession> _sessions = [];
            public Guid TestUser { get; } = Guid.NewGuid();

            public void Add(string connectionId, PlayerSession session) => _sessions[connectionId] = session;

            public bool Remove(string connectionId, out PlayerSession session, out bool isLast)
            {
                isLast = _sessions.Count <= 1;
                var removed = _sessions.Remove(connectionId, out session!);
                return removed;
            }

            public bool TryGet(string connectionId, out PlayerSession session) => _sessions.TryGetValue(connectionId, out session!);
        }

        private sealed class FakeGameService : IGameService
        {
            public Guid TestUser { get; } = Guid.NewGuid();
            public Guid LastGameId { get; private set; } = Guid.NewGuid();
            private readonly JoinGameResponse? _join;
            private readonly ProcessTurnOutcome? _turnOutcome;

            private FakeGameService(JoinGameResponse join, ProcessTurnOutcome outcome)
            {
                _join = join;
                _turnOutcome = outcome;
            }

            public static FakeGameService ForJoin(JoinGameResponse join)
            {
                return new FakeGameService(join, new ProcessTurnOutcome(false, "unused", null, null));
            }

            public static FakeGameService ForTurn(ProcessTurnOutcome outcome)
            {
                var state = outcome.State ?? new GameState(Guid.NewGuid(), wuno.domain.GameStatus.ACTIVE, 0, 1, 2, null, new List<PlayerState>(), new RoundState(Guid.NewGuid(), 0, null, DateTime.UtcNow, null),
                    new TurnState(Guid.NewGuid(), 0, 0, DateTime.UtcNow, DateTime.UtcNow, 1, 0));
                var join = new JoinGameResponse(Guid.NewGuid(), state);
                return new FakeGameService(join, outcome);
            }

            public Task<Guid> GetGameId(string code, CancellationToken ct)
            {
                LastGameId = _join!.State.GameId;
                return Task.FromResult(LastGameId);
            }

            public Task<JoinGameResponse> JoinGameAsync(Guid gameId, Guid userId, CancellationToken ct)
            {
                return Task.FromResult(_join!);
            }

            public Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct)
            {
                return Task.FromResult(_join!.State.Players);
            }

            public Task<ProcessTurnOutcome> ProcessTurnAsync(Guid gameId, Guid roundId, Guid turnId, Guid playerId, int seat, string word, CancellationToken ct)
            {
                return Task.FromResult(_turnOutcome!);
            }
            public Task<List<TurnHistoryState>> GetRecentWordHistoryAsync(Guid gameId, CancellationToken ct)
            {
                return Task.FromResult(new List<TurnHistoryState>());
            }

            public Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct)
            {
                return Task.FromResult(_join!.State);
            }

            public Task<bool> AreAllPlayersReadyAsync(Guid gameId, CancellationToken ct) => Task.FromResult(true);
            public Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct) => Task.FromResult(new NewGameResponse("", 0, 0));
            public Task<bool> MarkMatchAsStartedAsync(Guid gameId, CancellationToken ct) => Task.FromResult(true);
            public Task<TurnState> StartMatchAsync(Guid gameId, CancellationToken ct) => Task.FromResult(_join!.State.CurrentTurn);
            public Task<int> GetCurrentSeatAsync(Guid gameId, CancellationToken ct) => Task.FromResult(_join!.State.CurrentTurn.Seat);
            public Task<GameCodeResponse> GetUserActiveGameCodeAsync(Guid userId, CancellationToken ct) => Task.FromResult(new GameCodeResponse(true, true, ""));
            public Task<(Guid gameId, List<PlayerState> players)> DisconnectProtocolAsync(Guid playerId, CancellationToken ct) => Task.FromResult((Guid.NewGuid(), new List<PlayerState>()));
            public Task<LeaveGameResult> LeaveGameAsync(Guid userId, CancellationToken ct) => Task.FromResult(new LeaveGameResult(Guid.NewGuid(), false, new List<PlayerState>(), null));
            public Task ReadyAsync(Guid gameId, int seat, bool isReady, CancellationToken ct) => Task.CompletedTask;
            public Task ForceEndGame(Guid gameId, CancellationToken ct) => Task.CompletedTask;
            public Task<GameState?> TimeoutAndAdvanceAsync(Guid gameId, Guid turnId, CancellationToken ct) => Task.FromResult<GameState?>(_join!.State);
            public Task<MatchmakingResponse> FindOrCreatePublicGameAsync(CancellationToken ct)
                => Task.FromResult(new MatchmakingResponse(true, "TEST", true));
        }

        private sealed class TestHubContext(IHubClients clients) : IHubContext<GameHub>
        {
            public IHubClients Clients { get; } = clients;
            public IGroupManager Groups { get; } = new TestGroupManager();
        }

        private sealed class TestGroupManager : IGroupManager
        {
            public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
    }
}