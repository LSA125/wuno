using Microsoft.EntityFrameworkCore;
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

        // Fresh context for service + assertions
        using var db = factory.CreateContext();
        var service = CreateService(db);
        db.Games.Add(seedGame);
        await db.SaveChangesAsync();

        var turn = await service.StartMatchAsync(seedGame.Id, CancellationToken.None);

        Assert.Equal(1, turn.Seat);

        var saved = await db.Games
            .Include(g => g.Rounds)
            .Include(g => g.Turns)
            .SingleAsync();

        Assert.Equal(GameStatus.ACTIVE, saved.Status);
        Assert.Single(saved.Rounds);
        Assert.Single(saved.Turns);
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
            .AddTurn(new TurnBuilder().WithIndex(0).AtSeat(1))
            .AddRound(new RoundBuilder().WithIndex(0))
            .CurrentSeat(1)
            .Build();
        using var db = factory.CreateContext(ctx => ctx.Add(game));
        var service = CreateService(db);
        game.CurrentTurn = game.Turns[0];
        game.CurrentRound = game.Rounds[0];
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();


        var result = await service.ProcessTurnAsync(game.Id, game.Rounds[0].Id, game.Turns[0].Id, game.Players[0].Id, 1, "alpha", CancellationToken.None);

        Assert.True(result.Ok);
        Assert.NotNull(result.State);
        Assert.Equal(2, result.State!.CurrentTurn.Seat);
        var playerOne = await db.Players.FindAsync(game.Players[0].Id);
        Assert.Equal("alpha", playerOne!.LastWord);
    }
    [Fact]
    public async Task ProcessTurnAsync_rejects_overdue_submition_with_timeout()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var past = DateTime.UtcNow.AddSeconds(-5);

        var game = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)
            .AddPlayer(new PlayerBuilder().AtSeat(1))
            .AddPlayer(new PlayerBuilder().AtSeat(2))
            .AddRound(new RoundBuilder().WithIndex(0))
            .AddTurn(new TurnBuilder().WithIndex(0).AtSeat(1).DueAt(past).StartedAt(past.AddSeconds(-10)))
            .CurrentSeat(1)
        .Build();

        using var db = factory.CreateContext(ctx => ctx.Add(game));
        var service = CreateService(db);

        game.CurrentTurn = game.Turns[0];
        game.CurrentRound = game.Rounds[0];
        var turnId = game.Turns[0].Id;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await service.ProcessTurnAsync(game.Id, game.Rounds[0].Id, game.Turns[0].Id, game.Players[0].Id, 1, "alpha", CancellationToken.None);

        var turnReason = await db.Turns.FindAsync(turnId);
        Assert.Equal(TurnEndReason.TIMEOUT, turnReason!.EndReason);

        Assert.True(result.Ok);
        Assert.NotNull(result.State);
        Assert.NotEqual(result.State.CurrentTurn!.TurnId, turnId);
    }

    [Fact]
    public async Task TimeoutAndAdvanceAsync_marks_player_inactive_and_creates_next_turn()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var past = DateTime.UtcNow.AddSeconds(-5);

        var game = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)
            .AddPlayer(new PlayerBuilder().AtSeat(1))
            .AddPlayer(new PlayerBuilder().AtSeat(2))
            .AddRound(new RoundBuilder().WithIndex(0))
            .AddTurn(new TurnBuilder().WithIndex(0).AtSeat(1).DueAt(past).StartedAt(past.AddSeconds(-10)))
            .CurrentSeat(1)
        .Build();

        using var db = factory.CreateContext(ctx => ctx.Add(game));
        var service = CreateService(db);

        game.CurrentTurn = game.Turns[0];
        game.CurrentRound = game.Rounds[0];
        game.CurSeat = 1;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var gameId = game.Id;
        var turnId = game.Turns[0].Id;

        var state = await service.TimeoutAndAdvanceAsync(gameId, turnId, CancellationToken.None);

        Assert.NotNull(state);
        Assert.Equal(2, state!.CurrentTurn.Seat);

        var timedOut = await db.Turns.FindAsync(turnId);
        Assert.Equal(TurnEndReason.TIMEOUT, timedOut!.EndReason);
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
    [Fact]
    public async Task StartMatchAsync_resets_player_remaining_time_to_initial()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        
        // Player starts with custom time that should be reset at match start
        var seedGame = new GameBuilder()
            .WithStatus(GameStatus.WAITING)
            .AddPlayer(new PlayerBuilder().AtSeat(1).WithRemainingTime(20.0))
            .AddPlayer(new PlayerBuilder().AtSeat(2).WithRemainingTime(10.0))
            .Build();

        using var db = factory.CreateContext();
        var service = CreateService(db);
        db.Games.Add(seedGame);
        await db.SaveChangesAsync();

        var turn = await service.StartMatchAsync(seedGame.Id, CancellationToken.None);

        // At match start, ResetPlayers() should reset all players to INITIAL_REMAINING_TIME_SEC (30)
        // So the first turn should use 30s (or capped by tMax/MIN_ACTUAL_TIME_SEC)
        var durationSeconds = (turn.DueAt - turn.StartedAt).TotalSeconds;
        
        // First turn max time is 40, initial time is 30, so duration should be 30
        Assert.Equal(30, (int)durationSeconds);
    }

    [Fact]
    public async Task ProcessTurnAsync_next_player_turn_uses_their_remaining_time()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var player2RemainingTime = 8.0;  // Custom short time for player 2
        
        var game = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)
            .AddPlayer(new PlayerBuilder().AtSeat(1).WithRemainingTime(15.0))
            .AddPlayer(new PlayerBuilder().AtSeat(2).WithRemainingTime(player2RemainingTime))
            .AddRound(new RoundBuilder().WithIndex(0))
            .AddTurn(new TurnBuilder().WithIndex(0).AtSeat(1))
            .CurrentSeat(1)
            .Build();
        
        using var db = factory.CreateContext(ctx => ctx.Add(game));
        var service = CreateService(db);
        game.CurrentTurn = game.Turns[0];
        game.CurrentRound = game.Rounds[0];
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Player 1 submits a word, starting player 2's turn
        var result = await service.ProcessTurnAsync(game.Id, game.Rounds[0].Id, game.Turns[0].Id, game.Players[0].Id, 1, "alpha", CancellationToken.None);

        Assert.True(result.Ok);
        Assert.NotNull(result.State);
        Assert.Equal(2, result.State!.CurrentTurn.Seat);
        
        // Player 2's turn should use their remaining time
        var turnDuration = (result.State.CurrentTurn.DueAt - result.State.CurrentTurn.StartedAt).TotalSeconds;
        Assert.True(turnDuration <= player2RemainingTime + 1,  // +1 for any bonus time from scoring
            $"Turn duration ({turnDuration}s) should respect player 2's remaining time ({player2RemainingTime}s)");
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

    [Fact]
    public async Task JoinGameAsync_allows_joining_active_games()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var game = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)  // Game already started
            .AddPlayer(new PlayerBuilder().AtSeat(1).Taken(true).Active(true).Connected(true))
            .AddPlayer(new PlayerBuilder().AtSeat(2).Taken(false).Active(false).Connected(false))  // Open slot
            .Build();
        var user = new User { Name = "LateJoiner" };

        using var db = factory.CreateContext(ctx => ctx.AddRange(game, user));
        var service = CreateService(db);

        var response = await service.JoinGameAsync(game.Id, user.Id, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.PlayerId);
        var refreshedUser = await db.Users.Include(u => u.ActivePlayer).FirstAsync(u => u.Id == user.Id);
        Assert.NotNull(refreshedUser.ActivePlayer);
        Assert.True(refreshedUser.ActivePlayer!.IsTaken);
        Assert.Equal(2, refreshedUser.ActivePlayer.Seat);  // Should take seat 2
    }

    [Fact]
    public async Task JoinGameAsync_rejects_finished_games()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var game = new GameBuilder()
            .WithStatus(GameStatus.FINISHED)
            .AddPlayer(new PlayerBuilder().AtSeat(1).Taken(false).Active(false).Connected(false))
            .Build();
        var user = new User { Name = "TooLate" };

        using var db = factory.CreateContext(ctx => ctx.AddRange(game, user));
        var service = CreateService(db);

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            service.JoinGameAsync(game.Id, user.Id, CancellationToken.None));
        Assert.Equal("Game not joinable", ex.Message);
    }

    [Fact]
    public async Task JoinGameAsync_reconnects_player_already_in_game()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var playerBuilder = new PlayerBuilder().AtSeat(1).Taken(true).Active(true).Connected(false);
        var game = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)
            .AddPlayer(playerBuilder)
            .AddPlayer(new PlayerBuilder().AtSeat(2).Taken(true).Active(true).Connected(true))
            .Build();
        var player = game.Players.First(p => p.Seat == 1);
        var user = new User { Name = "Reconnector" };
        player.UserId = user.Id;

        // First save game and user without circular FK
        using var db = factory.CreateContext(ctx => ctx.AddRange(game, user));
        // Now update ActivePlayerId after entities exist
        user.ActivePlayerId = player.Id;
        await db.SaveChangesAsync();
        
        var service = CreateService(db);

        var response = await service.JoinGameAsync(game.Id, user.Id, CancellationToken.None);

        Assert.Equal(player.Id, response.PlayerId);
        var refreshedPlayer = await db.Players.FindAsync(player.Id);
        Assert.True(refreshedPlayer!.IsConnected);  // Should now be connected
    }

    [Fact]
    public async Task FindOrCreatePublicGameAsync_finds_existing_waiting_game()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var existingGame = new GameBuilder()
            .WithCode("PUBLIC")
            .WithStatus(GameStatus.WAITING)
            .IsPublic(true)
            .AddPlayer(new PlayerBuilder().AtSeat(1).Taken(true).Active(false).Connected(true))
            .AddPlayer(new PlayerBuilder().AtSeat(2).Taken(false).Active(false).Connected(false))  // Open slot
            .Build();

        using var db = factory.CreateContext(ctx => ctx.Add(existingGame));
        var service = CreateService(db);

        var result = await service.FindOrCreatePublicGameAsync(CancellationToken.None);

        Assert.True(result.Ok);
        Assert.Equal("PUBLIC", result.GameCode);
        Assert.False(result.WasCreated);  // Should find existing, not create new
    }

    [Fact]
    public async Task FindOrCreatePublicGameAsync_finds_existing_active_game()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var existingGame = new GameBuilder()
            .WithCode("ACTIVE1")
            .WithStatus(GameStatus.ACTIVE)  // Already started but has room
            .IsPublic(true)
            .AddPlayer(new PlayerBuilder().AtSeat(1).Taken(true).Active(true).Connected(true))
            .AddPlayer(new PlayerBuilder().AtSeat(2).Taken(false).Active(false).Connected(false))  // Open slot
            .Build();

        using var db = factory.CreateContext(ctx => ctx.Add(existingGame));
        var service = CreateService(db);

        var result = await service.FindOrCreatePublicGameAsync(CancellationToken.None);

        Assert.True(result.Ok);
        Assert.Equal("ACTIVE1", result.GameCode);
        Assert.False(result.WasCreated);  // Should find existing active game
    }

    [Fact]
    public async Task FindOrCreatePublicGameAsync_creates_new_game_when_none_available()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        // No existing games
        using var db = factory.CreateContext();
        var service = CreateService(db);

        var result = await service.FindOrCreatePublicGameAsync(CancellationToken.None);

        Assert.True(result.Ok);
        Assert.Equal("TEST01", result.GameCode);  // From StubCodeGenerator
        Assert.True(result.WasCreated);  // Should create new

        var savedGame = await db.Games.SingleAsync();
        Assert.True(savedGame.IsPublic);
        Assert.Equal(GameStatus.WAITING, savedGame.Status);
    }

    [Fact]
    public async Task FindOrCreatePublicGameAsync_ignores_finished_games()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var finishedGame = new GameBuilder()
            .WithCode("DONE01")
            .WithStatus(GameStatus.FINISHED)  // Finished, should be ignored
            .IsPublic(true)
            .AddPlayer(new PlayerBuilder().AtSeat(1).Taken(false).Active(false).Connected(false))  // Open slot but game is done
            .Build();

        using var db = factory.CreateContext(ctx => ctx.Add(finishedGame));
        var service = CreateService(db);

        var result = await service.FindOrCreatePublicGameAsync(CancellationToken.None);

        Assert.True(result.Ok);
        Assert.Equal("TEST01", result.GameCode);  // Should create new, not find finished
        Assert.True(result.WasCreated);
    }

    [Fact]
    public async Task LeaveGameAsync_returns_correct_result_and_ends_game_when_one_player_left()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var player1 = new PlayerBuilder().AtSeat(1).Taken(true).Active(true).Connected(true);
        var player2 = new PlayerBuilder().AtSeat(2).Taken(true).Active(true).Connected(true);
        var game = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)
            .AddPlayer(player1)
            .AddPlayer(player2)
            .Build();
        var user1 = new User { Name = "Player1" };
        var p1 = game.Players.First(p => p.Seat == 1);
        p1.UserId = user1.Id;

        // First save game and user without circular FK
        using var db = factory.CreateContext(ctx => ctx.AddRange(game, user1));
        // Now update ActivePlayerId after entities exist
        user1.ActivePlayerId = p1.Id;
        await db.SaveChangesAsync();
        
        var timer = new FakeTurnTimer();
        var service = CreateService(db, timer);
        var playerId = p1.Id;  // Capture before LeaveGameAsync sets it to null

        // Player 1 leaves - only player 2 remains, so game should end
        var result = await service.LeaveGameAsync(user1.Id, CancellationToken.None);

        Assert.Equal(game.Id, result.GameId);
        Assert.True(result.GameEnded);  // Only 1 player left = game should end
        Assert.NotNull(result.FinalState);
        Assert.Equal(GameStatus.FINISHED, result.FinalState!.Status);
        Assert.Null(result.RemainingPlayers);  // Not returned when game ended
        
        // Verify player slot was reset
        var refreshedPlayer = await db.Players.FindAsync(playerId);
        Assert.False(refreshedPlayer!.IsTaken);
        Assert.False(refreshedPlayer.IsConnected);
    }

    [Fact]
    public async Task LeaveGameAsync_returns_remaining_players_when_game_continues()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var player1 = new PlayerBuilder().AtSeat(1).Taken(true).Active(true).Connected(true);
        var player2 = new PlayerBuilder().AtSeat(2).Taken(true).Active(true).Connected(true);
        var player3 = new PlayerBuilder().AtSeat(3).Taken(true).Active(true).Connected(true);
        var game = new GameBuilder()
            .WithStatus(GameStatus.ACTIVE)
            .AddPlayer(player1)
            .AddPlayer(player2)
            .AddPlayer(player3)
            .Build();
        var user1 = new User { Name = "Player1" };
        var p1 = game.Players.First(p => p.Seat == 1);
        p1.UserId = user1.Id;

        // First save game and user without circular FK
        using var db = factory.CreateContext(ctx => ctx.AddRange(game, user1));
        // Now update ActivePlayerId after entities exist
        user1.ActivePlayerId = p1.Id;
        await db.SaveChangesAsync();
        
        var service = CreateService(db);

        // Player 1 leaves - 2 players remain, game continues
        var result = await service.LeaveGameAsync(user1.Id, CancellationToken.None);

        Assert.Equal(game.Id, result.GameId);
        Assert.False(result.GameEnded);  // 2+ players left = game continues
        Assert.Null(result.FinalState);
        Assert.NotNull(result.RemainingPlayers);
        Assert.Equal(2, result.RemainingPlayers!.Count);
    }
}