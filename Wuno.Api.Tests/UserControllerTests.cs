using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Wuno.Api.Controllers;
using Wuno.Application.Games.Util;
using Wuno.Application.Users;

public sealed class UserControllerTests
{
    [Fact]
    public async Task GetRegistered_RejectsUnregisteredUser()
    {
        var svc = new FakeUserService
        {
            GetUserResponse = new UserResponse(true, Guid.NewGuid(), "Anon", null, null, null)
        };
        var controller = new UserController(svc);

        var result = await controller.GetRegistered(Guid.NewGuid(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = Assert.IsType<UserResponse>(badRequest.Value);
        Assert.Contains("not registered", payload.Msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EditAnon_ReturnsUnauthorizedWhenResolverMissing()
    {
        var svc = new FakeUserService();
        var controller = new UserController(svc);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection()
                    .AddSingleton<IAppUserResolver>(new MissingUserResolver())
                    .BuildServiceProvider()
            }
        };

        var result = await controller.EditAnon(new TmpUserRequest(Guid.NewGuid(), "anon", null, null), new MissingUserResolver(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var payload = Assert.IsType<UserResponse>(unauthorized.Value);
        Assert.Contains("No identity", payload.Msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EditRegistered_ReturnsUnauthorizedWhenNoClaim()
    {
        var svc = new FakeUserService();
        var controller = new UserController(svc);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var result = await controller.EditRegistered(new RegUserRequest(Guid.NewGuid(), "pwd", "name", null, null), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var payload = Assert.IsType<UserResponse>(unauthorized.Value);
        Assert.Contains("Not signed in", payload.Msg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_ReturnsNotFoundWhenServiceFails()
    {
        var svc = new FakeUserService
        {
            GetUserResponse = new UserResponse(false, null, null, null, null, "missing")
        };
        var controller = new UserController(svc);

        var result = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    private sealed class FakeUserService : IUserService
    {
        public UserResponse? GetUserResponse { get; set; }
        public UserResponse? CreateUserResponse { get; set; }
        public UserResponse? EditAnonResponse { get; set; }
        public UserResponse? EditRegisteredResponse { get; set; }

        public Task<UserResponse> CreateUserAsync(string name, string? icon, string? email, CancellationToken ct)
            => Task.FromResult(CreateUserResponse ?? new UserResponse(false, null, null, null, null, "nope"));

        public Task<UserResponse> EditAnonUserAsync(Guid userId, string? name, string? iconUrl, string? email, CancellationToken ct)
            => Task.FromResult(EditAnonResponse ?? new UserResponse(false, null, null, null, null, "edit anon failed"));

        public Task<UserResponse> EditRegisteredUserAsync(Guid userId, string pass, string? name, string? iconUrl, string? email, CancellationToken ct)
            => Task.FromResult(EditRegisteredResponse ?? new UserResponse(false, null, null, null, null, "edit reg failed"));

        public Task<UserResponse> GetUserAsync(Guid userId, CancellationToken ct)
            => Task.FromResult(GetUserResponse ?? new UserResponse(false, null, null, null, null, "missing"));

        public Task<UserResponse> RegisterAccountAsync(Guid token, string username, string password, string? email, string? iconUrl, CancellationToken ct)
            => Task.FromResult(new UserResponse(false, null, null, null, null, "not used"));

        public Task VerifyEmailAsync(Guid token, string verificationCode, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class MissingUserResolver : IAppUserResolver
    {
        public bool TryGet(out Guid userId)
        {
            userId = default;
            return false;
        }
    }
}