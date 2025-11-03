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
        public const string CookieName = "gid";

        public EnsureGuestCookieMiddleware(RequestDelegate next, IDataProtectionProvider dp)
        {
            _next = next;
            _prot = dp.CreateProtector("guest-id-v1");
        }

        public async Task Invoke(HttpContext ctx)
        {
            // Auth'd users need no guest token
            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                await _next(ctx);
                return;
            }

            // Ensure we have a stable opaque token in an HttpOnly cookie
            if (!ctx.Request.Cookies.TryGetValue(CookieName, out var cookie))
            {
                var token = new byte[16];
                RandomNumberGenerator.Fill(token);
                var protectedBytes = _prot.Protect(token.ToArray());
                var value = WebEncoders.Base64UrlEncode(protectedBytes);

                ctx.Response.Cookies.Append(CookieName, value, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });

                cookie = value;
            }

            // Flow the token as a claim so controllers/hubs can read it
            try
            {
                var raw = _prot.Unprotect(WebEncoders.Base64UrlDecode(cookie!));
                var id = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                id.AddClaim(new Claim("guest-token", WebEncoders.Base64UrlEncode(raw))); // keep raw token b64url
                ctx.User.AddIdentity(id);
            }
            catch { }

            await _next(ctx);
        }
    }
}
