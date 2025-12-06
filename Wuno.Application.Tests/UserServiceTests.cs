using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Wuno.Application.Users;
using Wuno.Testing.Fixtures;
using wuno.domain;
using wuno.infrastructure;

public sealed class UserServiceTests
{
    private static NoEmailUserService CreateService(AppDbContext db)
    {
        return new NoEmailUserService(db, new PasswordHasher<User>());
    }

    [Fact]
    public async Task CreateUserAsync_rejects_duplicate_email_and_name()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var existing = new User { Name = "Alice", NameNormalized = "ALICE", Email = "a@test.com", EmailNormalized = "A@TEST.COM" };
        using var db = factory.CreateContext(ctx => ctx.Add(existing));
        var service = CreateService(db);

        var response = await service.CreateUserAsync("Alice", null, "a@test.com", CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Null(response.UserId);
        Assert.Contains("already in use", response.Msg, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task CreateUserAsync_trims_values_and_succeeds_for_new_user()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        using var db = factory.CreateContext();
        var service = CreateService(db);

        var response = await service.CreateUserAsync("  Bob  ", "  icon ", "  bob@example.com  ", CancellationToken.None);

        Assert.True(response.Ok);
        Assert.NotNull(response.UserId);
        var saved = await db.Users.SingleAsync();
        Assert.Equal("Bob", saved.Name);
        Assert.Equal("icon", saved.IconUrl);
        Assert.Equal("bob@example.com", saved.Email);
    }

    [Fact]
    public async Task GetUserAsync_returns_not_found_for_missing_user()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        using var db = factory.CreateContext();
        var service = CreateService(db);

        var response = await service.GetUserAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(response.Ok);
        Assert.Null(response.UserId);
        Assert.Equal("User not Found", response.Msg);
    }

    [Fact]
    public async Task EditAnonUserAsync_detects_duplicate_name_even_with_concurrent_callers()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var existing = new User { Name = "Existing", NameNormalized = "EXISTING" };
        var target = new User { Name = "Target", NameNormalized = "TARGET" };
        var (db1, db2) = factory.CreateConcurrentPair(ctx => ctx.AddRange(existing, target));
        var svc1 = CreateService(db1);
        var svc2 = CreateService(db2);

        var task1 = svc1.EditAnonUserAsync(target.Id, "Existing", null, null, CancellationToken.None);
        var task2 = svc2.EditAnonUserAsync(target.Id, "Existing", null, null, CancellationToken.None);

        var results = await Task.WhenAll(task1, task2);

        Assert.All(results, r => Assert.False(r.Ok));
        Assert.All(results, r => Assert.Contains("Username is already in use", r.Msg));
    }

    [Fact]
    public async Task RegisterAccountAsync_requires_password_and_prevents_duplicate_email()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var anon = new User { Name = "Anon", NameNormalized = "ANON" };
        var other = new User { Name = "Other", NameNormalized = "OTHER", Email = "taken@test.com", EmailNormalized = "taken@test.com" };
        using var db = factory.CreateContext(ctx => ctx.AddRange(anon, other));
        var service = CreateService(db);

        var missingPassword = await service.RegisterAccountAsync(anon.Id, "user", "", null, null, CancellationToken.None);
        Assert.False(missingPassword.Ok);
        Assert.Equal("Password is required.", missingPassword.Msg);

        var duplicateEmail = await service.RegisterAccountAsync(anon.Id, "user", "pass", "taken@test.com", null, CancellationToken.None);
        Assert.False(duplicateEmail.Ok);
        Assert.Contains("Email is already in use", duplicateEmail.Msg);
    }
}