using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Wuno.Application.Games.Implementation;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Games.Util;
using Wuno.Testing.Builders;
using Wuno.Testing.Fixtures;
using wuno.domain;
using wuno.infrastructure;

public sealed class GameServiceTests
{
    private static GameService CreateService(AppDbContext db, ITurnTimer? timer = null)
    {
        return new GameService(db, new AcceptAllWordList(), new StubCodeGenerator("TEST01"), timer ?? new FakeTurnTimer());
    }

    [Fact]
    public async Task StartNewGameAsync_creates_waiting_game_with_seeded_players()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        using var db = factory.CreateContext();
        var timer = new FakeTurnTimer();
        var service = CreateService(db, timer);

        var response = await service.StartNewGameAsync(new NewGameRequest(5, 3), CancellationToken.None);

        Assert.Equal("TEST01", response.GameCode);
        var saved = await db.Games.Include(g => g.Players).SingleAsync();
        Assert.Equal(GameStatus.WAITING, saved.Status);
        Assert.Equal(5, saved.Players.Count);
        Assert.All(saved.Players.OrderBy(p => p.Seat).Select((p, idx) => (p, idx)), pair => Assert.Equal(pair.idx + 1, pair.p.Seat));
    }

    [Fact]
    public async Task JoinGameAsync_connects_existing_user_and_returns_state()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var game = new GameBuilder()
            .WithStatus(GameStatus.WAITING)
            .AddPlayer(new PlayerBuilder().AtSeat(1).Taken(false).Active(false).Connected(false))
            .AddPlayer(new PlayerBuilder().AtSeat(2).Taken(false).Active(false).Connected(false))
            .Build();
        var user = new User { Name = "Guest" };

        using var db = factory.CreateContext(ctx => ctx.AddRange(game, user));
        var service = CreateService(db);

        var response = await service.JoinGameAsync(game.Id, user.Id, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.PlayerId);
        var refreshedUser = await db.Users.Include(u => u.ActivePlayer).SingleAsync();
        Assert.NotNull(refreshedUser.ActivePlayer);
        Assert.True(refreshedUser.ActivePlayer!.IsTaken);
        Assert.Equal(refreshedUser.ActivePlayer.Id, response.PlayerId);
        Assert.Equal(game.Id, response.State.GameId);
    }

    [Fact]
    public async Task ReadyAsync_toggles_players_and_all_ready_reflects_status()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var game = new GameBuilder()
            .WithStatus(GameStatus.WAITING)
            .AddPlayer(new PlayerBuilder().AtSeat(1).Active(false))
            .AddPlayer(new PlayerBuilder().AtSeat(2).Active(false))
            .Build();
        using var db = factory.CreateContext(ctx => ctx.Add(game));
        var service = CreateService(db);

        await service.ReadyAsync(game.Id, 1, true, CancellationToken.None);
        Assert.False(await service.AreAllPlayersReadyAsync(game.Id, CancellationToken.None));

        await service.ReadyAsync(game.Id, 2, true, CancellationToken.None);
        Assert.True(await service.AreAllPlayersReadyAsync(game.Id, CancellationToken.None));
    }

    [Fact]
    public async Task MarkMatchAsStartedAsync_allows_only_first_concurrent_transition()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var game = new GameBuilder().WithStatus(GameStatus.WAITING).Build();

        var (db1, db2) = factory.CreateConcurrentPair(ctx => ctx.Add(game));
        var svc1 = CreateService(db1);
        var svc2 = CreateService(db2);

        var results = await Task.WhenAll(
            svc1.MarkMatchAsStartedAsync(game.Id, CancellationToken.None),
            svc2.MarkMatchAsStartedAsync(game.Id, CancellationToken.None));
        var (first, second) = (results[0], results[1]);

        Assert.True(first ^ second); // exactly one succeeds

        await using var verifier = factory.CreateContext();
        var saved = await verifier.Games.SingleAsync();
        Assert.Equal(GameStatus.ACTIVE, saved.Status);
    }

    [Fact]
    public async Task StartMatchAsync_creates_first_round_and_turn()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();

        var seedGame = new GameBuilder()
            .WithStatus(GameStatus.WAITING)
            .AddPlayer(new PlayerBuilder().AtSeat(1))
            .AddPlayer(new PlayerBuilder().AtSeat(2))
            .Build();

        // Seed in its own context
        using (var seedCtx = factory.CreateContext())
        {
            seedCtx.Games.Add(seedGame);
            await seedCtx.SaveChangesAsync();
        }

        // Fresh context for service + assertions
        using var db = factory.CreateContext();
        var service = CreateService(db);

        var turn = await service.StartMatchAsync(seedGame.Id, CancellationToken.None);

        Assert.Equal(1, turn.Seat);

        var saved = await db.Games
            .Include(g => g.Rounds)
            .Include(g => g.Turns)
            .SingleAsync();

        Assert.Equal(GameStatus.ACTIVE, saved.Status);
        Assert.Equal(1, saved.Rounds.Count);
        Assert.Equal(1, saved.Turns.Count);
        Assert.NotNull(saved.CurrentRound);
        Assert.NotNull(saved.CurrentTurn);
        Assert.Equal(saved.CurrentTurn!.Id, turn.TurnId);
    }

    [Fact]
    public async Task ProcessTurnAsync_accepts_valid_word_and_moves_to_next_player()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var game = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)
            .AddPlayer(new PlayerBuilder().AtSeat(1))
            .AddPlayer(new PlayerBuilder().AtSeat(2))
            .AddRound(new RoundBuilder().WithIndex(0))
            .AddTurn(new TurnBuilder().WithIndex(0).AtSeat(1).FreeStart())
            .AddRound(new RoundBuilder().WithIndex(0))
            .CurrentSeat(1)
            .Build();
        using var db = factory.CreateContext(ctx => ctx.Add(game));
        var service = CreateService(db);
        game.CurrentTurn = game.Turns[0];
        game.CurrentRound = game.Rounds[0];

        var result = await service.ProcessTurnAsync(game.Id, game.Rounds[0].Id, game.Turns[0].Id, game.Players[0].Id, 1, "alpha", CancellationToken.None);

        Assert.True(result.Ok);
        Assert.NotNull(result.State);
        Assert.Equal(2, result.State!.CurrentTurn.Seat);
        var playerOne = await db.Players.FindAsync(game.Players[0].Id);
        Assert.Equal("alpha", playerOne!.LastWord);
    }

    [Fact]
    public async Task TimeoutAndAdvanceAsync_marks_player_inactive_and_creates_next_turn()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var past = DateTime.UtcNow.AddSeconds(-5);

        var seedGame = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)
            .AddPlayer(new PlayerBuilder().AtSeat(1))
            .AddPlayer(new PlayerBuilder().AtSeat(2))
            .AddRound(new RoundBuilder().WithIndex(0))
            .AddTurn(new TurnBuilder().WithIndex(0).AtSeat(1).DueAt(past).StartedAt(past.AddSeconds(-10)))
            .CurrentSeat(1)
            .Build();

        // 1. Seed in its own context
        using (var seedCtx = factory.CreateContext())
        {
            seedCtx.Games.Add(seedGame);
            await seedCtx.SaveChangesAsync();

            // Set CurrentRound/CurrentTurn on the tracked entities and persist
            var game = await seedCtx.Games
                .Include(g => g.Rounds)
                .Include(g => g.Turns)
                .SingleAsync();

            game.CurrentRound = game.Rounds[0];
            game.CurrentTurn = game.Turns[0];
            await seedCtx.SaveChangesAsync();
        }

        // 2. Use a fresh context for the service call
        using var db = factory.CreateContext();
        var service = CreateService(db);

        var gameId = seedGame.Id;
        var turnId = seedGame.Turns[0].Id;

        var state = await service.TimeoutAndAdvanceAsync(gameId, turnId, CancellationToken.None);

        Assert.NotNull(state);
        Assert.Equal(2, state!.CurrentTurn.Seat);

        var timedOut = await db.Turns.FindAsync(turnId);
        Assert.Equal(TurnEndReason.TIMEOUT, timedOut!.EndReason);

        var p1 = await db.Players.FindAsync(seedGame.Players[0].Id);
        Assert.False(p1!.IsActive);
    }

    [Fact]
    public async Task ForceEndGame_closes_open_turns_and_cancels_timer()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var turnId = Guid.NewGuid();
        var game = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)
            .AddPlayer(new PlayerBuilder().AtSeat(1))
            .AddRound(new RoundBuilder().WithIndex(0))
            .AddTurn(new TurnBuilder().WithId(turnId).WithIndex(0).AtSeat(1))
            .CurrentSeat(1)
            .Build();



        var timer = new FakeTurnTimer();
        timer.Schedule(game.Id, turnId, DateTime.UtcNow.AddMinutes(5), _ => Task.CompletedTask);

        using var db = factory.CreateContext(ctx => ctx.Add(game));
        var service = CreateService(db, timer);

        await service.ForceEndGame(game.Id, CancellationToken.None);

        var updated = await db.Games.Include(g => g.Turns).SingleAsync();
        Assert.Equal(GameStatus.FINISHED, updated.Status);
        Assert.All(updated.Turns, t => Assert.NotNull(t.EndedAt));
        Assert.Empty(timer.Scheduled);
    }

    private sealed class AcceptAllWordList : IWordList
    {
        public bool IsWord(string word) => true;
    }

    private sealed class StubCodeGenerator : ICodeGeneratorService
    {
        private readonly string _code;
        public StubCodeGenerator(string code) => _code = code;
        public string GenerateCode() => _code;
    }
}