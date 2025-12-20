using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Security.Cryptography;
namespace Wuno.Api.Middleware
{
    public sealed class EnsureGuestCookieMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly IDataProtector _prot;
        private readonly ILogger<EnsureGuestCookieMiddleware> _logger;
        public const string CookieName = "gid";

        public EnsureGuestCookieMiddleware(RequestDelegate next, IDataProtectionProvider dp, ILogger<EnsureGuestCookieMiddleware> logger)
        {
            _next = next;
            _prot = dp.CreateProtector("guest-id-v1");
            _logger = logger;
        }

        public async Task Invoke(HttpContext ctx)
        {
            // Auth'd users need no guest token
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                await _next(ctx);
                return;
            }

            byte[]? rawToken = null;
            bool needsNewCookie = false;

            // Try to read and decrypt existing cookie
            if (ctx.Request.Cookies.TryGetValue(CookieName, out var cookie) && !string.IsNullOrEmpty(cookie))
            {
                try
                {
                    rawToken = _prot.Unprotect(WebEncoders.Base64UrlDecode(cookie));
                }
                catch (CryptographicException ex)
                {
                    // Cookie decryption failed - likely due to key rotation after deploy
                    _logger.LogWarning("Guest cookie decryption failed (likely key rotation): {Message}", ex.Message);
                    needsNewCookie = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Guest cookie validation failed: {Message}", ex.Message);
                    needsNewCookie = true;
                }
            }
            else
            {
                // No cookie exists
                needsNewCookie = true;
            }

            // Generate new cookie if needed
            if (needsNewCookie || rawToken == null)
            {
                rawToken = new byte[16];
                RandomNumberGenerator.Fill(rawToken);
                var protectedBytes = _prot.Protect(rawToken);
                var value = WebEncoders.Base64UrlEncode(protectedBytes);
                
                ctx.Response.Cookies.Append(CookieName, value, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    Path = "/"  // Ensure cookie is sent on all paths
                });
                
                _logger.LogDebug("Generated new guest cookie");
            }

            // Add guest identity claim
            var id = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            id.AddClaim(new Claim("guest-token", WebEncoders.Base64UrlEncode(rawToken)));
            ctx.User.AddIdentity(id);

            await _next(ctx);
        }
    }
}
