using System;
using System.Linq;
using Wuno.Testing.Builders;
using wuno.domain;
using Xunit;

namespace Wuno.Domain.Tests
{
    public class EntityFactoryTests
    {
        [Fact]
        public void UserBuilder_populates_all_fields_and_accepts_unicode()
        {
            var id = Guid.Empty;
            var created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastActive = created.AddDays(1);

            var user = new UserBuilder()
                .WithId(id)
                .WithName(" José ", "jose")
                .WithIcon("https://example.com/icon.png")
                .WithEmail("User@Example.com", "user@example.com")
                .VerifiedAt(created)
                .WithPassword("hash")
                .Registered()
                .Created(created)
                .LastActive(lastActive)
                .WithActivePlayer(Guid.NewGuid())
                .Build();

            Assert.Equal(id, user.Id);
            Assert.Equal(" José ", user.Name);
            Assert.Equal("jose", user.NameNormalized);
            Assert.Equal("https://example.com/icon.png", user.IconUrl);
            Assert.Equal("User@Example.com", user.Email);
            Assert.Equal("user@example.com", user.EmailNormalized);
            Assert.Equal(created, user.EmailVerifiedAt);
            Assert.Equal("hash", user.PasswordHash);
            Assert.True(user.IsRegistered);
            Assert.Equal(created, user.CreatedAt);
            Assert.Equal(lastActive, user.LastActiveAt);
            Assert.NotNull(user.ActivePlayerId);
        }

        [Fact]
        public void GameBuilder_links_rounds_turns_players_and_sets_current_items()
        {
            var gameId = Guid.Empty;
            var player = new PlayerBuilder().WithName("Player ☃").AtSeat(-1);
            var roundBuilder = new RoundBuilder().WithIndex(0);
            var turnBuilder = new TurnBuilder().WithIndex(0).WithRound(roundBuilder.Build()).AtSeat(-2).MinLength(0);

            var game = new GameBuilder()
                .WithId(gameId)
                .WithCode(new string('C', 32))
                .WithStatus(GameStatus.WAITING)
                .TargetWins(-5)
                .CurrentSeat(-10)
                .Direction(-1)
                .AddPlayer(player)
                .AddRound(roundBuilder)
                .AddTurn(turnBuilder)
                .Build();

            Assert.Equal(gameId, game.Id);
            Assert.Equal(new string('C', 32), game.Code);
            Assert.Equal(GameStatus.WAITING, game.Status);
            Assert.Equal(-5, game.TargetWins);
            Assert.Equal(-10, game.CurSeat);
            Assert.Equal(-1, game.Direction);

            var builtPlayer = Assert.Single(game.Players);
            Assert.Same(game, builtPlayer.Game);
            Assert.Equal(-1, builtPlayer.Seat);
            Assert.Equal("Player ☃", builtPlayer.Name);

            var builtRound = Assert.Single(game.Rounds);
            Assert.Same(game, builtRound.Game);
            Assert.Equal(game.Id, builtRound.GameId);
            Assert.Equal(0, builtRound.Index);

            var builtTurn = Assert.Single(game.Turns);
            Assert.Same(game, builtTurn.Game);
            Assert.Equal(builtRound.Id, builtTurn.RoundId);
            Assert.Equal(-2, builtTurn.Seat);
            Assert.Equal(0, builtTurn.MinLen);
        }

        [Fact]
        public void GameBuilder_does_not_set_current_round_or_turn_out_of_range()
        {
            var game = new GameBuilder()
                .Build();

            Assert.Empty(game.Rounds);
            Assert.Empty(game.Turns);
            Assert.Null(game.CurrentRound);
            Assert.Null(game.CurrentTurn);
        }

        [Fact]
        public void PlayerBuilder_preserves_invalid_identifiers_and_counts()
        {
            var player = new PlayerBuilder()
                .WithGameId(Guid.Empty)
                .WithUserId(Guid.Empty)
                .AtSeat(-3)
                .RoundWins(-1)
                .TurnsPlayed(0)
                .Active(false)
                .Connected(false)
                .Taken(false)
                .LastWord(new string('ø', 64))
                .Build();

            Assert.Equal(Guid.Empty, player.GameId);
            Assert.Equal(Guid.Empty, player.UserId);
            Assert.Equal(-3, player.Seat);
            Assert.Equal(-1, player.RoundWins);
            Assert.Equal(0, player.TurnsPlayedThisRound);
            Assert.Equal(new string('ø', 64), player.LastWord);
            Assert.False(player.IsActive);
            Assert.False(player.IsConnected);
            Assert.False(player.IsTaken);
        }

        [Fact]
        public void TurnBuilder_allows_zero_min_length_and_links_round_and_game()
        {
            var game = new GameBuilder().Build();
            var round = new RoundBuilder().WithGame(game).WithIndex(2).Build();

            var turn = new TurnBuilder()
                .WithGame(game)
                .WithRound(round)
                .WithIndex(5)
                .AtSeat(0)
                .MinLength(0)
                .WithWord("word")
                .WithEndReason(TurnEndReason.TIMEOUT)
                .Build();

            Assert.Equal(game.Id, turn.GameId);
            Assert.Same(game, turn.Game);
            Assert.Equal(round.Id, turn.RoundId);
            Assert.Same(round, turn.Round);
            Assert.Equal(5, turn.Index);
            Assert.Equal(0, turn.MinLen);
            Assert.Equal("word", turn.Word);
            Assert.Equal(TurnEndReason.TIMEOUT, turn.EndReason);
            Assert.True(turn.DueAt >= turn.StartedAt);
        }
    }
}