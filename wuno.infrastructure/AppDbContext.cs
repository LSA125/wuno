using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using wuno.domain;
namespace wuno.infrastructure
{
    public sealed class AppDbContext : DbContext, IDataProtectionKeyContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Game> Games => Set<Game>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<Round> Rounds => Set<Round>();
        public DbSet<Turn> Turns => Set<Turn>();
        public DbSet<User> Users => Set<User>();
        public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
        
        // Data Protection keys - persisted to DB for Azure compatibility
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Game>().HasMany(g => g.Rounds).WithOne(r => r.Game).HasForeignKey(r => r.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Game>().HasMany(g => g.Turns).WithOne(t => t.Game).HasForeignKey(t => t.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Game>().HasOne(g => g.CurrentRound).WithMany().OnDelete(DeleteBehavior.Restrict);
            b.Entity<Game>().HasOne(g => g.CurrentTurn).WithMany().OnDelete(DeleteBehavior.Restrict);
            b.Entity<Game>().HasOne(g => g.Winner).WithMany().HasForeignKey(g => g.WinnerId).OnDelete(DeleteBehavior.NoAction);
            b.Entity<Player>().HasOne(p => p.Game).WithMany(g => g.Players).HasForeignKey(p => p.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<User>().HasOne(u => u.ActivePlayer).WithOne(p => p.User).HasForeignKey<Player>(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
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
            b.Entity<User>().HasIndex(u => u.EmailNormalized).IsUnique().HasFilter("[EmailNormalized] IS NOT NULL AND [EmailNormalized] <> ''");
            b.Entity<User>().HasIndex(u => u.NameNormalized).IsUnique().HasFilter("[NameNormalized] IS NOT NULL AND [NameNormalized] <> ''");
            b.Entity<EmailVerificationToken>().HasIndex(t => new {t.UserId, t.TokenHash}).IsUnique();
            //for matchmaking - find public games with open slots
            b.Entity<Game>().HasIndex(g => new { g.IsPublic, g.Status });
        }
    }
}
