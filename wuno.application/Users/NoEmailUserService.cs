using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using wuno.domain;
using wuno.infrastructure;
using Wuno.Application.Games;
using Wuno.Domain.Rules; // if you already have Email.NormalizeEmail

namespace Wuno.Application.Users
{
    public sealed class NoEmailUserService : IUserService
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<User> _hasher;

        public NoEmailUserService(AppDbContext db, IPasswordHasher<User> hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        // Get a user by cookie token (Guid). If not found, return "not found" shape.
        public async Task<UserResponse> GetUserAsync(Guid userId, CancellationToken ct)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
            {
                return new UserResponse(false, null, null, null, null, "User not Found");
            }
            user.LastActiveAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return new UserResponse(true, user.Id, user.Name, user.IconUrl, user.Email, null);
        }

        // Create a new (initially anonymous) user. Email is optional and NOT verified here.
        public async Task<UserResponse> CreateUserAsync(string name, string? icon, string? email, CancellationToken ct)
        {
            var emailNorm = Email.NormalizeEmail(email);

            // Optional uniqueness check for email if provided
            if (!string.IsNullOrWhiteSpace(emailNorm))
            {
                var emailInUse = await _db.Users.AnyAsync(u => u.EmailNormalized == emailNorm, ct);
                if (emailInUse)
                    return new UserResponse(false, null, null, null, null, "Email is already in use.");
            }

            User? matchingName = _db.Users.AsNoTracking().FirstOrDefault(u => u.Name == name);
            if (matchingName is not null)
            {
                return new UserResponse(false, null, null, null, null, "Username is already in use.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name?.Trim() ?? "",
                IconUrl = string.IsNullOrWhiteSpace(icon) ? null : icon!.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email!.Trim(),
                EmailNormalized = emailNorm,
                LastActiveAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);

            return new UserResponse(true, user.Id, user.Name, user.IconUrl, user.Email, null);
        }

        // Upgrade an existing anonymous user (by Guid) into a registered account.
        public async Task<UserResponse> RegisterAccountAsync(
            Guid userId,
            string username,
            string password,
            string? email,
            string? iconUrl,
            CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null)
                return new UserResponse(false, null, null, null, null, "User not Found");

            // If already registered (has a password), treat as idempotent “already registered”.
            if (!string.IsNullOrEmpty(user.PasswordHash))
                return new UserResponse(true, user.Id, user.Name, user.IconUrl, user.Email, null);

            // Basic validation
            if (string.IsNullOrWhiteSpace(username))
                return new UserResponse(false, null, null, null, null, "Username is required.");
            if (string.IsNullOrWhiteSpace(password))
                return new UserResponse(false, null, null, null, null, "Password is required.");

            // Optional email path
            var emailNorm = Email.NormalizeEmail(email);
            if (!string.IsNullOrWhiteSpace(emailNorm))
            {
                // Enforce uniqueness when provided
                var emailInUse = await _db.Users.AnyAsync(u => u.EmailNormalized == emailNorm && u.Id != user.Id, ct);
                if (emailInUse)
                    return new UserResponse(false, null, null, null, null, "Email is already in use.");
                user.Email = email!.Trim();
                user.EmailNormalized = emailNorm;
            }

            // Optional: ensure username uniqueness if you need it
            var nameInUse = await _db.Users.AnyAsync(u => u.Name == username && u.Id != user.Id, ct);
            if (nameInUse)
                return new UserResponse(false, null, null, null, null, "Username is already in use.");

            user.Name = username.Trim();
            user.IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? user.IconUrl : iconUrl!.Trim();
            user.PasswordHash = _hasher.HashPassword(user, password);
            user.LastActiveAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return new UserResponse(true, user.Id, user.Name, user.IconUrl, user.Email, null);
        }
        public async Task<UserResponse> EditRegisteredUserAsync(Guid userId, 
            string pass, 
            string? newName, 
            string? newIconUrl, 
            string? newEmail, 
            CancellationToken ct)
        {
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null)
                return new UserResponse(false, null, null, null, null, "User not Found");
            if (user.PasswordHash is null)
                return new UserResponse(false, null, null, null, null, "Cannot edit an anonymous user with this method.");
            // Verify password
            var verificationResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, pass);
            if (verificationResult == PasswordVerificationResult.Failed)
                return new UserResponse(false, null, null, null, null, "Invalid password.");
            // Update name if provided
            if (!string.IsNullOrWhiteSpace(newName))
            {
                var nameInUse = await _db.Users.AnyAsync(u => u.Name == newName.Trim() && u.Id != user.Id, ct);
                if (nameInUse)
                    return new UserResponse(false, null, null, null, null, "Username is already in use.");
                user.Name = newName.Trim();
            }
            // Update icon URL if provided
            if (!string.IsNullOrWhiteSpace(newIconUrl))
            {
                user.IconUrl = newIconUrl.Trim();
            }
            // Update email if provided
            if (!string.IsNullOrWhiteSpace(newEmail))
            {
                var emailNorm = Email.NormalizeEmail(newEmail);
                // Enforce uniqueness when provided
                var emailInUse = await _db.Users.AnyAsync(u => u.EmailNormalized == emailNorm && u.Id != user.Id, ct);
                if (emailInUse)
                    return new UserResponse(false, null, null, null, null, "Email is already in use.");
                user.Email = newEmail.Trim();
                user.EmailNormalized = emailNorm;
            }
            await _db.SaveChangesAsync(ct);
            return new UserResponse(true, user.Id, user.Name, user.IconUrl, user.Email, null);
        }
        public async Task<UserResponse> EditAnonUserAsync(Guid userId, string? newName, string? newIconUrl, string? newEmail, CancellationToken ct)
        {
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null)
                return new UserResponse(false, null, null, null, null, "User not Found");

            if (user.PasswordHash is not null)
                return new UserResponse(false, null, null, null, null, "Cannot edit a registered user with this method.");

            // Update name if provided
            if (!string.IsNullOrWhiteSpace(newName))
            {
                var nameInUse = await _db.Users.AnyAsync(u => u.Name == newName.Trim() && u.Id != user.Id, ct);
                if (nameInUse)
                    return new UserResponse(false, null, null, null, null, "Username is already in use.");
                user.Name = newName.Trim();
            }
            // Update icon URL if provided
            if (!string.IsNullOrWhiteSpace(newIconUrl))
            {
                user.IconUrl = newIconUrl.Trim();
            }
            // Update email if provided
            if (newEmail is not null)
            {
                var emailNorm = Email.NormalizeEmail(newEmail);
                // Enforce uniqueness when provided
                var emailInUse = await _db.Users.AnyAsync(u => u.EmailNormalized == emailNorm && u.Id != user.Id, ct);
                if (emailInUse)
                    return new UserResponse(false, null, null, null, null, "Email is already in use.");
                user.Email = newEmail.Trim();
                user.EmailNormalized = emailNorm;
            }
            await _db.SaveChangesAsync(ct);
            return new UserResponse(true, user.Id, user.Name, user.IconUrl, user.Email, null);
        }

        // No-op for the “NoEmailVerification” service. Keep signature for interface compliance.
        public Task VerifyEmailAsync(Guid token, string verificationCode, CancellationToken ct)
            => Task.CompletedTask;
    }
}
