using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClothingShop.Business.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
    }

    /// <summary>
    /// Gửi email qua SMTP. Cấu hình trong appsettings.json:
    /// "Email": { "Host": "smtp.gmail.com", "Port": 587,
    ///            "Username": "...", "Password": "...", "From": "..." }
    /// 
    /// Gmail: bật "App Password" trong tài khoản Google.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var host     = _config["Email:Host"]     ?? "smtp.gmail.com";
            var port     = int.Parse(_config["Email:Port"] ?? "587");
            var username = _config["Email:Username"] ?? throw new InvalidOperationException("Email:Username chưa cấu hình");
            var password = _config["Email:Password"] ?? throw new InvalidOperationException("Email:Password chưa cấu hình");
            var from     = _config["Email:From"]     ?? username;

            using var smtp   = new SmtpClient(host, port);
            smtp.Credentials = new NetworkCredential(username, password);
            smtp.EnableSsl   = true;

            var message = new MailMessage
            {
                From       = new MailAddress(from, "ClothingShop"),
                Subject    = subject,
                Body       = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            try
            {
                await smtp.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To} — {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw new InvalidOperationException($"Không thể gửi email: {ex.Message}");
            }
        }
    }
}
