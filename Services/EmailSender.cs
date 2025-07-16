using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace TodoApp.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // In a real application, this would send an email
            // For development, we'll just log the email to the console
            System.Console.WriteLine($"Email to {email}, subject: {subject}, message: {htmlMessage}");
            return Task.CompletedTask;
        }
    }
}
