using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Wuno.Api.Services
{
    public interface ITokenService
    {
        string GenerateToken(Guid userId, string? name = null, bool isRegistered = false);
        ClaimsPrincipal? ValidateToken(string token);
        Guid? GetUserIdFromToken(string token);
    }

    public sealed class TokenService : ITokenService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly int _expirationDays;
        private readonly SymmetricSecurityKey _signingKey;

        public TokenService(IConfiguration configuration)
        {
            // Use a secret key from configuration, or generate a stable one from data protection
            _secretKey = configuration["Jwt:Secret"] 
                ?? configuration.GetConnectionString("Default")?.Substring(0, 32) 
                ?? "WunoGameDefaultSecretKey12345678"; // Fallback for dev
            _issuer = configuration["Jwt:Issuer"] ?? "WunoApi";
            _expirationDays = int.TryParse(configuration["Jwt:ExpirationDays"], out var days) ? days : 30;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey.PadRight(32)));
        }

        public string GenerateToken(Guid userId, string? name = null, bool isRegistered = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("registered", isRegistered.ToString().ToLower()),
            };

            if (!string.IsNullOrEmpty(name))
            {
                claims.Add(new Claim(ClaimTypes.Name, name));
            }

            var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_expirationDays),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _issuer,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public Guid? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null)
                return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }
    }
}
