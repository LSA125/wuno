using wuno.infrastructure;

namespace Wuno.Testing.Fixtures
{
    public static class TestDataSeeder
    {
        public static void Seed(AppDbContext ctx, params object[] entities)
        {
            ctx.AddRange(entities);
            ctx.SaveChanges();
        }

        public static async Task SeedAsync(AppDbContext ctx, params object[] entities)
        {
            await ctx.AddRangeAsync(entities);
            await ctx.SaveChangesAsync();
        }
    }
}
