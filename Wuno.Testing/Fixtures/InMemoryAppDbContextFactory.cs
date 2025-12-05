using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using wuno.infrastructure;

namespace Wuno.Testing.Fixtures
{
    public sealed class InMemoryAppDbContextFactory
    {
        private readonly string _databaseName;
        private readonly InMemoryDatabaseRoot _databaseRoot;
        private readonly object _seedLock = new();

        public InMemoryAppDbContextFactory(string? databaseName = null, InMemoryDatabaseRoot? root = null)
        {
            _databaseName = databaseName ?? Guid.NewGuid().ToString();
            _databaseRoot = root ?? new InMemoryDatabaseRoot();
        }

        public AppDbContext CreateContext(Action<AppDbContext>? seed = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_databaseName, _databaseRoot)
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new AppDbContext(options);

            if (seed is not null)
            {
                lock (_seedLock)
                {
                    seed(ctx);
                    ctx.SaveChanges();
                }
            }

            return ctx;
        }

        public AppDbContext CreateContextWithSeed(params object[] entities)
        {
            return CreateContext(ctx => ctx.AddRange(entities));
        }

        public (AppDbContext First, AppDbContext Second) CreateConcurrentPair(Action<AppDbContext>? seed = null)
        {
            return (CreateContext(seed), CreateContext());
        }
    }
}
