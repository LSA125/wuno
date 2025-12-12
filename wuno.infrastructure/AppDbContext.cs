using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using wuno.domain;
namespace wuno.infrastructure
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Game> Games => Set<Game>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Round> Rounds => Set<Round>();
        public DbSet<Turn> Turns => Set<Turn>();
        public DbSet<Effect> Effects => Set<Effect>();
        public DbSet<User> Users => Set<User>();
        public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Game>().HasMany(g => g.Rounds).WithOne(r => r.Game).HasForeignKey(r => r.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Game>().HasMany(g => g.Turns).WithOne(t => t.Game).HasForeignKey(t => t.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Game>().HasMany(g => g.Effects).WithOne(e => e.Game).HasForeignKey(e => e.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Game>().HasOne(g => g.CurrentRound).WithMany().OnDelete(DeleteBehavior.Restrict);
            b.Entity<Game>().HasOne(g => g.CurrentTurn).WithMany().OnDelete(DeleteBehavior.Restrict);
            b.Entity<Player>().HasOne(p => p.Game).WithMany(g => g.Players).HasForeignKey(p => p.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<User>().HasOne(u => u.ActivePlayer).WithOne(p => p.User).HasForeignKey<Player>(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
            b.Entity<Effect>().HasOne(e => e.Round).WithMany().OnDelete(DeleteBehavior.Restrict);
            b.Entity<EmailVerificationToken>().HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

            b.Entity<Turn>().HasOne(t => t.Round).WithMany().HasForeignKey(t => t.RoundId).OnDelete(DeleteBehavior.Restrict);

            int finished = (int)GameStatus.FINISHED;

            //force unique game code for active games:
            b.Entity<Game>().HasIndex(g => g.Code).IsUnique().HasFilter("[Status] <> " + finished + " AND [Code] IS NOT NULL AND CODE <> ''");
            //for finding player seats fast/uniqueness
            b.Entity<Player>().HasIndex(p => new { p.GameId, p.Seat }).IsUnique();
            //find the most recent round/turn fast
            b.Entity<Round>().HasIndex(r => new { r.GameId, r.Index });
            b.Entity<Turn>().HasIndex(t => new { t.GameId, t.Index });
            //ensure words are unique per round
            b.Entity<Turn>().HasIndex(t => new {t.RoundId, t.Word}).IsUnique().HasFilter("[RoundId] IS NOT NULL AND [Word] IS NOT NULL AND [Word] <> ''");
            //find the players effects fast
            b.Entity<Effect>().HasIndex(e => new {e.RoundId, e.TargetSeat, e.AppliesOnTurn });
            b.Entity<User>().HasIndex(u => u.EmailNormalized).IsUnique().HasFilter("[EmailNormalized] IS NOT NULL AND [EmailNormalized] <> ''");
            b.Entity<User>().HasIndex(u => u.NameNormalized).IsUnique().HasFilter("[NameNormalized] IS NOT NULL AND [NameNormalized] <> ''");
            b.Entity<EmailVerificationToken>().HasIndex(t => new {t.UserId, t.TokenHash}).IsUnique();

            ApplyUtcDateTimeConverters(b);
        }

        private static void ApplyUtcDateTimeConverters(ModelBuilder builder)
        {
            static DateTime Normalize(DateTime value) => value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            ValueConverter<DateTime, DateTime> utcConverter = new(
                to => Normalize(to),
                from => DateTime.SpecifyKind(from, DateTimeKind.Utc));

            ValueConverter<DateTime?, DateTime?> nullableUtcConverter = new(
                to => to is null ? null : Normalize(to.Value),
                from => from is null ? null : DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableUtcConverter);
                    }
                }
            }
        }
    }
}
