using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.DataProtection;
using Wuno.Api.Middleware;
using Wuno.Api.Services;

public sealed class HubUserIdProvider : IUserIdProvider
{
    private readonly IDataProtector _prot;
    private readonly ITokenService _tokenService;
    
    public HubUserIdProvider(IDataProtectionProvider dp, ITokenService tokenService)
    {
        _prot = dp.CreateProtector("guest-id-v1");
        _tokenService = tokenService;
    }
    
    public string? GetUserId(HubConnectionContext connection)
    {
        // 1. Check for authenticated user via cookie
        var authId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(authId)) return authId;
        
        // 2. Check for access token in query string (mobile fallback)
        var httpContext = connection.GetHttpContext();
        var accessToken = httpContext?.Request.Query["access_token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(accessToken))
        {
            var userId = _tokenService.GetUserIdFromToken(accessToken);
            if (userId.HasValue)
                return userId.Value.ToString();
        }
        
        // 3. Check for guest cookie (legacy fallback)
        var cookie = httpContext?.Request.Cookies[EnsureGuestCookieMiddleware.CookieName];
        if (string.IsNullOrEmpty(cookie)) return null;
        try
        {
            var raw = _prot.Unprotect(WebEncoders.Base64UrlDecode(cookie));
            var gid = new Guid(raw.AsSpan(0, 16));
            return "guest:" + gid.ToString();
        }
        catch { return null; }
    }
}