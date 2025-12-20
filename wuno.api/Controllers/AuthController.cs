using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using wuno.domain;
using wuno.infrastructure;
using Microsoft.EntityFrameworkCore;
using Wuno.Application.Games;
using Wuno.Domain.Rules;
using Wuno.Application.Games.Util;
using Wuno.Api.Services;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ITokenService _tokenService;
    
    public AuthController(AppDbContext db, IPasswordHasher<User> hasher, ITokenService tokenService)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    public record LoginRequest(string Username, string Password);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { msg = "Username and password required." });

        var username = req.Username.Trim();
        var norm = Name.normalize(username);
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.IsRegistered && u.NameNormalized == norm, ct);

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

        // Generate access token for mobile fallback
        var accessToken = _tokenService.GenerateToken(user.Id, user.Name, isRegistered: true);

        return Ok(new AuthResponse(
            Ok: true,
            UserId: user.Id,
            Name: user.Name,
            IconUrl: user.IconUrl,
            Email: user.Email,
            Msg: null,
            AccessToken: accessToken));
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

        // Generate fresh token for session
        var accessToken = _tokenService.GenerateToken(u.Id, u.Name, isRegistered: u.IsRegistered);

        return Ok(new AuthResponse(
            Ok: true,
            UserId: u.Id,
            Name: u.Name,
            IconUrl: u.IconUrl,
            Email: u.Email,
            Msg: null,
            AccessToken: accessToken));
    }

    /// <summary>
    /// Validate an access token and return user info. Used for session restoration on mobile.
    /// </summary>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest req, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(req.Token))
            return BadRequest(new { msg = "Token required." });

        var userId = _tokenService.GetUserIdFromToken(req.Token);
        if (!userId.HasValue)
            return Unauthorized(new { msg = "Invalid or expired token." });

        var u = await _db.Users.FindAsync([userId.Value], ct);
        if (u is null)
            return Unauthorized(new { msg = "User not found." });

        // Generate fresh token
        var freshToken = _tokenService.GenerateToken(u.Id, u.Name, isRegistered: u.IsRegistered);

        return Ok(new AuthResponse(
            Ok: true,
            UserId: u.Id,
            Name: u.Name,
            IconUrl: u.IconUrl,
            Email: u.Email,
            Msg: null,
            AccessToken: freshToken));
    }

    public record ValidateTokenRequest(string Token);
    public record RegisterRequest(Guid? TempUserId, string Username, string Password, string? Email, string? IconUrl);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { msg = "Username and password are required." });

        var username = req.Username.Trim();
        var norm = Name.normalize(username);

        var exists = await _db.Users.AnyAsync(
            u => u.NameNormalized == norm, ct);
        if (exists) return Conflict(new { msg = "Username is already taken." });

        User? user;
        // upgrading guest vs creating new…
        if (req.TempUserId is Guid tempId)
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Id == tempId, ct);
            if (user is null)
                return BadRequest(new { msg = "Temporary user not found." });

            user.IsRegistered = true;
            user.Name = username;
            user.NameNormalized = norm;
            user.PasswordHash = _hasher.HashPassword(user, req.Password);
            user.Email = string.IsNullOrWhiteSpace(req.Email) ? user.Email : req.Email!.Trim();
            user.IconUrl = string.IsNullOrWhiteSpace(req.IconUrl) ? user.IconUrl : req.IconUrl!.Trim();
        }
        else
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                IsRegistered = true,
                Name = username,
                NameNormalized = norm,
                Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email!.Trim(),
                IconUrl = string.IsNullOrWhiteSpace(req.IconUrl) ? null : req.IconUrl!.Trim(),
                PasswordHash = null // set below
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

        // Generate access token for mobile fallback
        var accessToken = _tokenService.GenerateToken(user.Id, user.Name, isRegistered: true);

        return Ok(new AuthResponse(
            Ok: true,
            UserId: user.Id,
            Name: user.Name,
            IconUrl: user.IconUrl,
            Email: user.Email,
            Msg: null,
            AccessToken: accessToken));
    }
}
