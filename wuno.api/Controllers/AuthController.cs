using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using wuno.domain;
using wuno.infrastructure;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    public AuthController(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db; _hasher = hasher;
    }

    public record LoginRequest(string Username, string Password);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { msg = "Username and password required." });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == req.Username, ct);

        if (user is null)
            return Unauthorized(new { msg = "Invalid username or password." });

        var ok = _hasher.VerifyHashedPassword(user, user.PasswordHash!, req.Password);
        if (ok == PasswordVerificationResult.Failed)
            return Unauthorized(new { msg = "Invalid username or password." });

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name ?? string.Empty)
        };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(id));

        return Ok(new { ok = true });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { ok = true });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized();
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idStr, out var userId)) return Unauthorized();

        var u = await _db.Users.FindAsync([userId], ct);
        if (u is null) return Unauthorized();

        return Ok(new
        {
            ok = true,
            userId = u.Id,
            name = u.Name,
            iconUrl = u.IconUrl,
            email = u.Email
        });
    }

    public record RegisterRequest(Guid? TempUserId, string Username, string Password, string? Email, string? IconUrl);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { msg = "Username and password are required." });

        // normalize username (optional)
        var username = req.Username.Trim();

        // enforce unique username (and maybe unique email if you want)
        var exists = await _db.Users.AnyAsync(u => u.Name == username, ct);
        if (exists) return Conflict(new { msg = "Username is already taken." });

        User user;

        if (req.TempUserId is Guid tempId)
        {
            // Upgrade existing temp user
            var maybeUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == tempId, ct);
            if (maybeUser is null)
                return NotFound(new { msg = "Temp user not found." });
            user = maybeUser;
            user.Name = username;
            user.Email = string.IsNullOrWhiteSpace(req.Email) ? user.Email : req.Email!.Trim();
            user.IconUrl = string.IsNullOrWhiteSpace(req.IconUrl) ? user.IconUrl : req.IconUrl!.Trim();
            user.PasswordHash = _hasher.HashPassword(user, req.Password);
        }
        else
        {
            // Directly create a new registered user
            user = new User
            {
                Id = Guid.NewGuid(),
                Name = username,
                Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email!.Trim(),
                IconUrl = string.IsNullOrWhiteSpace(req.IconUrl) ? null : req.IconUrl!.Trim(),
                // other defaults…
            };
            user.PasswordHash = _hasher.HashPassword(user, req.Password);
            _db.Users.Add(user);
        }

        await _db.SaveChangesAsync(ct);

        // Sign in via cookie
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
    };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));

        return Ok(new
        {
            ok = true,
            userId = user.Id,
            name = user.Name,
            iconUrl = user.IconUrl,
            email = user.Email
        });
    }
}
