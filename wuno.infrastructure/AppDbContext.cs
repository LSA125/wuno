using Microsoft.EntityFrameworkCore;
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

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Game>().HasMany(g => g.Players).WithOne().HasForeignKey(p => p.GameId);
            b.Entity<Game>().HasMany(g => g.Rounds).WithOne(r => r.Game).HasForeignKey(r => r.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Game>().HasMany(g => g.Turns).WithOne(t => t.Game).HasForeignKey(t => t.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Game>().HasMany(g => g.Effects).WithOne(e => e.Game).HasForeignKey(e => e.GameId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<User>().HasOne(u => u.ActivePlayer).WithOne(p => p.User).HasForeignKey<Player>(p => p.UserId).OnDelete(DeleteBehavior.SetNull);
            b.Entity<Effect>().HasOne(e => e.Round).WithMany().OnDelete(DeleteBehavior.Restrict);
            b.Entity<Effect>().HasOne(e => e.Player).WithMany().OnDelete(DeleteBehavior.Restrict);

            b.Entity<Turn>().HasOne(t => t.Round).WithMany().HasForeignKey(t => t.RoundId).OnDelete(DeleteBehavior.Restrict);

            int finished = (int)GameStatus.FINISHED;
            b.Entity<Game>().HasIndex(g => g.Code).IsUnique().HasFilter("[Status] <> " + finished + " AND [Code] IS NOT NULL AND CODE <> ''");
            b.Entity<Player>().HasIndex(p => new { p.GameId, p.Seat }).IsUnique();
            b.Entity<Round>().HasIndex(r => new { r.GameId, r.Index });
            b.Entity<Turn>().HasIndex(t => new { t.GameId, t.Index });

            b.Entity<Turn>().Property(t => t.RowVersion).IsRowVersion();
        }
    }
}
