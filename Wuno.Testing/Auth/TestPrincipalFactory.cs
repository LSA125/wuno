using System.Security.Claims;

namespace Wuno.Testing.Auth
{
    public static class TestPrincipalFactory
    {
        public static ClaimsPrincipal Create(Guid userId, bool asGuest = false)
        {
            var claims = new List<Claim>
            {
                new Claim(asGuest ? "guest-id" : ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            return new ClaimsPrincipal(identity);
        }

        public static ClaimsPrincipal Create(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            return new ClaimsPrincipal(identity);
        }
    }
}
