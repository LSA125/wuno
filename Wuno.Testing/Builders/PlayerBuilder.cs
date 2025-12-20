using wuno.domain;

namespace Wuno.Testing.Builders
{
    public sealed class PlayerBuilder
    {
        private Guid _id = Guid.NewGuid();
        private Guid _gameId = Guid.NewGuid();
        private Game? _game;
        private Guid? _userId;
        private User? _user;
        private string _name = "Player";
        private string? _iconUrl;
        private bool _isActive = true;
        private bool _isConnected = true;
        private bool _isTaken = true;
        private int _seat;
        private int _roundWins;
        private int _turnsPlayed;
        private string? _lastWord;
        private double _remainingTime = 15.0;  // Default initial time

        public PlayerBuilder WithId(Guid id) { _id = id; return this; }
        public PlayerBuilder WithGame(Game game) { _game = game; _gameId = game.Id; return this; }
        public PlayerBuilder WithGameId(Guid id) { _gameId = id; return this; }
        public PlayerBuilder WithUser(User user) { _user = user; _userId = user.Id; return this; }
        public PlayerBuilder WithUserId(Guid? id) { _userId = id; return this; }
        public PlayerBuilder WithName(string name) { _name = name; return this; }
        public PlayerBuilder WithIcon(string? url) { _iconUrl = url; return this; }
        public PlayerBuilder Active(bool active = true) { _isActive = active; return this; }
        public PlayerBuilder Connected(bool connected = true) { _isConnected = connected; return this; }
        public PlayerBuilder Taken(bool taken = true) { _isTaken = taken; return this; }
        public PlayerBuilder AtSeat(int seat) { _seat = seat; return this; }
        public PlayerBuilder RoundWins(int wins) { _roundWins = wins; return this; }
        public PlayerBuilder TurnsPlayed(int count) { _turnsPlayed = count; return this; }
        public PlayerBuilder LastWord(string? word) { _lastWord = word; return this; }
        public PlayerBuilder WithRemainingTime(double time) { _remainingTime = time; return this; }

        public Player Build()
        {
            return new Player
            {
                Id = _id,
                GameId = _gameId,
                Game = _game ?? new Game { Id = _gameId },
                UserId = _userId,
                User = _user,
                Name = _name,
                IconUrl = _iconUrl,
                IsActive = _isActive,
                IsConnected = _isConnected,
                IsTaken = _isTaken,
                Seat = _seat,
                RoundWins = _roundWins,
                TurnsPlayedThisRound = _turnsPlayed,
                LastWord = _lastWord,
                RemainingTime = _remainingTime,
            };
        }
    }
}
