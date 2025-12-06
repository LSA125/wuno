using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using wuno.infrastructure;

namespace Wuno.Testing.Fixtures
{
    public sealed class SqliteInMemoryAppDbContextFactory : IDisposable
    {
        private readonly DbConnection _connection;

        public SqliteInMemoryAppDbContextFactory()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            using var ctx = CreateContext();
            ctx.Database.EnsureCreated(); // Create schema once
        }

        public AppDbContext CreateContext(Action<AppDbContext>? seed = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new AppDbContext(options);

            if (seed is not null)
            {
                seed(ctx);
                ctx.SaveChanges();
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

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}