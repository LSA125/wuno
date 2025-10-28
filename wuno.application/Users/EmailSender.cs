using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MailKit.Net.Smtp;
using MimeKit;
using Wuno.Domain.Rules;

namespace Wuno.Application.Users
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailVerificationAsync(string email, string verifyUrl, CancellationToken ct)
        {
            //Need a host
            throw new NotImplementedException();
            MimeMessage message = new();
            message.From.Add(new MailboxAddress("Wuno", "no-reply@wuno.example"));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "Verify your email";
            message.Body = new TextPart("html")
            {
                Text = $"""
                <p>Hi! Please verify your email by clicking the link below:</p>
                <p><a href="{verifyUrl}">Verify email</a></p>
                <p>This link expires in 24 hours.</p>
                """
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.yourhost.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync("smtp-user", "smtp-pass");
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
