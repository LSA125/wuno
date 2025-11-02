using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using wuno.domain;
using Wuno.Application;
using Wuno.Application.Games;
namespace Wuno.Api.Controllers
{

    [ApiController]
    [Route("api/games")]
    public sealed class GamesController : ControllerBase
    {
        private readonly IGameService _svc;
        public GamesController(IGameService svc)
        {
            _svc = svc;
        }
        [HttpPost("new")]
        [AllowAnonymous]
        public async Task<IActionResult> New([FromBody] NewGameRequest request, CancellationToken cancellationToken)
        {
            NewGameResponse res = await _svc.StartNewGameAsync(request, cancellationToken);

            return Ok(res);
        }
        [HttpPost("id/{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var state = await _svc.GetGameStateAsync(id, ct);
            return state is null ? NotFound() : Ok(state);
        }
        [HttpGet("users/{userId:guid}/active-game")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserActive(Guid userId, CancellationToken ct)
        {
            try
            {
                return Ok(await _svc.GetUserActiveGameCodeAsync(userId, ct));
            }
            catch (Exception ex)
            {
                return BadRequest(new GameCodeResponse(false, null, null));
            }
        }
        [HttpGet("me/active-game")]
        [Authorize] // cookie auth
        public async Task<IActionResult> GetMyActive(CancellationToken ct)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized(new { ok = false, reason = "Not signed in." });
            try
            {
                return Ok(await _svc.GetUserActiveGameCodeAsync(userId, ct));
            }
            catch (Exception ex)
            {
                return BadRequest(new GameCodeResponse(false, null, null));
            }
        }
    }
}
