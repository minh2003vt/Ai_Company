using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Application.Options;
using Application.Service.Interfaces;
using Microsoft.Extensions.Options;

namespace Application.Service
{
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;

        public EmailService(IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                // Validate email address
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    Console.WriteLine($"[EmailService] ERROR: To email address is null or empty");
                    throw new ArgumentException("Email address cannot be null or empty", nameof(toEmail));
                }

                // Validate configuration
                if (string.IsNullOrWhiteSpace(_options?.Host))
                {
                    Console.WriteLine($"[EmailService] ERROR: Email Host is not configured");
                    throw new InvalidOperationException("Email Host is not configured");
                }
                if (string.IsNullOrWhiteSpace(_options?.FromAddress))
                {
                    Console.WriteLine($"[EmailService] ERROR: Email FromAddress is not configured");
                    throw new InvalidOperationException("Email FromAddress is not configured");
                }

                using (var client = new SmtpClient(_options.Host, _options.Port))
                {
                    client.EnableSsl = _options.EnableSsl;
                    if (!string.IsNullOrWhiteSpace(_options.UserName))
                    {
                        client.Credentials = new NetworkCredential(_options.UserName, _options.Password);
                    }

                    var from = new MailAddress(_options.FromAddress, _options.FromName ?? "System");
                    var to = new MailAddress(toEmail);
                    using (var message = new MailMessage(from, to))
                    {
                        message.Subject = subject;
                        message.Body = htmlBody;
                        message.IsBodyHtml = true;
                        await client.SendMailAsync(message);
                    }
                }
                
                Console.WriteLine($"[EmailService] Email sent successfully to {toEmail}");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"[EmailService] SMTP ERROR sending email to {toEmail}: {smtpEx.Message}");
                throw new InvalidOperationException($"Failed to send email: {smtpEx.Message}", smtpEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] ERROR sending email to {toEmail}: {ex.Message}");
                Console.WriteLine($"[EmailService] Exception type: {ex.GetType().Name}");
                // Re-throw với message đơn giản, không throw toàn bộ exception object
                throw new InvalidOperationException($"Failed to send email: {ex.Message}");
            }
        }
    }
}
