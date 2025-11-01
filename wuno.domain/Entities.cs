using System.ComponentModel.DataAnnotations;

namespace wuno.domain
{
    public sealed class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string? IconUrl { get; set; }

        public string? Email { get; set; }
        public string? EmailNormalized { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }

        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

        public Guid? ActivePlayerId { get; set; }
        public Player? ActivePlayer { get; set; }
    }
    public sealed class EmailVerificationToken
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;
        public string TokenHash { get; set; } = default!;
        public DateTime ExpiresAt { get; init; }
        public DateTime UsedAt { get; set; }
    }
    public sealed class Game
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Code { get; set; } = "";
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
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string Name { get; set; } = "";
        public string? IconUrl { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsConnected { get; set; }
        public bool IsTaken { get; set; }
        public int Seat { get; set; }
        public int RoundWins { get; set; }
        public string? LastWord { get; set; }
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
        public Game Game { get; set; } = null!;
        public Round Round { get; set; } = null!;

        [Timestamp]
        public byte[] RowVersion { get; set; } = default!;
        public int Index { get; set; }
        public int Seat { get; set; }
        public char? StartLetter { get; set; } // null = any / free-start
        public int MinLen { get; set; } = 1;
        public bool FreeStart { get; set; }
        public DateTime StartedAt { get; set; }
        public int DurationSec { get; set; }
        public DateTime DueAt { get; set; }
        public string? Word { get; set; }
        public int? WordLen { get; set; }
        public DateTime? EndedAt { get; set; }
        public TurnEndReason? EndReason { get; set; }
    }
    public sealed class Effect
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public Guid GameId { get; set; }
        public Game Game { get; set; } = null!;

        public Guid RoundId { get; set; }
        public Round Round { get; set; } = null!;

        public Guid PlayerId { get; set; }
        public Player Player { get; set; } = null!;

        public int AppliesOn { get; set; }
        public EffectType Type { get; set; }
        public int Value { get; set; }

        // audit/idempotency:
        public Guid? SourceTurnId { get; set; }
        public Guid? ConsumedTurnId { get; set; }

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
}
