using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Domain.Rules
{
    public class Email
    {
        public static string? NormalizeEmail(string? email)
    => email?.Trim().ToLowerInvariant();

        public static bool LooksLikeEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(input);
                return addr.Host.Contains('.') && !addr.Address.EndsWith('.');
            }
            catch { return false; }
        }

        public static string CreateEmailToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        public static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash);
        }

        public static string BuildVerifyUrl(Guid userId, string token)
            => $"https://your-frontend.example/verify-email?userId={userId}&token={token}";
    }
}
