using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.DataProtection;
using Wuno.Api.Middleware;
public sealed class HubUserIdProvider : IUserIdProvider
{
    private readonly IDataProtector _prot;
    public HubUserIdProvider(IDataProtectionProvider dp)
        => _prot = dp.CreateProtector("guest-id-v1");

    public string? GetUserId(HubConnectionContext connection)
    {
        var authId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(authId)) return authId;

        var cookie = connection.GetHttpContext()?.Request.Cookies[EnsureGuestCookieMiddleware.CookieName];
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