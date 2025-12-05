using wuno.domain;

namespace Wuno.Testing.Builders
{
    public sealed class TurnBuilder
    {
        private Guid _id = Guid.NewGuid();
        private Guid _gameId = Guid.NewGuid();
        private Game? _game;
        private Guid _roundId = Guid.NewGuid();
        private Round? _round;
        private int _index;
        private int _seat;
        private int _minLen = 1;
        private bool _freeStart;
        private DateTime _startedAt = DateTime.UtcNow;
        private DateTime _dueAt = DateTime.UtcNow.AddSeconds(30);
        private string? _word;
        private DateTime? _endedAt;
        private TurnEndReason? _endReason;

        public TurnBuilder WithId(Guid id) { _id = id; return this; }
        public TurnBuilder WithGame(Game game) { _game = game; _gameId = game.Id; return this; }
        public TurnBuilder WithGameId(Guid id) { _gameId = id; return this; }
        public TurnBuilder WithRound(Round round) { _round = round; _roundId = round.Id; return this; }
        public TurnBuilder WithRoundId(Guid id) { _roundId = id; return this; }
        public TurnBuilder WithIndex(int index) { _index = index; return this; }
        public TurnBuilder AtSeat(int seat) { _seat = seat; return this; }
        public TurnBuilder MinLength(int minLen) { _minLen = minLen; return this; }
        public TurnBuilder FreeStart(bool freeStart = true) { _freeStart = freeStart; return this; }
        public TurnBuilder StartedAt(DateTime start) { _startedAt = start; return this; }
        public TurnBuilder DueAt(DateTime due) { _dueAt = due; return this; }
        public TurnBuilder WithWord(string? word) { _word = word; return this; }
        public TurnBuilder EndedAt(DateTime? end) { _endedAt = end; return this; }
        public TurnBuilder WithEndReason(TurnEndReason? reason) { _endReason = reason; return this; }

        public Turn Build()
        {
            var gameRef = _game ?? new Game { Id = _gameId };
            var roundRef = _round ?? new Round { Id = _roundId, GameId = _gameId, Game = gameRef };

            return new Turn
            {
                Id = _id,
                GameId = _gameId,
                Game = gameRef,
                RoundId = _roundId,
                Round = roundRef,
                Index = _index,
                Seat = _seat,
                MinLen = _minLen,
                FreeStart = _freeStart,
                StartedAt = _startedAt,
                DueAt = _dueAt,
                Word = _word,
                EndedAt = _endedAt,
                EndReason = _endReason,
            };
        }
    }
}
