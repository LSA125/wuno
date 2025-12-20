using wuno.domain;

namespace Wuno.Testing.Builders
{
    public sealed class GameBuilder
    {
        private Guid _id = Guid.NewGuid();
        private string _code = "TEST";
        private GameStatus _status = GameStatus.ACTIVE;
        private int _targetWins = 2;
        private int _curSeat;
        private int _direction = 1;
        private DateTime _createdAt = DateTime.UtcNow;
        private string? _lastWord;
        private bool _isPublic = false;
        private readonly List<PlayerBuilder> _players = new();
        private readonly List<Round> _rounds = new();
        private readonly List<TurnBuilder> _turns = new();

        public GameBuilder WithId(Guid id) { _id = id; return this; }
        public GameBuilder WithCode(string code) { _code = code; return this; }
        public GameBuilder WithStatus(GameStatus status) { _status = status; return this; }
        public GameBuilder TargetWins(int wins) { _targetWins = wins; return this; }
        public GameBuilder CurrentSeat(int seat) { _curSeat = seat; return this; }
        public GameBuilder Direction(int direction) { _direction = direction; return this; }
        public GameBuilder Created(DateTime created) { _createdAt = created; return this; }
        public GameBuilder LastWord(string? word) { _lastWord = word; return this; }
        public GameBuilder IsPublic(bool isPublic) { _isPublic = isPublic; return this; }
        public GameBuilder AddPlayer(PlayerBuilder player) { _players.Add(player); return this; }
        public GameBuilder AddRound(Round round) { _rounds.Add(round); return this; }
        public GameBuilder AddRound(RoundBuilder round) { _rounds.Add(round.Build()); return this; }
        public GameBuilder AddTurn(TurnBuilder turn) { _turns.Add(turn); return this; }

        public Game Build()
        {
            var game = new Game
            {
                Id = _id,
                Code = _code,
                Status = _status,
                TargetWins = _targetWins,
                CurSeat = _curSeat,
                Direction = _direction,
                CreatedAt = _createdAt,
                LastWord = _lastWord,
                IsPublic = _isPublic,
            };

            foreach (var round in _rounds)
            {
                round.GameId = game.Id;
                round.Game = game;
                game.Rounds.Add(round);
            }

            foreach (var playerBuilder in _players)
            {
                var player = playerBuilder.WithGame(game).Build();
                game.Players.Add(player);
            }

            foreach (var turnBuilder in _turns)
            {
                var turn = turnBuilder.WithGame(game).Build();
                game.Turns.Add(turn);
            }

            return game;
        }
    }
}

