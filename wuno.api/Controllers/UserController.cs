using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Wuno.Application.Games;
using Wuno.Application.Users;
namespace Wuno.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class UserController(IUserService us) : ControllerBase
    {
        private readonly IUserService _us = us;

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var res = await _us.GetUserAsync(id, ct);
            return res.Ok ? Ok(res) : NotFound();
        }
        [HttpGet("/reg/{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRegistered(Guid id, CancellationToken ct)
        {
            var res = await _us.GetUserAsync(id, ct);
            if (!res.Ok)
                return NotFound();
            if (res.Email == null)
                return BadRequest(new UserResponse(false, null, null, null, null, "User is not registered."));
            return Ok(res);
        }
        [HttpPost("new")]
        [AllowAnonymous]
        public async Task<IActionResult> New([FromBody] TmpUserRequest req, CancellationToken ct)
        {
            var res = await _us.CreateUserAsync(req.Name, req.IconUrl, req.Email, ct);
            return res.Ok ? Ok(res) : BadRequest(res);
        }
        [HttpPost("register/{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegUserRequest req, CancellationToken ct)
        {
            if(req.Name == null || req.Pass == null)
                return BadRequest(new UserResponse(false, null, null, null, null, "Username and password are required for registration."));
            var res = await _us.RegisterAccountAsync(req.UserId, req.Name, req.Pass, req.Email, req.IconUrl, ct);
            return res.Ok ? Ok(res) : BadRequest(res);
        }
        [HttpPost("edit/anon/{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> EditAnon([FromBody] TmpUserRequest req, CancellationToken ct)
        {
            var res = await _us.EditAnonUserAsync(req.UserId, req.Name, req.IconUrl, req.Email, ct);
            return res.Ok ? Ok(res) : BadRequest(res);
        }
        [HttpPost("edit/registered")]
        public async Task<IActionResult> EditRegistered([FromBody] RegUserRequest req, CancellationToken ct)
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
                return Unauthorized(new UserResponse(false, null, null, null, null, "Not signed in."));

            var res = await _us.EditRegisteredUserAsync(userId, req.Pass, req.Name, req.IconUrl, req.Email, ct);
            return res.Ok ? Ok(res) : BadRequest(res);
        }

    }
}
