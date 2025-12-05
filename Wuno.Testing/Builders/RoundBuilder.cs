using wuno.domain;

namespace Wuno.Testing.Builders
{
    public sealed class RoundBuilder
    {
        private Guid _id = Guid.NewGuid();
        private Guid _gameId = Guid.NewGuid();
        private Game? _game;
        private int _index;
        private Guid? _winnerId;
        private DateTime? _startedAt = DateTime.UtcNow;
        private DateTime? _endedAt;

        public RoundBuilder WithId(Guid id) { _id = id; return this; }
        public RoundBuilder WithGame(Game game) { _game = game; _gameId = game.Id; return this; }
        public RoundBuilder WithGameId(Guid id) { _gameId = id; return this; }
        public RoundBuilder WithIndex(int index) { _index = index; return this; }
        public RoundBuilder WithWinner(Guid? playerId) { _winnerId = playerId; return this; }
        public RoundBuilder StartedAt(DateTime? started) { _startedAt = started; return this; }
        public RoundBuilder EndedAt(DateTime? ended) { _endedAt = ended; return this; }

        public Round Build()
        {
            return new Round
            {
                Id = _id,
                GameId = _gameId,
                Game = _game ?? new Game { Id = _gameId },
                Index = _index,
                WinnerId = _winnerId,
                StartedAt = _startedAt,
                EndedAt = _endedAt,
            };
        }
    }
}
