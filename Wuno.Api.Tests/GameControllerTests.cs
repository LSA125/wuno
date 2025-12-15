using Microsoft.AspNetCore.Mvc;
using Wuno.Api.Controllers;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Games.Util;
using Wuno.Application.Users;

public sealed class GamesControllerTests
{
    [Fact]
    public async Task Get_ReturnsNotFoundForUnknownGame()
    {
        var svc = new FakeGameService { GameState = null };
        var controller = new GamesController(svc);

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ActiveForCurrent_ReturnsEmptyWhenNoIdentity()
    {
        var svc = new FakeGameService { GameCodeResponse = new GameCodeResponse(true, true, "ABC") };
        var controller = new GamesController(svc);

        var result = await controller.ActiveForCurrent(new MissingUserResolver(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GameCodeResponse>(ok.Value);
        Assert.False(payload.Ok);
        Assert.Null(payload.GameCode);
    }

    [Fact]
    public async Task NewGame_UsesServiceResponse()
    {
        var response = new NewGameResponse("ABCD", 4, 3);
        var svc = new FakeGameService { NewGameResponse = response };
        var controller = new GamesController(svc);

        var result = await controller.New(new NewGameRequest(4, 3), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    private sealed class FakeGameService : IGameService
    {
        public GameState? GameState { get; set; }
        public GameCodeResponse? GameCodeResponse { get; set; }
        public NewGameResponse? NewGameResponse { get; set; }

        public Task<bool> AreAllPlayersReadyAsync(Guid gameId, CancellationToken ct) => Task.FromResult(false);
        public Task<(Guid gameId, List<PlayerState> players)> DisconnectProtocolAsync(Guid playerId, CancellationToken ct)
            => Task.FromResult((Guid.Empty, new List<PlayerState>()));
        public Task<GameState?> TimeoutAndAdvanceAsync(Guid gameId, Guid turnId, CancellationToken ct)
            => Task.FromResult<GameState?>(null);
        public Task ForceEndGame(Guid gameId, CancellationToken ct) => Task.CompletedTask;
        public Task<Guid> GetGameId(string code, CancellationToken ct) => Task.FromResult(Guid.NewGuid());
        public Task<int> GetCurrentSeatAsync(Guid gameId, CancellationToken ct) => Task.FromResult(0);
        public Task<GameCodeResponse> GetUserActiveGameCodeAsync(Guid userId, CancellationToken ct)
            => Task.FromResult(GameCodeResponse ?? new GameCodeResponse(false, null, null));
        public Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct) => Task.FromResult(new List<PlayerState>());
        public Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct)
            => GameState is null ? Task.FromResult<GameState>(null!) : Task.FromResult(GameState);
        public Task<JoinGameResponse> JoinGameAsync(Guid gameId, Guid userId, CancellationToken ct)
            => Task.FromResult(new JoinGameResponse(Guid.NewGuid(), GameState!));
        public Task<bool> MarkMatchAsStartedAsync(Guid gameId, CancellationToken ct) => Task.FromResult(true);
        public Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct)
            => Task.FromResult(NewGameResponse ?? new NewGameResponse("", req.PlayerCount, req.TargetWins));
        public Task<TurnState> StartMatchAsync(Guid gameId, CancellationToken ct)
            => Task.FromResult(new TurnState(Guid.NewGuid(), 0, 0, DateTime.UtcNow, DateTime.UtcNow, 1, 0));
        public Task<ProcessTurnOutcome> ProcessTurnAsync(Guid gameId, Guid roundId, Guid turnId, Guid playerId, int seat, string word, CancellationToken ct)
            => Task.FromResult(new ProcessTurnOutcome(false, "bad", null, null));
        public Task<List<TurnHistoryState>> GetRecentWordHistoryAsync(Guid gameId, CancellationToken ct)
            => Task.FromResult(new List<TurnHistoryState>());
        public Task ReadyAsync(Guid gameId, int seat, bool isReady, CancellationToken ct) => Task.CompletedTask;
        public Task LeaveGameAsync(Guid userId, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class MissingUserResolver : IAppUserResolver
    {
        public bool TryGet(out Guid userId)
        {
            userId = default;
            return false;
        }
    }
}