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
        private int _minLen = 0;
        private DateTime _startedAt = DateTime.UtcNow;
        private DateTime _dueAt = DateTime.UtcNow.AddSeconds(30);
        private string? _word;
        private DateTime? _endedAt;
        private TurnEndReason? _endReason;
        private int _score = 0;

        public TurnBuilder WithId(Guid id) { _id = id; return this; }
        public TurnBuilder WithGame(Game game) { _game = game; _gameId = game.Id; return this; }
        public TurnBuilder WithGameId(Guid id) { _gameId = id; return this; }
        public TurnBuilder WithRound(Round round) { _round = round; _roundId = round.Id; return this; }
        public TurnBuilder WithRoundId(Guid id) { _roundId = id; return this; }
        public TurnBuilder WithIndex(int index) { _index = index; return this; }
        public TurnBuilder AtSeat(int seat) { _seat = seat; return this; }
        public TurnBuilder MinLength(int minLen) { _minLen = minLen; return this; }
        public TurnBuilder StartedAt(DateTime start) { _startedAt = start; return this; }
        public TurnBuilder DueAt(DateTime due) { _dueAt = due; return this; }
        public TurnBuilder WithWord(string? word) { _word = word; return this; }
        public TurnBuilder EndedAt(DateTime? end) { _endedAt = end; return this; }
        public TurnBuilder WithEndReason(TurnEndReason? reason) { _endReason = reason; return this; }
        public TurnBuilder WithScore(int score) { _score = score; return this; }

        public Turn Build()
        {
            var gameRef = _game ?? new Game { Id = _gameId };
            var roundRef = _round
                ?? gameRef.Rounds.FirstOrDefault(r => r.Id == _roundId)
                ?? gameRef.CurrentRound
                ?? gameRef.Rounds.FirstOrDefault();

            if (roundRef is null)
            {
                roundRef = new Round { Id = _roundId, GameId = _gameId, Game = gameRef };
            }
            else
            {
                _roundId = roundRef.Id;
                _gameId = roundRef.GameId == default ? _gameId : roundRef.GameId;

                roundRef.GameId = _gameId;
                roundRef.Game = gameRef;
            }

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
                StartedAt = _startedAt,
                DueAt = _dueAt,
                Word = _word,
                EndedAt = _endedAt,
                EndReason = _endReason,
                Score = _score,
            };
        }
    }
}
