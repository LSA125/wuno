using System.ComponentModel.DataAnnotations;

namespace wuno.domain
{
    public sealed class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string? Name { get; set; }
        public string? NameNormalized { get; set; }
        public string? IconUrl { get; set; }

        public string? Email { get; set; }
        public string? EmailNormalized { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }

        public string? PasswordHash { get; set; }
        public bool IsRegistered { get; set; } = false;
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
        public int CurSeat { get; set; } = 0;
        public int Direction { get; set; } = 1; // 1 for clockwise, -1 for counter-clockwise
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public List<Player> Players { get; } = new();
        public List<Round> Rounds { get; } = new();
        public List<Turn> Turns { get; } = new();
        public Turn? CurrentTurn { get; set; }
        public Round? CurrentRound { get; set; }
        public string? LastWord { get; set; }
    }

    public sealed class Player
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid GameId { get; set; }
        public Game Game { get; set; } = null!;
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string Name { get; set; } = "";
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsConnected { get; set; }
        public bool IsTaken { get; set; }
        public int Seat { get; set; }
        public int RoundWins { get; set; }
        public int TurnsPlayedThisRound { get; set; }
        public string? LastWord { get; set; }
    }

    public sealed class  Round
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid GameId { get; set; }
        public int Index { get; set; }
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

        public int Index { get; set; }
        public int Seat { get; set; }
        public int MinLen { get; set; } = 0;
        public DateTime StartedAt { get; set; }
        public DateTime DueAt { get; set; }
        public string? Word { get; set; }
        public DateTime? EndedAt { get; set; }
        public TurnEndReason? EndReason { get; set; }
        public int Score { get; set; } = 0;
    }
}
