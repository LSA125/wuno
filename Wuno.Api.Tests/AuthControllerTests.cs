using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using wuno.domain;
using wuno.domain.Rules;
using wuno.infrastructure;
using Wuno.Domain.Rules;
using Wuno.Testing.Fixtures;
using Wuno.Api.Services;

public sealed class AuthControllerTests
{
    private static AuthController CreateController(AppDbContext db, FakeAuthService auth)
    {
        var controller = new AuthController(db, new PasswordHasher<User>(), new FakeTokenService());
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(auth);
        var sp = services.BuildServiceProvider();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = sp
            }
        };
        return controller;
    }
    
    private sealed class FakeTokenService : ITokenService
    {
        public string GenerateToken(Guid userId, string? name = null, bool isRegistered = false)
            => $"fake-token-{userId}";
        public ClaimsPrincipal? ValidateToken(string token) => null;
        public Guid? GetUserIdFromToken(string token) => null;
    }

    [Fact]
    public async Task Register_MissingFields_ReturnsBadRequest()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        await using var db = factory.CreateContext();
        var auth = new FakeAuthService();
        var controller = CreateController(db, auth);

        var result = await controller.Register(new AuthController.RegisterRequest(null, " ", "", null, null), CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("required", bad.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsConflict()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        await using var db = factory.CreateContextWithSeed(new User
        {
            Id = Guid.NewGuid(),
            IsRegistered = true,
            Name = "TestUser",
            NameNormalized = Name.normalize("TestUser"),
            PasswordHash = "hash"
        });
        var auth = new FakeAuthService();
        var controller = CreateController(db, auth);

        var result = await controller.Register(new AuthController.RegisterRequest(null, "TestUser", "pass", null, null), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Null(auth.LastPrincipal);
    }

    [Fact]
    public async Task Register_SameNameInParallel_OnlyOneSucceeds()
    {
        var dbFactory = new SqliteInMemoryAppDbContextFactory();
        var authA = new FakeAuthService();
        var authB = new FakeAuthService();

        var (ctxA, ctxB) = dbFactory.CreateConcurrentPair();
        await using var first = ctxA;
        await using var second = ctxB;

        var controllerA = CreateController(first, authA);
        var controllerB = CreateController(second, authB);

        var request = new AuthController.RegisterRequest(null, "RaceUser", "pwd", null, null);
        var taskA = controllerA.Register(request, CancellationToken.None);
        var taskB = controllerB.Register(request, CancellationToken.None);

        var results = await Task.WhenAll(taskA, taskB);

        Assert.Contains(results, r => r is OkObjectResult);
        Assert.Contains(results, r => r is ConflictObjectResult);
        Assert.NotNull(authA.LastPrincipal ?? authB.LastPrincipal);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        var factory = new SqliteInMemoryAppDbContextFactory();
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            IsRegistered = true,
            Name = "tester",
            NameNormalized = Name.normalize("tester"),
        };
        user.PasswordHash = hasher.HashPassword(user, "correct");

        await using var db = factory.CreateContextWithSeed(user);
        var auth = new FakeAuthService();
        var controller = CreateController(db, auth);

        var result = await controller.Login(new AuthController.LoginRequest("tester", "wrong"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Null(auth.LastPrincipal);
    }

    [Fact]
    public async Task Me_WithInvalidPrincipal_ReturnsUnauthorized()
    {
        await using var db = new SqliteInMemoryAppDbContextFactory().CreateContext();
        var auth = new FakeAuthService();
        var controller = CreateController(db, auth);
        controller.ControllerContext.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await controller.Me(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private sealed class FakeAuthService : IAuthenticationService
    {
        public ClaimsPrincipal? LastPrincipal { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            LastPrincipal = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            LastPrincipal = null;
            return Task.CompletedTask;
        }
    }
}