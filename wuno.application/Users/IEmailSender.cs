using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Users
{
    public interface IEmailSender
    {
        Task SendEmailVerificationAsync(string email, string verifyUrl, CancellationToken ct);
    }
}
