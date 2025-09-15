using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wuno.domain
{

    public sealed class Game
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public GameStatus Status { get; set; } = GameStatus.ACTIVE;
        public int TargetWins { get; set; } = 2;
        public int NextSeat { get; set; } = 0;
        public int Direction { get; set; } = 1; // 1 for clockwise, -1 for counter-clockwise
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public List<Player> Players { get; } = new();
        public List<Round> Rounds { get; } = new();
        public List<Turn> Turns { get; } = new();
        public List<Effect> Effects { get; } = new();
    }

    public sealed class Player
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid GameId { get; set; }
        public int Seat { get; set; }
        public int RoundWins { get; set; }
        public string? LastWord { get; set; }
        public int LastWordLength { get; set; }
    }

    public sealed class  Round
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid GameId { get; set; }
        public int Index { get; set; }
        public bool Active { get; set; } = true;
        public Guid? WinnerId { get; set; }
        public DateTime? StartedAt { get; init; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public Game Game { get; set; } = null!;

    }
    public sealed class Turn
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid GameId { get; set; }
        public Guid RoundId { get; set; }
        public int Index { get; set; }
        public int Seat { get; set; }
        public char? StartLetter { get; set; } // null = any / free-start
        public int MinLen { get; set; } = 1;
        public bool Require2Vowels { get; set; }
        public bool FreeStart { get; set; }
        public DateTime StartedAt { get; set; }
        public int DurationSec { get; set; }
        public string? Word { get; set; }
        public int? WordLen { get; set; }
        public DateTime? EndedAt { get; set; }
        public TurnEndReason? EndReason { get; set; }

        public Game Game { get; set; } = null!;
        public Round Round { get; set; } = null!;
    }
    public sealed class Effect
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid GameId { get; set; }
        public Guid PlayerId { get; set; }
        public int AppliesOn { get; set; } // recipient’s personal next turn index
        public EffectType Type { get; set; }
        public int Value { get; set; } // seconds or +/- length; bool as 0/1
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public Game Game { get; set; } = null!;
    }

}
