// Controllers/GuestsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Wuno.Api.Middleware; // CookieName
using Wuno.Infrastructure; // AppDbContext
using wuno.domain;         // User entity
using Wuno.Application.Users;
using Wuno.Application.Games;
using wuno.infrastructure;

[ApiController]
[Route("api/guests")]
[AllowAnonymous]
public sealed class GuestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IDataProtector _prot;

    public GuestsController(AppDbContext db, IDataProtectionProvider dp)
    {
        _db = db;
        _prot = dp.CreateProtector("guest-id-v1");
    }

    public sealed record EnsureGuestReq(string Name, string? Email, string? IconUrl);

    [HttpPost("ensure")]
    public async Task<IActionResult> Ensure([FromBody] EnsureGuestReq body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { msg = "Name required." });

        Guid userId;

        // If already signed-in (maybe by AutoGuestSignIn), reuse that user
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var parsed))
        {
            userId = parsed;
        }
        else
        {
            // Create a brand-new guest user
            var uNew = new User
            {
                Id = Guid.NewGuid(),
                Name = body.Name.Trim(),
                Email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim(),
                IconUrl = string.IsNullOrWhiteSpace(body.IconUrl) ? null : body.IconUrl.Trim(),
                // PasswordHash remains null for guest
            };
            _db.Users.Add(uNew);
            await _db.SaveChangesAsync(ct);
            userId = uNew.Id;

            // Set gid cookie to this userId (HttpOnly, signed)
            var buf = new byte[16];
            userId.TryWriteBytes(buf);
            var token = WebEncoders.Base64UrlEncode(_prot.Protect(buf));
            Response.Cookies.Append(EnsureGuestCookieMiddleware.CookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                IsEssential = true
            });
        }

        // Update the guest's display info (not unique)
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (u is null) return Unauthorized(new { msg = "Guest session invalid." });

        u.Name = body.Name.Trim();
        if (!string.IsNullOrWhiteSpace(body.Email)) u.Email = body.Email.Trim();
        if (!string.IsNullOrWhiteSpace(body.IconUrl)) u.IconUrl = body.IconUrl.Trim();
        await _db.SaveChangesAsync(ct);

        // Ensure they are signed-in via cookie (so hub sees a user)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Name, u.Name ?? string.Empty),
            new Claim("kind","guest"),
        };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));

        return Ok(new UserResponse(true, u.Id, u.Name, u.IconUrl, u.Email, null));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();

        var u = await _db.Users.FindAsync([userId], ct);
        if (u is null) return NotFound();

        return Ok(new UserResponse(true, u.Id, u.Name, u.IconUrl, u.Email, null));
    }
}
