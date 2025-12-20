using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using wuno.domain;
using Wuno.Application.Games.Implementation;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Games.Util;

public sealed class TurnTimerTests
{
    [Fact]
    public async Task Schedule_and_execute_invokes_game_service_and_broadcast()
    {
        var calls = new List<(Guid gameId, Guid turnId)>();
        var broadcasted = new List<GameState>();
        var tcs = new TaskCompletionSource();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IGameService>(new StubGameService((g, t) =>
        {
            calls.Add((g, t));
            return Task.FromResult<GameState?>(new GameState(g, GameStatus.ACTIVE, 1, 1, 1, null, new(), new(Guid.NewGuid(), 0, null, DateTime.UtcNow, null), new(Guid.NewGuid(), 0, 1, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(10), 1, 0)));
        }));
        var provider = serviceCollection.BuildServiceProvider();

        var timer = new TurnTimer(provider.GetRequiredService<IServiceScopeFactory>());
        var gameId = Guid.NewGuid();
        var turnId = Guid.NewGuid();

        Assert.True(timer.Schedule(gameId, turnId, DateTime.UtcNow.AddMilliseconds(25), state =>
        {
            broadcasted.Add(state);
            tcs.TrySetResult();
            return Task.CompletedTask;
        }));

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Single(calls);
        Assert.Equal((gameId, turnId), calls[0]);
        Assert.Single(broadcasted);
    }

    [Fact]
    public async Task Cancel_prevents_callback_and_frees_entry()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IGameService>(new StubGameService((_, _) => Task.FromResult<GameState?>(null)));
        var provider = serviceCollection.BuildServiceProvider();
        var timer = new TurnTimer(provider.GetRequiredService<IServiceScopeFactory>());
        var turnId = Guid.NewGuid();
        var broadcasted = false;

        Assert.True(timer.Schedule(Guid.NewGuid(), turnId, DateTime.UtcNow.AddMilliseconds(50), _ =>
        {
            broadcasted = true;
            return Task.CompletedTask;
        }));

        timer.Cancel(turnId);
        await Task.Delay(100);

        Assert.False(broadcasted);
        Assert.True(timer.Schedule(Guid.NewGuid(), turnId, DateTime.UtcNow.AddMilliseconds(10), _ => Task.CompletedTask));
    }

    [Fact]
    public async Task Scheduling_same_turn_replaces_existing_timer()
    {
        var serviceCollection = new ServiceCollection();
        // Return a non-null GameState so the broadcast callback is actually invoked
        serviceCollection.AddSingleton<IGameService>(new StubGameService((g, _) => Task.FromResult<GameState?>(
            new GameState(g, GameStatus.ACTIVE, 1, 1, 1, null, new(), new(Guid.NewGuid(), 0, null, DateTime.UtcNow, null), new(Guid.NewGuid(), 0, 1, DateTime.UtcNow, DateTime.UtcNow.AddSeconds(10), 1, 0)))));
        var provider = serviceCollection.BuildServiceProvider();
        var timer = new TurnTimer(provider.GetRequiredService<IServiceScopeFactory>());
        var turnId = Guid.NewGuid();
        int callbacks = 0;

        // Schedule a timer far in the future - this should be replaced
        Assert.True(timer.Schedule(Guid.NewGuid(), turnId, DateTime.UtcNow.AddMinutes(1), _ =>
        {
            Interlocked.Increment(ref callbacks);
            return Task.CompletedTask;
        }));

        // Replace with one that fires soon - only this one should call back
        Assert.True(timer.Schedule(Guid.NewGuid(), turnId, DateTime.UtcNow.AddMilliseconds(30), _ =>
        {
            Interlocked.Increment(ref callbacks);
            return Task.CompletedTask;
        }));

        await Task.Delay(200);

        Assert.Equal(1, callbacks);
    }

    private sealed class StubGameService : IGameService
    {
        private readonly Func<Guid, Guid, Task<GameState?>> _timeout;

        public StubGameService(Func<Guid, Guid, Task<GameState?>> timeout)
        {
            _timeout = timeout;
        }

        public Task<bool> AreAllPlayersReadyAsync(Guid gameId, CancellationToken ct) => Task.FromResult(false);

        public Task ReadyAsync(Guid gameId, int seat, bool isReady, CancellationToken ct) => Task.CompletedTask;

        public Task<Guid> GetGameId(string code, CancellationToken ct) => Task.FromResult(Guid.Empty);

        public Task<GameCodeResponse> GetUserActiveGameCodeAsync(Guid userId, CancellationToken ct) => Task.FromResult(new GameCodeResponse(false, null, null));

        public Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct) => Task.FromResult(new NewGameResponse("", 0, 0));

        public Task<JoinGameResponse> JoinGameAsync(Guid gameId, Guid userId, CancellationToken ct) => Task.FromResult(new JoinGameResponse(Guid.Empty, new(Guid.Empty, GameStatus.WAITING, 0, 1, 1, "", new(), new(Guid.Empty, 0, null, DateTime.UtcNow, null), new(Guid.Empty, 0, 0, DateTime.UtcNow, DateTime.UtcNow, 1, 0))));

        public Task<(Guid gameId, List<PlayerState> players)> DisconnectProtocolAsync(Guid playerId, CancellationToken ct) => Task.FromResult((Guid.Empty, new List<PlayerState>()));

        public Task<Guid?> LeaveGameAsync(Guid userId, CancellationToken ct) => Task.FromResult<Guid?>(null);

        public Task<bool> MarkMatchAsStartedAsync(Guid gameId, CancellationToken ct) => Task.FromResult(false);

        public Task<TurnState> StartMatchAsync(Guid gameId, CancellationToken ct) => Task.FromResult(new TurnState(Guid.Empty, 0, 0, DateTime.UtcNow, DateTime.UtcNow, 0, 0));

        public Task<ProcessTurnOutcome> ProcessTurnAsync(Guid gameId, Guid roundId, Guid turnId, Guid playerId, int seat, string word, CancellationToken ct) => Task.FromResult(new ProcessTurnOutcome(false, null, null, null));
        public Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct) => Task.FromResult(new List<PlayerState>());

        public Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct) => Task.FromResult(new GameState(Guid.Empty, GameStatus.WAITING, 0, 1, 1, null, new(), new(Guid.Empty, 0, null, null, null), new(Guid.Empty, 0, 0, DateTime.UtcNow, DateTime.UtcNow, 0, 0)));
        public Task<int> GetCurrentSeatAsync(Guid gameId, CancellationToken ct) => Task.FromResult(0);
        public Task<List<TurnHistoryState>> GetRecentWordHistoryAsync(Guid gameId, CancellationToken ct) => Task.FromResult(new List<TurnHistoryState>());
        public Task<GameState?> TimeoutAndAdvanceAsync(Guid gameId, Guid turnId, CancellationToken ct) => _timeout(gameId, turnId);

        public Task ForceEndGame(Guid gameId, CancellationToken ct) => Task.CompletedTask;
        public Task<MatchmakingResponse> FindOrCreatePublicGameAsync(CancellationToken ct)
            => Task.FromResult(new MatchmakingResponse(true, "TEST", true));
    }
}