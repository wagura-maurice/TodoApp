using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace TodoApp.Services
{
    public class EmailSender : IEmailSender<IdentityUser>
    {
        public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink) => 
            SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

        public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
            SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

        public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) =>
            SendEmailAsync(email, "Reset your password", $"Your password reset code is: {resetCode}");

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // In a real application, this would send an email
            // For development, we'll just log the email to the console
            System.Console.WriteLine($"Email to {email}, subject: {subject}, message: {htmlMessage}");
            return Task.CompletedTask;
        }
    }
}
