using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace DentistAPI.Services
{
    public interface IEmailService
    {
        Task<(bool Success, string ErrorMessage)> SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<(bool Success, string ErrorMessage)> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var host = _configuration["SmtpSettings:Host"];
                var port = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
                var username = _configuration["SmtpSettings:Username"];
                var password = _configuration["SmtpSettings:Password"];
                var fromAddress = _configuration["SmtpSettings:FromAddress"];
                var enableSmtp = bool.Parse(_configuration["SmtpSettings:EnableSmtp"] ?? "false");

                if (!enableSmtp)
                    return (true, "SMTP is disabled. Email skipped.");

                if (string.IsNullOrEmpty(toEmail) || string.IsNullOrEmpty(host)) 
                    return (false, "Invalid email configuration or destination address.");

                using (var smtpClient = new SmtpClient(host, port))
                {
                    smtpClient.Credentials = new NetworkCredential(username, password);
                    smtpClient.EnableSsl = true;
                    smtpClient.Timeout = 5000; // Fail fast after 5 seconds instead of 100 seconds

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromAddress),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                    };
                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                    return (true, string.Empty);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
