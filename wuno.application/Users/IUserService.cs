using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuno.Application.Games;

namespace Wuno.Application.Users
{
    public interface IUserService
    {
        Task<UserResponse> GetUserAsync(Guid userId, CancellationToken ct);
        Task<UserResponse> CreateUserAsync(string name, string? icon, string? email, CancellationToken ct);
        Task RegisterAccountAsync(Guid token, string username, string password, string email, string iconUrl, CancellationToken ct);
        Task VerifyEmailAsync(Guid token, string verificationCode, CancellationToken ct);
    }
}
