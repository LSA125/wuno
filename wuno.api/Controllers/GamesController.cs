using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using wuno.domain;
using Wuno.Application;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Games.Util;
using Wuno.Application.Users;
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
        [HttpGet("active-for-current")]
        [AllowAnonymous]
        public async Task<IActionResult> ActiveForCurrent([FromServices] IAppUserResolver who, CancellationToken ct)
        {
            if (!who.TryGet(out var userId))
                return Ok(new GameCodeResponse(false, null, null));
            try
            {
                return Ok(await _svc.GetUserActiveGameCodeAsync(userId, ct));
            }
            catch
            {
                return BadRequest(new GameCodeResponse(false, null, null));
            }
        }

        [HttpPost("matchmake")]
        [AllowAnonymous]
        public async Task<IActionResult> Matchmake(CancellationToken ct)
        {
            var result = await _svc.FindOrCreatePublicGameAsync(ct);
            return Ok(result);
        }

        /// <summary>
        /// Beacon-based leave for reliable tab close notification.
        /// Used by sendBeacon in beforeunload - body may be empty.
        /// </summary>
        [HttpPost("leave")]
        [AllowAnonymous]
        public async Task<IActionResult> Leave([FromServices] IAppUserResolver who, CancellationToken ct)
        {
            if (!who.TryGet(out var userId))
                return Ok(); // Silent fail for beacon - no auth means nothing to clean up
            
            try
            {
                await _svc.LeaveGameAsync(userId, ct);
            }
            catch
            {
                // Best effort - don't fail the beacon request
            }
            return Ok();
        }
    }
}
