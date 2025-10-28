using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.infrastructure;
using Wuno.Application.Games;
using Wuno.Domain.Rules;

namespace Wuno.Application.Users
{
    public class NoEmailUserService : IUserService
    {
        AppDbContext _db;
        public NoEmailUserService(AppDbContext db)
        {
            _db = db;
        }
        public Task<UserResponse> GetUserAsync(Guid userId, CancellationToken ct)
        {
            var user = _db.Users.Find(userId);
            if (user == null)
            {
                return Task.FromResult( new UserResponse(false, null, null, null));
            }
            return Task.FromResult(new UserResponse(true, user.Id, user.Name, user.IconUrl));
        }
        public Task<UserResponse> CreateUserAsync(string name, string? icon, string? email, CancellationToken ct)
        {
            User user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                IconUrl = icon,
                Email = email,
                EmailNormalized = Email.NormalizeEmail(email),
            };
            _db.Users.Add(user);
            _db.SaveChanges();
            return Task.FromResult(new UserResponse(true, user.Id, user.Name, user.IconUrl));
        }

        public Task<UserResponse> RegisterAccountAsync(Guid userId, string username, string password, string? email, string? iconUrl, CancellationToken ct)
        {
            User? user = _db.Users.Find(userId);
            if (user == null)
            {
                return Task.FromResult(new UserResponse(false, null, null, null));
            }
            var passwordHasher = new PasswordHasher
            user.Name = username;
            user.PasswordHash = 
        }

        public Task VerifyEmailAsync(Guid token, string verificationCode, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
